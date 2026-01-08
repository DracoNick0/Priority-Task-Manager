# Project Status

This document provides a high-level summary of the Priority Task Manager's current capabilities, available commands, and recent developments. It is intended to be a quick reference for understanding the application's present state.

## Current Capabilities

-   **Advanced Task Scheduling**: The application can generate a detailed, non-contiguous schedule for tasks based on their due date, complexity, and dependencies. This is powered by a sophisticated multi-agent pipeline.
-   **Task Management**: Full CRUD (Create, Read, Update, Delete) operations for tasks, including setting properties like title, description, importance, and due date.
-   **Dependency Management**: Users can create dependency chains between tasks (`depend add`). **Note:** The scheduling engine's respect for these dependencies is not fully implemented or is buggy and requires fixing.
-   **List Management**: Users can create, switch between, and delete multiple task lists to organize their work.
-   **Event Management**: Users can create fixed events (e.g., meetings), which the scheduler will block off as unavailable time.
-   **Configurable Workday**: Users can define their daily start and end work times via a `user_profile.json` file, which the scheduler uses to determine available work windows.

## Recent Developments

The last major refactoring focused on two key areas:

1.  **Scheduling Pipeline Fix**: A critical bug was fixed where the agent pipeline was corrupting the final schedule. The agent execution order was corrected to a more logical **Prioritize -> Balance -> Schedule** workflow, which is now documented in `docs/ARCHITECTURE.md`.
2.  **Data Persistence Decoupling**: The application's `.json` data files were moved from the `PriorityTaskManager.CLI` project into a `Data/` folder in the core `PriorityTaskManager` library. The `PersistenceService` was updated to be more flexible, removing its dependency on the CLI project's file structure.

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
| `depend add <childId> <parentId>` | Make one task dependent on another. |

### List Commands
| Command | Description |
| :--- | :--- |
| `list` | Display the scheduled tasks for the current active list. |
| `list all` | Show all available lists and which one is active. |
| `list create <Name>` | Create a new task list. |
| `list switch [Name]` | Switch the active list. |
| `list delete <Name>` | Delete a list and all of its tasks. |

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
