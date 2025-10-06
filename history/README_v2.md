# Priority Task Manager

Priority Task Manager is not just another to-do list. It is an intelligent, command-line-driven application built on the **"Upstream Urgency"** philosophy. Instead of relying on subjective priority scores, this system logically calculates what is truly the most critical task to work on next, based on a rigorous, time-based analysis of deadlines, task durations, and dependencies.

It is designed for developers, project managers, and students who need a powerful tool to cut through the noise and focus on what truly matters to meet their deadlines.

---

# Architectural Deep Dive: The `cleanup` Command and MCP

The `cleanup` command is a powerful utility designed to maintain the health and usability of the task database. It performs a complex, multi-step operation that includes archiving and deleting completed tasks, re-indexing the remaining tasks, and updating all dependency relationships to ensure data integrity.

Given the destructive and intricate nature of this operation, it was architected using the **Model Context Protocol (MCP)** to ensure the process is robust, transparent, and safe.

## The MCP Framework

The core of the implementation is a generic MCP framework that can orchestrate any multi-agent workflow. It consists of three main components:

*   **`IAgent.cs`**: An interface defining the contract for any agent. Each agent is a self-contained component with a single responsibility, receiving an `MCPContext` and returning a modified version of it.
*   **`MCPContext.cs`**: The central data object that acts as the "shared memory" for the entire operation. It holds the shared state (data passed between agents), a running history log of all actions taken, and error-handling flags (`LastError`, `ShouldTerminate`).
*   **`MCP.cs`**: A static coordinator with a `Coordinate` method that executes a given chain of agents in sequence. It is responsible for passing the context from one agent to the next and for halting the process if an agent signals an error.

## The `cleanup` Command as an MCP Workflow

The `cleanup` command is implemented as a sequential pipeline of five distinct agents, each with a single responsibility:

1.  **`FindCompletedTasksAgent`**: Identifies all tasks marked as "completed" and adds them to the `MCPContext`.
2.  **`ArchiveTasksAgent`**: Takes the completed tasks from the context and archives them to a separate `archive.json` file for historical record-keeping.
3.  **`DeleteTasksAgent`**: Deletes the archived tasks from the active `tasks.json` database.
4.  **`ReIndexTasksAgent`**: Sorts the remaining tasks by urgency and assigns them new, sequential `DisplayId`s (1, 2, 3...). It crucially creates an "ID Map" of the old IDs to the new IDs and stores it in the context.
5.  **`UpdateDependenciesAgent`**: Reads the "ID Map" from the context and iterates through all tasks, updating their dependency lists to use the new IDs. This step is critical for maintaining data integrity.

### Architectural Benefits of Using MCP

Choosing MCP for this feature was a deliberate technical decision that provides significant benefits over a monolithic, direct-call implementation:

*   **Safety and Atomicity**: The protocol ensures that the operation can be safely aborted. If the `ReIndexTasksAgent` were to fail, the `UpdateDependenciesAgent` would never run, preventing the database from being left in a corrupted state with broken dependency links.
*   **Transparency and Auditing**: The `MCPContext.History` log provides a complete, step-by-step audit trail of the entire operation. This is invaluable for debugging and gives the user clear feedback on the actions performed.
*   **Modularity and Extensibility**: Each step of the cleanup process is a self-contained agent. This makes the system easy to maintain and extend. For example, a new agent could be added to the chain to notify the user via email, without modifying any of the existing agents.

---

## The Core Philosophy: "Upstream Urgency"

The foundation of this application is that true urgency flows backward from the final deadline of a project. The deadline of the last task in a dependency chain dictates the schedule for all preceding tasks. The system's core calculation is the **Latest Possible Start Date (LPSD)**—the absolute last moment a task can begin without causing a project-wide delay.

-   **For Dependent Tasks:** The LPSD of a task is determined by the LPSD of the task it blocks. If Task B must start in 5 days, and it depends on Task A which takes 3 day, then Task A's LPSD is now 2 days away.
-   **For All Tasks:** The system creates a unified timeline, first mapping the critical paths for all projects with dependencies, and then intelligently slotting in standalone tasks based on their own LPSD. The result is a single, prioritized list of what to do next.

---

## Key Features

### Intelligent Task Management
*   **Dynamic Urgency Engine:** Automatically sorts tasks by their calculated LPSD, ensuring the most time-critical work is always at the top.
*   **Dependency Management:** Define dependencies between tasks (`depend add <child> <parent>`) to build accurate project timelines. The system is protected against circular dependencies.
*   **Importance Score:** Factor in subjective priority with a user-set `Importance` score (1-10), which acts as a multiplier on the final urgency calculation.

### Multi-List Organization
*   **Create & Manage Lists:** Organize your life into multiple contexts (e.g., `Work`, `Home`, `Project Phoenix`).
*   **Active List System:** Switch between lists effortlessly. New tasks are automatically added to your currently active list.
*   **Per-List Sorting:** Each list's view can be independently sorted by `Default` (Urgency), `Alphabetical`, `DueDate`, or `Id`.

### Advanced Command-Line Interface
*   **Interactive Menus:** Effortlessly navigate a modern TUI for complex actions like selecting a date or switching lists using only your arrow keys.
*   **Multi-Task Commands:** Operate on multiple tasks at once. Commands like `delete`, `complete`, and `uncomplete` accept comma-separated IDs (e.g., `delete 5, 6, 7`).
*   **Powerful Subcommands:** A rich command set allows for precise control over tasks, lists, and dependencies.

---

## The Future Vision: The "Focus Dashboard"

The primary long-term goal is to evolve the application into a modern, interactive **Text-based User Interface (TUI)** called the "Focus Dashboard." This dashboard will provide a clean, actionable, and motivating user experience with three main components:

1.  **The "Up Next" Queue:** A panel displaying only the top 3-5 most urgent tasks, telling you exactly what to work on now.
2.  **Project Health Indicators:** A high-level overview showing each project and a color-coded "Project Slack Bar" (Green/Yellow/Red) representing its overall health.
3.  **The Contextual Timeline:** A dynamic "micro-Gantt" panel that provides a timeline snippet for the currently highlighted task, showing the tasks immediately before and after it.

---

## Getting Started

**Prerequisites:**
- .NET SDK (8.0 or higher) installed on your system.

**Steps to Run:**
1.  Clone the repository:
    ```bash
    git clone https://github.com/DracoNick0/Priority-Task-Manager.git
    ```
2.  Navigate to the solution directory:
    ```bash
    cd Priority-Task-Manager
    ```
3.  Navigate to the CLI project:
    ```bash
    cd PriorityTaskManager.CLI
    ```
4.  Run the application:
    ```bash
    dotnet build
    dotnet run
    ```

---

## Command Reference

### Task Commands
| Command | Description |
| :--- | :--- |
| `add <Title>` | Add a new task to the active list. |
| `view <Id>` | View all details of a specific task. |
| `edit <Id> ...` | Edit a task's attributes interactively. |
| `edit <attr> <Id> [val]` | Edit a specific attribute, optionally providing the new value directly. |
| `delete <Id1,Id2,...>` | Delete one or more tasks by ID. |
| `complete <Id1,Id2,...>` | Mark one or more tasks as complete. |
| `uncomplete <Id1,Id2,...>` | Mark one or more tasks as incomplete. |

### List Commands
| Command | Description |
| :--- | :--- |
| `list` | Display tasks in the current active list. |
| `list all` | Show all available lists and which one is active. |
| `list create <Name>` | Create a new task list. |
| `list switch [Name]` | Switch the active list, or open an interactive menu if no name is given. |
| `list sort <Option>` | Set the sort order for the active list (`Default`, `Alphabetical`, `DueDate`, `Id`). |
| `list delete <Name>` | Delete a list and all of its tasks. |

### Dependency Commands
| Command | Description |
| :--- | :--- |
| `depend add <childId> <parentId>` | Make the child task dependent on the parent task. |
| `depend remove <childId> <parentId>` | Remove a dependency link. |

### General Commands
| Command | Description |
| :--- | :--- |
| `help` | Show this help message. |
| `exit` | Exit the application. |

---

### License

This project is licensed under the MIT License.