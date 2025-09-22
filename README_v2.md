# Priority Task Manager

Priority Task Manager is not just another to-do list. It is an intelligent, command-line-driven application built on the **"Upstream Urgency"** philosophy. Instead of relying on subjective priority scores, this system logically calculates what is truly the most critical task to work on next, based on a rigorous, time-based analysis of deadlines, task durations, and dependencies.

It is designed for developers, project managers, and students who need a powerful tool to cut through the noise and focus on what truly matters to meet their deadlines.

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