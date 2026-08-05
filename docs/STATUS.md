# Project Status

**Framework**: .NET 8 Console
**Storage**: Local JSON files

This document is the current-state snapshot for Priority Task Manager. It records what is working now, what is partial, what is broken, and what is under active revision.

## Status Snapshot

- The CLI project builds successfully.
- Gold Panning is the active scheduling strategy; constraint optimization is routed but not implemented.
- CLI command dispatch is consolidated onto a single `ICommandResultHandler` contract; see the repository's GitHub Issues for remaining testing-overhaul work.
- The test suite passes. CLI handler tests no longer fail on real console clearing/cursor behavior; broader interactive seam adoption and dependency-order scheduling coverage remain pending.

## Feature Matrix

| Feature Area | Status | Notes |
| --- | --- | --- |
| Task management | 🟢 Working | Core add, edit, view, complete, uncomplete, delete, and dependency commands are available through the CLI. |
| List management | 🟢 Working | Create, switch, delete, and list flows work, and lists carry copied settings for scheduling and presentation. |
| Data persistence | 🟢 Working | JSON-backed data loads and saves through the core persistence service. |
| Settings and defaults | 🟢 Working | `defaults` controls global defaults, while list-specific settings are edited on the active list. |
| Scheduling logic | 🟡 Partially implemented | Gold Panning is active; the constraint-optimization mode is routed but not implemented in the current code path. |
| Event system | 🟡 Under review | Add, edit, list, and delete are available, but the event experience is still being refined. |
| Task dependencies | � Working | Dependency add/remove is supported, and the active Gold Panning pipeline now enforces dependency-order placement (a dependent task is never scheduled before its prerequisite completes). |
| Unit tests | � Passing / under overhaul | Deterministic core-service, first-pass CLI command-surface, Gold Panning invariant/replay coverage (including dependency-order scheduling), exist. Broader interactive seam adoption remains pending. |
| Networked API | 🟡 Partially implemented | `PriorityTaskManager.API` has account/JWT auth (see [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md)) and minimal REST endpoints for task/list/event CRUD wrapping `TaskManagerService`; no client (CLI or Flutter) authenticates against it yet (tracked by issue #44). |
| Flutter client | 🟡 Partially implemented | `PriorityTaskManager.Flutter/` is a local-only (guest/offline) web + Windows desktop shell with Riverpod state management and a Hive-backed `TaskRepository` implementation; supports add/edit/complete/delete and dependency management. No login or API-backed repository yet (tracked by issue #44). |

## Confirmed Capabilities

- The CLI starts from `PriorityTaskManager.CLI/Program.cs` and wires the core services at startup.
- The core library owns business logic, persistence coordination, and scheduling behavior.
- Gold Panning is the currently active scheduling strategy.
- The app stores data in JSON files and loads that state into memory on startup.
- Lists can carry copied settings snapshots instead of mutating only global defaults.
- The CLI now supports command orchestration through a single canonical contract: every wired handler implements `ICommandResultHandler` and returns `CommandResult` values that let `Program.cs` own dashboard refresh and message output.
- `DeleteHandler`, `CompleteHandler`, `UncompleteHandler`, `DependHandler`, `TimeHandler`, `ModeHandler`, `CleanupHandler`, `AddHandler`, `ViewHandler`, and the flag-based branch of `SettingsHandler` build real `CommandResult` outcomes (status, message, dashboard-refresh flag).
- `HelpHandler`, `EditHandler`, `ListHandler`, and `EventCommandHandler` also implement `ICommandResultHandler`, but return an inert `CommandResult` (no message, no refresh) because these handlers already own their console rendering end-to-end through `IInteractiveConsoleFacade`. `Program.cs` does no extra work for them beyond what already happened before migration.
- `SettingsHandler`'s no-arg (`defaults`) branch also renders through `IInteractiveConsoleFacade` and returns an inert `CommandResult`; its flag-based (`--default-*`) branch returns a real `CommandResult` built from parsed arguments.
- `EventHandler` (currently unused/unwired in `Program.cs`; `event`/`e` route to `EventCommandHandler` instead) has the same `ICommandResultHandler` shape for contract consistency.
- Shared parsing/usage-result behavior for migrated non-interactive handlers is centralized via `NonInteractiveCommandResultHelper`.
- Shared interactive console behavior for keyboard-driven handlers is abstracted through `IInteractiveConsoleFacade`.
- `HelpHandler`, `EditHandler`, interactive `list settings` flow, interactive `list switch` flow, the interactive `defaults` menu (`SettingsHandler`), and `event add`/`event edit`/`event clear` interactive paths currently use the interactive console facade seam.
- `EditHandler` interactive field updates now use row-anchored editing/toggling for strings, numeric values, duration, and booleans; due date/time use `ConsoleInputHelper` interactive pickers, with dashboard clear/rerender before picker launch and on edit-exit.
- `PriorityTaskManager.API` exposes authenticated REST endpoints (`/api/tasks`, `/api/lists`, `/api/events`) for CRUD, each built only on `TaskManagerService` calls per the integrations boundary; no scheduling or persistence logic is duplicated in the API layer.
- `PriorityTaskManager.Flutter/` is a separate Dart/Flutter codebase (web + Windows desktop targets scaffolded; macOS/Linux desktop folders present but unverified) implementing its own local `TaskRepository` abstraction backed by Hive, independent of the .NET solution; it does not call into `PriorityTaskManager`/`PriorityTaskManager.API` for this offline/guest shell.

## Known Limitations

- The application is designed for a single local user.
- There is no undo/redo system.
- Recurring tasks are not supported.
- The constraint-optimization scheduling path is not implemented yet.

## Known Issues and Technical Debt

- The scheduling system still needs future refinement around slack handling, intra-day focus heuristics, and backlog fairness.
- The event workflow is functional but still under UX refinement.
- The test suite overhaul is in progress; deterministic core-service coverage, first-pass CLI handler command-surface coverage, Gold Panning invariant/replay coverage, dependency-order scheduling coverage, and console-handle-safe CLI handler tests are now in place, while deep interactive CLI flows remain pending.
- Most CLI handlers still directly own console rendering/refresh and remain pending migration to result-based orchestration.
- `ConsoleHelper.ClearAndRenderDashboard` tolerates environments with no attached console handle (e.g. test hosts) as a safety net.
- The interactive `defaults` menu (`SettingsHandler`), `list switch` (`ListHandler`), and `event add`/`event edit`/`event clear` (`EventCommandHandler`) flows have adopted the `IInteractiveConsoleFacade` seam; remaining direct-console interactive flows are limited to simple, non-menu confirm/read-line prompts (e.g. `AddHandler`'s no-arg task creation, `list delete`/`cleanup` confirmations), which are out of scope for the facade seam by design.
- Task, list, and event `Id` values are now globally unique `Guid`s (see [docs/ARCHITECTURE_DATA.md](ARCHITECTURE_DATA.md)); `TaskItem.DisplayId` remains the short, user-facing sequential identifier for task commands. Events have no `DisplayId` equivalent, so `event`/`e` edit and remove flows currently require typing full GUID strings — a known UX gap, not yet scoped for a fix.

## Command Surface Summary

### Top-level commands

| Command | Purpose |
| --- | --- |
| `add` | Add a new task |
| `list` | Show tasks for the active list |
| `edit` | Edit an existing task |
| `delete` | Delete tasks |
| `complete` | Mark tasks complete |
| `uncomplete` | Mark tasks incomplete |
| `depend` | Manage task dependencies |
| `view` | Show task details |
| `cleanup` | Archive completed tasks and refresh list state |
| `help` | Show command help |
| `defaults` | Edit global default settings |
| `event` | Manage events |
| `e` | Shortcut for `event` |
| `time` | View or change simulated time |
| `mode` | View or change scheduling mode |
| `exit` | Quit the CLI |

### Common subcommand groups

| Group | Examples |
| --- | --- |
| `list` | `list all`, `list create`, `list switch`, `list delete`, `list settings` |
| `event` | `event add`, `event list`, `event edit`, `event delete` |
| `time` | `time now`, `time custom` |
| `defaults` | Interactive menu for global defaults |

## Validation Notes

- Build check: `dotnet build .\PriorityTaskManager.CLI\PriorityTaskManager.CLI.csproj` succeeds.
- Build check: `dotnet build .\PriorityTaskManager.API\PriorityTaskManager.API.csproj` succeeds.
- Test check: `dotnet test .\PriorityTaskManager.Tests\PriorityTaskManager.Tests.csproj` passes (146 passed, 1 skipped).
- Build check: `flutter build web` and `flutter build windows` succeed in `PriorityTaskManager.Flutter/`; `flutter analyze` and `flutter test` pass.
- Use the repository's GitHub Issues for the current active-work sequence, blockers, and next steps.
