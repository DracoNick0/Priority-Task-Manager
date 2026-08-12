# Project Status

**Framework**: .NET 8 Console
**Storage**: Local JSON files

This document is the current-state snapshot for Priority Task Manager. It records what is working now, what is partial, what is broken, and what is under active revision.

## Status Snapshot

- The CLI runs and all documented commands work.
- Gold Panning is the active scheduling strategy; constraint optimization is not yet available as a selectable mode.
- The test suite passes. See the repository's GitHub Issues for remaining testing-overhaul work.

## Feature Matrix

### Core (C# backend)

`PriorityTaskManager` is the shared business-logic library used by the CLI and API.

| Feature Area | Status | Notes |
| --- | --- | --- |
| Task management | 🟢 Working | Add, edit, view, complete, uncomplete, and delete are implemented in core services. |
| List management | 🟢 Working | Create, switch, delete, and settings flows work, and lists carry copied settings for scheduling and presentation. |
| Data persistence | 🟢 Working | JSON-backed data loads and saves through the core persistence service. |
| Settings and defaults | 🟢 Working | Global defaults and per-list settings overrides are both supported. |
| Scheduling logic | 🟡 Partially implemented | Gold Panning is active; the constraint-optimization mode is routed but not implemented in the current code path. |
| Event system | 🟢 Working | Add, edit, list, and delete are implemented in core services. |
| Task dependencies | 🟢 Working | Dependency add/remove is supported, and the active Gold Panning pipeline enforces dependency-order placement (a dependent task is never scheduled before its prerequisite completes). |
| Unit tests | 🟢 Passing / under overhaul | Deterministic core-service and Gold Panning invariant/replay coverage (including dependency-order scheduling) exist. See the repository's GitHub Issues for remaining testing-overhaul work. |
| Networked API | 🟡 Partially implemented | `PriorityTaskManager.API` has account/JWT auth (see [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md)) and minimal REST endpoints for task/list/event CRUD wrapping core services; no client (CLI or Flutter) authenticates against it yet (tracked by issue #44). A separate unauthenticated `/api/local/schedule` route runs the real scheduling strategies statelessly for the offline Flutter client (see [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md)). |

### CLI integration

Tracks how much of the Core feature set is exposed through `PriorityTaskManager.CLI`.

| Feature Area | Status | Notes |
| --- | --- | --- |
| Task management | 🟢 Integrated | `add`, `edit`, `view`, `complete`, `uncomplete`, and `delete` commands are available. |
| List management | 🟢 Integrated | `list create`, `list switch`, `list delete`, and `list settings` are available. |
| Data persistence | 🟢 Integrated | Data loads and saves automatically on CLI startup/exit. |
| Settings and defaults | 🟢 Integrated | `defaults` controls global defaults, while list-specific settings are edited on the active list. |
| Scheduling logic | 🟡 Partially integrated | `mode` switches scheduling mode, but constraint optimization is not yet a usable mode. |
| Event system | 🟡 Under review | `event`/`e` add, edit, list, and delete are available, but the event experience is still being refined. |
| Task dependencies | 🟢 Integrated | `depend` manages dependencies. |
| Networked API | ⚪ Not integrated | The CLI does not call `PriorityTaskManager.API`; it uses core services directly in-process. |

### Flutter client integration

Tracks how much of the Core feature set is exposed through `PriorityTaskManager.Flutter` (local-only guest/offline web + Windows desktop client).

| Feature Area | Status | Notes |
| --- | --- | --- |
| Task management | 🟢 Integrated | Add, edit, complete, and delete are supported against the Hive-backed local repository. |
| List management | 🟡 Partially integrated | List switching is supported through the Left Rail; create/delete/settings flows are still limited (tracked by ongoing Flutter issues). |
| Data persistence | 🟢 Integrated | Local persistence is Hive-backed, independent of the .NET JSON persistence. |
| Settings and defaults | 🔴 Not yet integrated | No defaults/settings screen exists in the Flutter client yet. |
| Scheduling logic | 🟢 Integrated | The Command Center computes real schedules by sending Hive task data to a locally spawned `PriorityTaskManager.API` sidecar (`/api/local/schedule`), not mock data (tracked by issue #47). |
| Event system | 🔴 Not yet integrated | Core event CRUD exists, but the Flutter client has no event UI yet. |
| Task dependencies | 🟢 Integrated | Dependency management is supported in the inline task inspector form. |
| Networked API | 🔴 Not yet integrated | No login or API-backed repository yet; the client only reaches the local `/api/local/schedule` sidecar (tracked by issue #44). |

## Confirmed Capabilities

- The CLI starts up and loads existing data automatically.
- Gold Panning is the currently active scheduling strategy.
- Data is stored in local JSON files and persists between runs.
- Lists can carry their own settings snapshot instead of always inheriting only global defaults.
- Every command produces clear feedback: a success message, a warning, usage guidance, or an actionable error.
- `PriorityTaskManager.API` exposes authenticated REST endpoints (`/api/tasks`, `/api/lists`, `/api/events`) for CRUD, and an unauthenticated, stateless `/api/local/schedule` endpoint that computes a real Gold Panning (or constraint-optimization) schedule from posted task data without persisting anything server-side.
- `PriorityTaskManager.Flutter/` is a local-only (guest/offline) web + Windows desktop client with its own task/list/event CRUD, supporting add/edit/complete/delete and dependency management. Its single-screen "Three-Pane Command Center" (Left Rail, Center Stage, Right Inspector) computes real schedules by calling the local `PriorityTaskManager.API` sidecar rather than using mock data (tracked by issue #47). No login or API-backed repository yet (tracked by issue #44).
- The Flutter Command Center layout: a persistent Left Rail (list switcher, Archive/Settings nav, Engine Status clock/mode indicator), a horizontally scrolling Center Stage (daily columns — Today, Tomorrow, ..., Unscheduled — with scheduled task cards and fixed event cards), and a Right Inspector with an inline CRUD form for the selected task, event, or list. At narrow/medium window widths the Left Rail and/or Right Inspector collapse into drawer overlays; each docked pane can be resized or dragged into a drawer, with a button to restore it. Selecting an item while the inspector isn't docked auto-opens the drawer. Multi-line inputs (e.g. task descriptions) have a drag handle to resize their height.

## Known Limitations

- The application is designed for a single local user.
- There is no undo/redo system.
- Recurring tasks are not supported.
- The constraint-optimization scheduling mode is not implemented yet.

## Known Issues and Technical Debt

- The scheduling system still needs future refinement around slack handling, intra-day focus heuristics, and backlog fairness.
- The event workflow is functional but still under UX refinement.
- The test suite overhaul is in progress; see the repository's GitHub Issues for current scope and remaining work.
- Task, list, and event `Id` values are globally unique `Guid`s; `TaskItem.DisplayId` remains the short, user-facing sequential identifier for task commands. Events have no `DisplayId` equivalent, so `event`/`e` edit and remove flows currently require typing full GUID strings — a known UX gap, not yet scoped for a fix.

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
