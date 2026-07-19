# Project Status

**Framework**: .NET 8 Console
**Storage**: Local JSON files

This document is the current-state snapshot for Priority Task Manager. It records what is working now, what is partial, what is broken, and what is under active revision.

## Status Snapshot

- The CLI project builds successfully.
- Gold Panning is the active scheduling strategy; constraint optimization is routed but not implemented.
- CLI command orchestration migration is in progress. See `docs/TODO.md` for active-work handoff and next steps.
- The test suite is under overhaul and currently needs CLI console-seam cleanup before it can be treated as green.

## Feature Matrix

| Feature Area | Status | Notes |
| --- | --- | --- |
| Task management | 🟢 Working | Core add, edit, view, complete, uncomplete, delete, and dependency commands are available through the CLI. |
| List management | 🟢 Working | Create, switch, delete, and list flows work, and lists carry copied settings for scheduling and presentation. |
| Data persistence | 🟢 Working | JSON-backed data loads and saves through the core persistence service. |
| Settings and defaults | 🟢 Working | `defaults` controls global defaults, while list-specific settings are edited on the active list. |
| Scheduling logic | 🟡 Partially implemented | Gold Panning is active; the constraint-optimization mode is routed but not implemented in the current code path. |
| Event system | 🟡 Under review | Add, edit, list, and delete are available, but the event experience is still being refined. |
| Task dependencies | 🟡 Under review | Dependency handling is supported, but correctness and migration coverage still need attention. |
| Unit tests | 🟠 Under overhaul / failing | Deterministic core-service, first-pass CLI command-surface, Gold Panning invariant, and replay coverage exist; CLI handler tests still fail where paths depend on real console clearing/cursor behavior. |

## Confirmed Capabilities

- The CLI starts from `PriorityTaskManager.CLI/Program.cs` and wires the core services at startup.
- The core library owns business logic, persistence coordination, and scheduling behavior.
- Gold Panning is the currently active scheduling strategy.
- The app stores data in JSON files and loads that state into memory on startup.
- Lists can carry copied settings snapshots instead of mutating only global defaults.
- The CLI now supports incremental command orchestration migration: result-based handlers can return `CommandResult` values that let `Program.cs` own dashboard refresh and message output.
- `DeleteHandler` and `CompleteHandler` are currently migrated to the result-based path.
- Shared parsing/usage-result behavior for migrated non-interactive handlers is centralized via `NonInteractiveCommandResultHelper`.
- Shared interactive console behavior for keyboard-driven handlers is abstracted through `IInteractiveConsoleFacade`.
- `HelpHandler`, `EditHandler`, interactive `list settings` flow, and `event edit`/`event clear` interactive paths currently use the interactive console facade seam.
- `EditHandler` interactive field updates now use row-anchored editing/toggling for strings, numeric values, duration, and booleans; due date/time use `ConsoleInputHelper` interactive pickers, with dashboard clear/rerender before picker launch and on edit-exit.

## Known Limitations

- The application is designed for a single local user.
- There is no undo/redo system.
- Recurring tasks are not supported.
- The constraint-optimization scheduling path is not implemented yet.

## Known Issues and Technical Debt

- The scheduling system still needs future refinement around slack handling, intra-day focus heuristics, and backlog fairness.
- The event workflow is functional but still under UX refinement.
- The test suite overhaul is in progress; deterministic core-service coverage, first-pass CLI handler command-surface coverage, and Gold Panning invariant/replay coverage are now in place, while console-safe CLI handler tests, deep interactive CLI flows, and dependency-order scheduling coverage remain pending.
- Most CLI handlers still directly own console rendering/refresh and remain pending migration to result-based orchestration.
- CLI command execution currently uses dual-contract dispatch in `Program.cs` and includes compatibility bridges for partially migrated handlers.
- Several interactive handlers still call console APIs directly and remain pending interactive I/O seam adoption.

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
- Test check: `dotnet test .\PriorityTaskManager.Tests\PriorityTaskManager.Tests.csproj` currently fails in CLI handler tests that reach real console clearing/cursor behavior.
- Use `docs/TODO.md` for the current active-work sequence, blockers, and next steps.
