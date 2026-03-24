# Architecture

This document provides a high-level overview of the Priority Task Manager's architecture. It is intended for developers to understand the key components, data flow, and design patterns used in the system.

For details on **Quantifying Task Complexity**, please refer to [COMPLEXITY_GUIDE.md](COMPLEXITY_GUIDE.md).

## Project Structure

The solution is divided into three main projects to ensure a clean separation of concerns:

-   `PriorityTaskManager/`: The **core library**. This project contains all business logic, data models, services, and the agent-based scheduling engine. It is completely independent of any user interface and can be reused by other front-ends (e.g., a future GUI or web application).
-   `PriorityTaskManager.CLI/`: The **command-line interface**. This project is responsible for parsing user commands, interacting with the `TaskManagerService` from the core library, and displaying output to the console.
-   `PriorityTaskManager.Tests/`: The **unit testing project**. This contains xUnit tests for the core library. **Note:** These tests are currently outdated due to recent refactoring and require a complete overhaul.

### Detailed File Structure

```markdown
Priority-Task-Manager/
├── Priority-Task-Manager.sln
├── PriorityTaskManager.sln
├── README.md
├── TODO.md
├── docs/
│   ├── ARCHITECTURE.md
│   ├── RFC_SOLVER_MIGRATION.md
│   ├── SCHEDULING_DISCUSSION_NOTES.md
│   ├── SCHEDULING_SYSTEM_SPEC.md
│   ├── STATUS.md
│   ├── TESTING_STRATEGY.md
│   ├── TODO.md
│   └── WORKFLOW.md
├── PriorityTaskManager/          <-- Core Logic
│   ├── PriorityTaskManager.csproj
│   ├── Models/                   <-- Shared Domain Models
│   ├── Services/                 <-- Shared Services
│   ├── MCP/                      <-- LEGACY Scheduler (Strategies/Agents)
│   │   ├── Agents/
│   │   ├── MCP.cs
│   │   ├── MCPContext.cs
│   │   └── McpGoldPanningStrategy.cs
│   └── Scheduling/               <-- V1 Scheduler (New)
│       ├── Core/                 <-- Interfaces
│       └── Optimization/         <-- Optimization Strategy Implementation
├── PriorityTaskManager.CLI/      <-- Command Line Interface
│   ├── ...
└── PriorityTaskManager.Tests/    <-- Unit Tests
    ├── PriorityTaskManager.Tests.csproj
    ├── Infrastructure/           <-- Shared Mocks/Helpers
    ├── Integration/              <-- Service-Level Tests
    ├── LegacyMCP/                <-- Legacy Agent Tests
    └── V1Optimization/           <-- New V1 Tests
```

### Technology Stack
*   **Framework**: .NET 8
*   **Serialization**: `System.Text.Json`
*   **Testing**: `xUnit`

## Data Flow and Persistence

The application's state is managed through a clear and decoupled persistence layer.

### Glossary of Core Types

Before understanding the flow, it is helpful to define the core data objects passed between components:

*   **`TaskItem`**: Represents a single unit of work. Contains properties like `Title`, `DueDate`, `Complexity`, `Importance`, and `Dependencies`.
*   **`ScheduledChunk`**: A specific allocation of time for a `TaskItem`. A large task might be split into multiple `ScheduledChunk`s across different days.
*   **`ScheduleWindow`**: A period of available time (start to end) derived from the `UserProfile`'s work hours, minus any `Event`s.
*   **`DataContainer`**: The master state object held by `TaskManagerService`. It wraps all lists, tasks, events, and user settings.
*   **`MCPContext`**: The context object passed heavily through the Agent pipeline. It contains the `Tasks` being processed, the `AvailableWindows`, and a `SharedState` dictionary for inter-agent communication. It implies a functional style: agents act on this context to produce a *new* or modified state.

1.  **Data Source**: All application data (tasks, lists, events, user profile) is stored in `.json` files located in the `PriorityTaskManager/Data/` directory.
2.  **Build Process**: The `.csproj` file for the `PriorityTaskManager` project is configured to copy these `Data` files to the output directory during the build process.
3.  **`PersistenceService`**: This service is responsible for all read/write operations to the JSON files. Its constructor takes a single `dataDirectory` path, making it portable and independent of the file system's layout.
4.  **`DataContainer`**: On application startup, the `PersistenceService` loads all data from the JSON files into a single `DataContainer` object.
5.  **`TaskManagerService`**: This central service holds the `DataContainer` in memory. All business logic operations (adding tasks, updating events, etc.) are performed on the data within this container. When data is modified, `TaskManagerService` calls `PersistenceService.SaveData()` to write the changes back to the disk.

## Scheduling Strategies (Dual-Mode)

The application supports multiple scheduling algorithms, selectable by the user via `UserProfile.SchedulingMode`. This allows for safe evolution of the scheduling logic without breaking existing functionality. The common interface for all strategies is `IUrgencyStrategy`.

### 1. MCP Gold Panning Strategy (Legacy)

*   **Class**: `McpGoldPanningStrategy` (formerly `MultiAgentUrgencyStrategy`)
*   **Concept**: "Gold Panning". Tasks flow through time like material in a sluice box. Heavy items (Urgent/Important) settle early; light items (Backlog) wash downstream.
*   **Architecture**: Multi-Agent Coordination Pattern (MCP).
*   **Status**: Maintenance Mode. Default for existing users.

The flow for this strategy is defined below in "Legacy Agent Pipeline".

### 2. Constraint Optimization Strategy (New)

*   **Class**: `ConstraintOptimizationStrategy`
*   **Concept**: "Solver". The scheduler treats the calendar as a constraint satisfaction problem. It optimizes for an objective function (minimizing lateness, maximizing balance) while respecting hard limits.
*   **Architecture**: Single-pass Optimization Planner.
*   **Status**: Active Development.
*   **Spec**: implementation details are strictly defined in `SCHEDULING_SYSTEM_SPEC.md`.

## Legacy Agent Pipeline (MCP Gold Panning)

The legacy pipeline is used when `SchedulerMode` is set to `GoldPanning`.

The primary benefit of this architecture is **modularity**. It breaks down the complex process of scheduling into a series of small, independent, and single-responsibility agents. This makes the system easier to modify, debug, and extend.

The pipeline is defined and executed in `PriorityTaskManager/MCP/McpGoldPanningStrategy.cs`.

### Agent Execution Order

When `list` is called, the agents are executed in the following sequence. This order is designed to progressively refine the task list before the final schedule is generated, following a **Prioritize -> Balance -> Schedule** workflow. Each agent passes an `MCPContext` object containing the shared data to the next agent in the chain.

1.  **`TaskAnalyzerAgent`**:
    -   **Purpose**: Analyzes and adjusts task properties before prioritization.
    -   **Action**: Currently, its primary role is to calculate the `EffectiveImportance` of each task.

2.  **`SchedulePreProcessorAgent`**:
    -   **Purpose**: Prepares the user's schedule by identifying all available time slots for the upcoming scheduling.
    -   **Action**: It looks at the user's defined work hours (from `UserProfile`) and any existing `Events`. It then generates a list of `AvailableScheduleWindow` objects representing the free time slots.

3.  **`PrioritizationAgent`**:
    -   **Purpose**: To perform an initial, high-level sort of the master task list into a logical order.
    -   **Action**: It sorts the tasks first by their `DueDate` (ascending) and then by their `Complexity` (descending). This provides a baseline priority for the next agent.

4.  **`ComplexityBalancerAgent`**:
    -   **Purpose**: To intelligently re-order the prioritized task list to distribute complexity. This happens *before* final scheduling.
    -   **Action**: It reviews the sorted list from the `PrioritizationAgent` and the available time slots. Its goal is to re-order the tasks to: 1) Distribute high-complexity tasks across different days to prevent overload, while respecting due dates. 2) Within any single day, move complex tasks earlier in the work window. The output is the final, balanced task order.

5.  **`SchedulingAgent`**:
    -   **Purpose**: As the **final** agent, its sole authority is to execute the plan and create the detailed schedule.
    -   **Action**: It takes the perfectly ordered list of tasks from the `ComplexityBalancerAgent` and performs a "greedy" placement into the available time slots. It has no complex logic; it simply populates the schedule based on the final task order.

### Future Vision

The current MCP framework provides a strong foundation for future enhancements. The key long-term goals are:

1.  **Multi-Platform Support**: The clean separation between the core logic and the UI is intentional, paving the way for future front-ends on platforms like Web, Desktop, iOS, and Android.

2.  **Calendar Integration**: Integrate with external calendar services (e.g., Google Calendar, Outlook) to get a more accurate, real-time view of the user's availability. This would replace the manual `Events` system and improve scheduling accuracy.

3.  **Refining the Multi-Agent System**: While the system is already multi-agent, the vision is to enhance it by adding more specialized agents. These could analyze tasks from different perspectives (e.g., user habits, energy levels, long-term goals) to provide a more adaptive and context-aware prioritization.

## V1 Scheduling Contract (Phase 1 Baseline)

This section defines the contract for the new scheduling path. It is the implementation baseline for migration work.

### Planner Input Contract
The planner consumes a request with:
1. Candidate tasks (excluding completed tasks).
2. User profile and scheduling preferences.
3. Event blocks and work-window rules.
4. Current time context via `ITimeService`.
5. Overtime and lateness policy flags (`AllowMustScheduleLateness`, `AllowMustScheduleOvertime`, `OvertimeScope`, `AllowNonMustLateness`).

### Planner Output Contract
The planner returns:
1. Scheduled chunks with explicit start/end times.
2. Unscheduled non-must tasks.
3. Late and overtime classification metadata.
4. Reason codes and explanation entries for user-facing output.
5. Infeasibility diagnostics for must-schedule tasks that cannot be placed under current policy/horizon.
6. Optional horizon advisories and timeline estimate metadata.

### Locked V1 Execution Pipeline
The V1 execution order is:
1. `PolicyCoordinator + Feasibility`
2. `WindowBuilder`
3. `Dependency + Decomposition`
4. `Scoring`
5. `OptimizationPlanner`
6. `Explanation`

In V1, in-day sequencing and cross-day balancing run as internal sub-passes inside `OptimizationPlanner`.

### Stage Ownership Boundaries (No Overlap)
Single owner per concern:
1. Validation and cycle checks: `Feasibility`.
2. Availability window construction: `WindowBuilder`.
3. Chunk construction rules: `Decomposition`.
4. Objective and weight definitions: `Scoring`.
5. Placement, drop, and overtime decisions: `OptimizationPlanner`.
6. User-facing reason mapping: `Explanation`.

### Objective Model Requirements
The scoring model must include:
1. Slack risk (deadline closeness before lateness).
2. Lateness penalty.
3. Drop policy penalty.
4. In-day ordering penalty.
5. Cross-day load balancing penalty.
6. Switching penalty derived from schedule shape (transitions, fragmentation, tiny chunks).

V1 due-date handling note:
1. Null due-date tasks are treated as neutral backlog tasks in V1.
2. No aging urgency is applied in V1.
3. Null due-date tasks are considered as capacity-fill work after must-schedule and due-dated urgency work.

V1 horizon behavior note:
1. Adaptive mode estimates required horizon from remaining duration and effective capacity.
2. If estimated horizon exceeds 30 days, the caller should request confirmation.
3. If estimated horizon exceeds 90 days, the caller should emit a high-horizon alert with timeline estimate.

### Migration Policy on This Branch
1. Legacy scheduling paths may be removed early on this branch.
2. Branch-level fallback is the rollback strategy.
3. Documentation contracts must be completed before implementation tasks.

## Core Services and Strategies

The core library is built around a set of services and strategies, each with a specific responsibility. The use of interfaces (e.g., `IPersistenceService`, `IUrgencyStrategy`) is a key design principle, allowing for modularity and testability.

### Key Files & Responsibilities

This section details the primary files in the core library and their interconnections. It is designed to help developers identify the correct file to modify for a specific task.

#### 1. The Coordinator: `MultiAgentUrgencyStrategy.cs`
*   **Path**: `PriorityTaskManager/MCP/MultiAgentUrgencyStrategy.cs`
*   **Role**: The **Orchestrator**. It defines the entire scheduling pipeline. It is responsible for instantiating the agents, defining their execution order, and passing the `MCPContext` between them.
*   **Connections**:
    *   **Invokes**: ALL Agents (`TaskAnalyzerAgent`, `SchedulePreProcessorAgent`, etc.).
    *   **Invoked By**: `TaskManagerService.GetPrioritizedTasks()`.
*   **Developer Note**: If you add a new Agent, **you must** register it here in the `_agents` list, or it will never run. If you want to change the order of execution, this is the file to edit.

#### 2. The Facade: `TaskManagerService.cs`
*   **Path**: `PriorityTaskManager/Services/TaskManagerService.cs`
*   **Role**: The **Public API**. It provides high-level methods for the CLI to interact with (e.g., `AddTask`, `DeleteTask`, `ListTasks`). It manages data persistence calls but delegates complex logic (like scheduling) to strategies.
*   **Connections**:
    *   **Invokes**: `IPersistenceService` (for saving), `IUrgencyStrategy` (for scheduling), `ITimeService`.
    *   **Invoked By**: All CLI Handlers (e.g., `AddHandler`, `ListHandler`).
*   **Developer Note**: Do not put complex scheduling logic here. This service should remain a thin coordination layer.

#### 3. The Data Manager: `PersistenceService.cs`
*   **Path**: `PriorityTaskManager/Services/PersistenceService.cs`
*   **Role**: The **Storage Engine**. It handles reading and writing the JSON files (`tasks.json`, `user_profile.json`, etc.) in the `Data/` folder.
*   **Connections**:
    *   **Invoked By**: `TaskManagerService`.
*   **Developer Note**: This service is file-system agnostic regarding the data folder path, which is injected via constructor.

#### 4. The Timekeeper: `TimeService.cs`
*   **Path**: `PriorityTaskManager/Services/TimeService.cs`
*   **Role**: The **Clock**. It abstracts `DateTime.Now`. It supports a "Simulated Time" mode for testing logic that depends on specific dates/times.
*   **Connections**:
    *   **Invoked By**: `TaskManagerService`, `SchedulePreProcessorAgent`, `TaskAnalyzerAgent`.
*   **Developer Note**: **NEVER** use `DateTime.Now` directly in the Core library. Always inject `ITimeService`.

### Agent Responsibilities

*   **`TaskAnalyzerAgent.cs`**:
    *   **Role**: Pure logic calculation on individual tasks (e.g., `EffectiveImportance`).
    *   **Input**: Raw `TaskItem`s.
    *   **Output**: Modified `TaskItem`s with updated properties.
*   **`SchedulePreProcessorAgent.cs`**:
    *   **Role**: Environment analysis. Calculates *when* work can happen.
    *   **Input**: `UserProfile`, `Events`.
    *   **Output**: Populates `MCPContext.AvailableWindows`.
*   **`PrioritizationAgent.cs`**:
    *   **Role**: Sorting. Determines the *ideal* order of tasks ignoring time constraints.
    *   **Input**: Unsorted `TaskItem`s.
    *   **Output**: Sorted `TaskItem`s.
*   **`ComplexityBalancerAgent.cs`**:
    *   **Role**: Optimization. Re-shuffles the sorted list to prevent burnout (e.g., not doing 3 hard tasks in a row).
    *   **Input**: Sorted `TaskItem`s, `AvailableWindows`.
    *   **Output**: Optimally re-ordered `TaskItem`s.
*   **`SchedulingAgent.cs`**:
    *   **Role**: Execution. Places tasks into slots. **Dumb logic**—it just follows the order given by the previous agent.
    *   **Input**: Optimally ordered `TaskItem`s, `AvailableWindows`.
    *   **Output**: `ScheduledChunk`s assigned to specific times.

-   **`Services/Helpers`**: This directory contains helper classes that perform specific, isolated tasks for the services. For example, `DependencyGraphHelper` provides functions for working with task dependencies.
