# Project Status

**Framework**: .NET 8.0 Console
**Storage**: Local JSON Files

This document provides a high-level summary of the Priority Task Manager's current capabilities. It is the distinct source of truth for what is working, what is in progress, and what is broken.

## Migration Snapshot (Scheduling Overhaul)

Current branch strategy:
1. Documentation-first migration.
2. Early removal of legacy scheduling paths on this branch.
3. Branch-level fallback for rollback.

Phase progress:
- Phase 1 (Docs and contracts): In progress.
- Phase 2 (Legacy path removal): Planned.
- Phase 3 (New pipeline implementation): Planned.
- Phase 4 (Migration test matrix): Planned.

## Feature Matrix

| Feature Area | Status | Notes |
| :--- | :--- | :--- |
| **Task Management** | 🟢 **Stable** | Standard CRUD (Title, Importance, Complexity, DueDate) is solid. |
| **List Management** | 🟢 **Stable** | Creating, switching, and deleting lists works as expected. |
| **Data Persistence** | 🟢 **Stable** | JSON data is correctly saved/loaded from the `Data/` directory. |
| **Scheduling Logic** | 🟡 **Migration** | Legacy scheduling is being replaced by the V1 reduced pipeline contract. |
| **Event System** | 🟡 **Limited** | Fixed blocks of time works. **No recurring events** (e.g., daily meetings). |
| **Dependencies** | 🟡 **Migration** | FS dependency correctness is part of the migration test matrix and new pipeline contract. |
| **Unit Tests** | 🟡 **Stable** | Core Agents are covered. Integration and CLI tests pending. |

## Current Capabilities

### Core Features
-   **Multi-Agent Scheduling**: Uses the `MultiAgentUrgencyStrategy` to prioritize tasks based on Due Date and Complexity, then slots them into available functionality.
-   **Command Line Interface**: A robust CLI loop (`PriorityTaskManager.CLI`) handles user input with clear feedback.
-   **Workday Configuration**: Respects user-defined start/end times in `user_profile.json`.

### Limitations (Not Bugs)
-   **Single User Only**: Designed for a single user on a local machine. No multi-user support.
-   **No Undo/Redo**: Actions are permanent immediately upon execution.
-   **No Recurring Tasks**: Tasks must be created individually.

## Known Issues & Technical Debt

*   **Migration Churn**: Scheduling internals are actively being replaced; temporary instability is expected during Phase 2 and Phase 3.
*   **Dependency Management**: FS enforcement is a priority in the new migration test matrix and target pipeline.
*   **Unit Tests**: Existing tests are still being realigned to the new scheduling contract.

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
