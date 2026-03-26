# Project Status

**Framework**: .NET 8.0 Console
**Storage**: Local JSON Files

This document provides a high-level summary of the Priority Task Manager's current capabilities. It is the distinct source of truth for what is working, what is in progress, and what is broken.

## Migration Snapshot (Scheduling Overhaul)

Current branch strategy:
1. Documentation-first migration.
2. Stabilize Gold Panning (Green Tests).
3. Implement V1 Core (Constraint Solver).
4. Migrate CLI and Verify (Dual-Mode Support).

Phase progress:
- Phase 1 (Docs and contracts): Complete.
- Phase 2 (Stabilize Gold Panning): Complete. All 70 tests are passing, covering Horizon, Prioritization, Balancing, Spreading, and Sequencing.
- Phase 3 (Critical UX & Settings Prep): Complete. Enhanced CLI with standardized interactive flows for Add, Edit, and Settings. Includes improved Time Picker (split minutes, End-of-Day shortcut) and Dependency Editor (submenu with cancel support).
- Phase 3.5 (User Feedback Refinements): Complete. Polished Event system (interactive + Smart Shift), fixed Scheduler spill-over and gaps (Constructive Fill), improved Split Task visibility.
- Phase 4 (New pipeline implementation): Ready to Start.

Recently clarified contract decisions:
- Must-schedule tasks can be marked late only when policy allows and cannot be dropped.
- Overtime is considered only after normal windows are exhausted, with scope control (must-only vs all tasks).
- Null due-date tasks remain a neutral backlog in V1 (no aging urgency yet).
- Unscheduled non-must tasks are excluded from the current run and re-evaluated on future runs unless completed, deleted, or archived.
- Adaptive horizon may emit user confirmation/alert guidance when projected timelines become long.

## Feature Matrix

| Feature Area | Status | Notes |
| :--- | :--- | :--- |
| **Task Management** | 🟢 **Stable** | Standard CRUD (add, edit, list) is robust with enhanced interactive menus for Date, Time, and Dependencies. |
| **List Management** | 🟢 **Stable** | Creating, switching, and deleting lists works as expected. |
| **Data Persistence** | 🟢 **Stable** | JSON data is correctly saved/loaded from the `Data/` directory. |
| **Settings & Config** | 🟢 **Enhanced** | Interactive `settings` to toggle strategy (Gold Panning vs Constraint), set work hours, and adjust time. |
| Scheduling Logic | 🟡 **Migration** | Constraint Solver is being added alongside the Gold Panning strategy |
| **Scheduling Contract Clarity** | 🟢 **Documented** | Lateness, overtime scope, unscheduled re-entry, and adaptive horizon advisories are now explicitly defined in docs. |
| **Event System** | 🟢 **Stable** | Interactive Add/Edit/List, Smart Shifting, Multi-delete, Global 'e' alias. |
| **Dependencies** | 🟡 **Migration** | FS dependency correctness is part of the migration test matrix and new pipeline contract. |
| **Unit Tests** | 🟡 **Active** | Core Agents covered; Integration and CLI tests pending alignment with new contract. |

## Current Capabilities

### Core Features
-   **Dual-Mode Scheduling Strategy**: Toggle between `GoldPanning` and `ConstraintSolver` (In-Progress) via Settings.
-   **Multi-Agent Scheduling**: Uses the **Gold Panning** strategy to prioritize tasks based on Due Date and Complexity, then slots them into available functionality.
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
| `mode` | Display current strategy, or set it via `mode gold` / `mode solver`. |
| `cleanup` | Archive completed tasks and re-index the list. |
| `time` | View the current time (real or simulated). |
| `time now` | Switch to using real-time. |
| `time custom` | Interactively set a custom simulated time. |
| `settings` | Interactive menu for User Profile (Work Hours, Strategy), Cleanup, and Time configuration. |
| `help` | Display a list of all commands. |
| `exit` | Close the application. |
