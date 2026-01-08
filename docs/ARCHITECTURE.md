# Architecture

This document provides a high-level overview of the Priority Task Manager's architecture. It is intended for developers to understand the key components, data flow, and design patterns used in the system.

## Project Structure

The solution is divided into three main projects to ensure a clean separation of concerns:

-   `PriorityTaskManager/`: The **core library**. This project contains all business logic, data models, services, and the agent-based scheduling engine. It is completely independent of any user interface and can be reused by other front-ends (e.g., a future GUI or web application).
-   `PriorityTaskManager.CLI/`: The **command-line interface**. This project is responsible for parsing user commands, interacting with the `TaskManagerService` from the core library, and displaying output to the console.
-   `PriorityTaskManager.Tests/`: The **unit testing project**. This contains xUnit tests for the core library. **Note:** These tests are currently outdated due to recent refactoring and require a complete overhaul.

## Data Flow and Persistence

The application's state is managed through a clear and decoupled persistence layer.

1.  **Data Source**: All application data (tasks, lists, events, user profile) is stored in `.json` files located in the `PriorityTaskManager/Data/` directory.
2.  **Build Process**: The `.csproj` file for the `PriorityTaskManager` project is configured to copy these `Data` files to the output directory during the build process.
3.  **`PersistenceService`**: This service is responsible for all read/write operations to the JSON files. Its constructor takes a single `dataDirectory` path, making it portable and independent of the file system's layout.
4.  **`DataContainer`**: On application startup, the `PersistenceService` loads all data from the JSON files into a single `DataContainer` object.
5.  **`TaskManagerService`**: This central service holds the `DataContainer` in memory. All business logic operations (adding tasks, updating events, etc.) are performed on the data within this container. When data is modified, `TaskManagerService` calls `PersistenceService.SaveData()` to write the changes back to the disk.

## The Agent Pipeline (Multi-Agent Coordination Pattern - MCP)

The most sophisticated part of the architecture is the agent-based pipeline used for task scheduling, which leverages a Multi-Agent Coordination Pattern (MCP).

The primary benefit of this architecture is **modularity**. It breaks down the complex process of scheduling into a series of small, independent, and single-responsibility agents. This makes the system easier to modify, debug, and extend.

The pipeline is defined and executed in `PriorityTaskManager/Services/MultiAgentUrgencyStrategy.cs`.

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

## Core Services and Strategies

The core library is built around a set of services and strategies, each with a specific responsibility. The use of interfaces (e.g., `IPersistenceService`, `IUrgencyStrategy`) is a key design principle, allowing for modularity and testability.

-   **`TaskManagerService`**: The central facade for all business logic. It coordinates operations like adding, deleting, and updating tasks, lists, and events.
-   **`PersistenceService`**: Implements `IPersistenceService`. Handles the serialization and deserialization of data to and from the `.json` files on disk.
-   **`TaskMetricsService`**: Implements `ITaskMetricsService`. Provides utility functions for calculating metrics about tasks, used by the `ListHandler` to display statistics.
-   **`IUrgencyStrategy`**: This interface defines the contract for any class that can prioritize tasks. The application currently has two implementations:
    -   **`MultiAgentUrgencyStrategy`**: The primary, sophisticated strategy that uses the agent pipeline described below.
    -   **`SingleAgentStrategy`**: A simpler, legacy strategy. **Note:** This strategy is currently broken and deprecated. It will be removed in a future refactoring.
-   **`Services/Helpers`**: This directory contains helper classes that perform specific, isolated tasks for the services. For example, `DependencyGraphHelper` provides functions for working with task dependencies.
