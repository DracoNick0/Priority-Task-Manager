# Priority Task Manager

An intelligent command-line application designed to solve decision fatigue in task management through a sophisticated, agent-based scheduling engine.

This project transforms a simple to-do list into a smart assistant that generates a detailed, optimized schedule by analyzing tasks based on due dates, complexity, and dependencies.

## Project Status

The application is stable and includes the following capabilities:

-   **Advanced Task Scheduling**: Generates a detailed, non-contiguous schedule for tasks.
-   **Full Task Management**: Complete CRUD (Create, Read, Update, Delete) operations for tasks.
-   **Dependency Management**: Create dependency chains between tasks.
-   **Event Management**: Create fixed events (e.g., meetings) that the scheduler will treat as unavailable time.
-   **Simulated Time**: A `time` command allows for setting a simulated date and time, which is crucial for testing and debugging the scheduler.

## Getting Started

**Prerequisites:**
*   .NET SDK (8.0 or higher)

**Build and Run:**
1.  Build the solution:
    ```bash
    dotnet build
    ```
2.  Run the CLI application:
    ```bash
    cd PriorityTaskManager.CLI
    dotnet run
    ```

## Architecture Overview

The solution is divided into three projects for a clean separation of concerns:

-   `PriorityTaskManager/`: The **core library** containing all business logic, data models, and the agent-based scheduling engine.
-   `PriorityTaskManager.CLI/`: The **command-line interface** responsible for parsing user commands and displaying output.
-   `PriorityTaskManager.Tests/`: The **unit testing project**.

### The Agent Pipeline (Multi-Agent Coordination Pattern)

The core of the application is an agent-based pipeline for task scheduling. This modular architecture breaks the complex process of scheduling into a series of small, independent agents, making the system easier to debug and extend.

The pipeline follows a **Prioritize -> Balance -> Schedule** workflow:

1.  **`TaskAnalyzerAgent`**: Analyzes and adjusts task properties.
2.  **`SchedulePreProcessorAgent`**: Identifies all available time slots based on the user's work hours and events.
3.  **`PrioritizationAgent`**: Performs an initial sort of the tasks by due date and complexity.
4.  **`ComplexityBalancerAgent`**: Intelligently re-orders the list to distribute complex tasks, preventing overload.
5.  **`SchedulingAgent`**: As the final agent, it takes the perfectly ordered list and populates the schedule.

## Command Reference

### Task Commands
| Command | Description |
| :--- | :--- |
| `add <Title>` | Add a new task to the active list. |
| `view <Id>` | View all details of a specific task. |
| `edit <Id> ...` | Edit a task's attributes interactively. |
| `delete <Id1,Id2,...>` | Delete one or more tasks by ID. |
| `complete <Id1,Id2,...>` | Mark one or more tasks as complete. |
| `uncomplete <Id1,Id2,...>` | Mark one or more tasks as incomplete. |
| `depend add <childId> <parentId>` | Make one task dependent on another. |

### List Commands
| Command | Description |
| :--- | :--- |
| `list` | Display the scheduled tasks for the current active list. |
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

### Event Commands
| Command | Description |
| :--- | :--- |
| `event add` | Interactively add a new event. |
| `event list` | List all upcoming events. |
| `event edit <Id>` | Interactively edit an existing event. |
| `event delete <Id>` | Delete an event. |

### System Commands
| Command | Description |
| :--- | :--- |
| `mode` | Switch between `MultiAgent` and `SingleAgent` urgency modes. |
| `cleanup` | Archive completed tasks and re-index the list. |
| `time` | View the current time (real or simulated). |
| `time now` | Switch to using real-time. |
| `time custom` | Interactively set a custom simulated time. |
| `settings` | View and edit user profile settings. |
| `help` | Display a list of all commands. |
| `exit` | Close the application. |

---

### **License**

This project is licensed under the MIT License.