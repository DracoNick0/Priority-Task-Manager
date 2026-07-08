# Project Status

**Framework**: .NET 8.0 Console
**Storage**: Local JSON Files

This document provides a high-level summary of the Priority Task Manager's current capabilities. It is the distinct source of truth for what is working, what is in progress, and what is broken.

## Feature Matrix

| Feature Area | Status | Notes |
| :--- | :--- | :--- |
| **Task Management** | 🟢 **Dependencies Feature Needs Review** | Standard CRUD (add, edit, list) is robust with enhanced interactive menus for Date, Time, and Dependencies. |
| **List Management** | 🟢 **Stable** | Core create/switch/delete flows work, and the list model carries copied per-list settings for sorting, scheduling mode, work hours, work days, thresholds, and simulated time. |
| **Data Persistence** | 🟢 **Stable** | JSON data is correctly saved/loaded from the `Data/` directory. |
| **Settings & Config** | 🟢 **Stable** | `defaults` now controls global defaults, including default list sort and default scheduling mode, while `list settings` edits the active list. |
| Scheduling Logic | 🟢 **Stable - New Alg in Progress** | Constraint Solver is being added alongside the Gold Panning strategy |
| **Scheduling Contract Clarity** | 🟢 **Documented** | Lateness, overtime scope, unscheduled re-entry, and adaptive horizon advisories are now explicitly defined in docs. |
| **Event System** | 🟡 **Under Review** | Interactive Add/Edit/List and Smart Shifting work, but past-event retention and the main schedule-view UX still need refinement. |
| **Task Dependencies** | 🟡 **Fixing** | FS dependency correctness is part of the migration test matrix and new pipeline contract. |
| **Unit Tests** | 🔴 **Outdated** | Core stages covered; Integration and CLI tests pending alignment with the new contract. |

## Current Capabilities

### Core Features
-   **Dual-Mode Scheduling Strategy**: Toggle between `GoldPanning` and `ConstraintSolver` (In-Progress) via `defaults`, with per-list overrides now supported in `list settings`.
-   **Staged Scheduling**: Uses the **Gold Panning** strategy to prioritize tasks based on Due Date and Complexity, then slots them into available time windows.
-   **Command Line Interface**: A robust CLI loop (`PriorityTaskManager.CLI`) handles user input with clear feedback.
-   **Workday Configuration**: Respects global defaults in `user_profile.json` and copies them into each new list.

### Limitations (Not Bugs)
-   **Single User Only**: Designed for a single user on a local machine. No multi-user support.
-   **No Undo/Redo**: Actions are permanent immediately upon execution.
-   **No Recurring Tasks**: Tasks must be created individually.

## Known Issues & Technical Debt

*   **Gold Panning Backlog**: Slack-aware urgency, focus-window sequencing, and anti-starvation logic are still pending.
*   **List Settings UX**: The interactive `list settings` editor is in place, but mock schedule support has not been added yet.
*   **Interactive Rendering Rollout (In Progress)**: Incremental menu repainting and shared day/slack selector widgets now cover the main interactive menus in `list settings`, `defaults`, `help`, `edit`, and event editing. Menu-row painting and selector widgets live in `ConsoleMenuHelper`, while dashboard rendering remains in `ConsoleHelper`.
*   **Simulated Time UX**: `list settings -> Simulated Time` now defaults to the list's currently saved mode (real-time vs custom) and reuses the saved custom datetime as the input default when editing.
*   **Background Refresh Policy**: Quarter-hour background snapshot refresh now runs only in real-time mode and is paused automatically while simulated time is active.
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
| `list settings` | View or edit the active list's copied settings. |
| `list settings sort <option>` | Set the active list's sort option. |
| `list settings strategy <gold|solver>` | Set the active list's scheduling mode. |
| `list settings hours <HH:mm-HH:mm>` | Set the active list's work hours. |
| `list settings days <Mon,Tue,...>` | Set the active list's work days. |
| `list settings slack <dire pressing focus safe>` | Set the active list's urgency thresholds. |
| `list settings time <datetime|now>` | Set or clear the active list's simulated time preference. |

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
| `defaults` | Interactive menu for global defaults copied into new lists. |
| `help` | Display a list of all commands. |
| `exit` | Close the application. |
