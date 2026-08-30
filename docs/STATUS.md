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
| Networked API | � Working | `PriorityTaskManager.API` has account/JWT auth (see [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md)) and minimal REST endpoints for task/list/event CRUD wrapping core services. The Flutter client authenticates against it (issue #44 core). Scheduling is now exclusively served through the authenticated, Subscription-tier-gated `/api/schedule` route (issue #41): the unauthenticated `/api/local/schedule` route and the `LocalOnly` startup flag have been removed, and every request now requires Postgres. `Account.SubscriptionTier` (`Free`/`Subscription`) is carried as a JWT claim; Development-environment startup seeds fixed `free@dev.local`/`subscriber@dev.local` test accounts (see `PriorityTaskManager.API/Dev/DevAccountSeeder.cs`). This login->JWT->authenticated-`/api/schedule` round trip has been live-smoke-tested end to end against a real local Postgres instance (2026-08-30), including confirming Free-tier callers get `403 Forbidden`. New account registration (`POST /api/auth/register`) defaults to Subscription tier (not Free) during the MVP/beta grace period, config-driven via `BetaGracePeriod:DefaultNewAccountsToSubscription` (issue #50); the auth response carries a `BetaGracePeriodNotice` for a future client to display. |

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
| List management | � Integrated | List switching, create, delete, and a settings form (name/description plus per-list scheduling overrides) are all supported through the Left Rail and Right Inspector. |
| Data persistence | 🟢 Integrated | Local persistence is Hive-backed, independent of the .NET JSON persistence. |
| Settings and defaults | 🟢 Integrated | A global defaults form (Left Rail Settings) and per-list overrides (sort option, scheduling mode, work hours/days, urgency thresholds) are Hive-backed; unset list fields inherit the global defaults, mirroring `TaskList.ApplyDefaultsFrom`. |
| Scheduling logic | 🟢 Integrated | The Command Center computes real schedules by sending Hive task/event data and the active list's effective settings to a separately-running `PriorityTaskManager.API` instance, not mock data (tracked by issue #47), via the authenticated, Subscription-gated `/api/schedule` route (issue #41; see [WORKFLOW.md](WORKFLOW.md)). Scheduling is online-exclusive: logged-in accounts see the computed Daily Column pipeline, while Guests (no account) see a plain, user-sortable task list (`GuestTaskList`: Importance/Due Date/Alphabetical) instead, since they have no access to online-only features (see [VISION.md](VISION.md)). The client no longer spawns the API process itself; it must already be running. |
| Event system | 🟢 Integrated | Events are Hive-backed (add/edit/list/delete) and are sent to the local schedule-compute API as fixed, unmovable blocks when computing the schedule. |
| Task dependencies | 🟢 Integrated | Dependency management is supported in the inline task inspector form. |
| Networked API | 🟢 Integrated | Real login/register screens, secure JWT storage (`flutter_secure_storage`), and Guest-vs-Authenticated session state are implemented (issue #44 core), with a guest-first entry flow (one-time "Continue as Guest" vs. "Log in / Create account" choice, never a forced login). Guests can later opt into logging in from a Left Rail account control. Logging in from Guest only switches session state; it does not yet migrate local Hive task/list data to the account (tracked by issue #46, targeted for V1). Task/list/event data still lives only in the local Hive store — there is no API-backed repository for it yet. The dev-only, define-gated auto-login shim (`PTM_DEV_AUTOLOGIN`) has been removed (issue #41 cleanup); testing the authenticated route now goes through real login with a seeded dev account. |

## Confirmed Capabilities

- The CLI starts up and loads existing data automatically.
- Gold Panning is the currently active scheduling strategy.
- Data is stored in local JSON files and persists between runs.
- Lists can carry their own settings snapshot instead of always inheriting only global defaults.
- Every command produces clear feedback: a success message, a warning, usage guidance, or an actionable error.
- `PriorityTaskManager.API` exposes authenticated REST endpoints (`/api/tasks`, `/api/lists`, `/api/events`) for CRUD, and an authenticated, Subscription-tier-gated `/api/schedule` endpoint that computes a real Gold Panning (or constraint-optimization) schedule from posted task data for callers with a `Subscription`-tier JWT claim, without persisting anything server-side. Scheduling has no unauthenticated route; every request requires Postgres.
- `PriorityTaskManager.Flutter/` is a local-only (guest/offline) web + Windows desktop client with its own task/list/event/settings CRUD, supporting add/edit/complete/delete, dependency management, per-list scheduling overrides, and global defaults. Its single-screen "Three-Pane Command Center" (Left Rail, Center Stage, Right Inspector) computes real schedules for logged-in accounts — including fixed events and the active list's effective settings — by calling a separately-running local `PriorityTaskManager.API` instance's authenticated `/api/schedule` route rather than using mock data (tracked by issue #47); Guests see a plain, user-sortable task list instead, since scheduling is online-exclusive. The client does not start or manage this API process itself (see [WORKFLOW.md](WORKFLOW.md) for how to run it during development). Real login/register screens and Guest-vs-Authenticated session state exist (issue #44 core); an API-backed repository for tasks/lists/events does not yet exist, and guest-to-account data migration is tracked separately by issue #46.
- The Flutter Command Center layout: a persistent Left Rail (list switcher, Archive/Settings nav, Engine Status clock/mode indicator), a horizontally scrolling Center Stage (daily columns — Today, Tomorrow, ..., Unscheduled — with scheduled task cards and fixed event cards), and a Right Inspector with an inline CRUD form for the selected task, event, list (including its scheduling-settings overrides), or the global defaults. At narrow/medium window widths the Left Rail and/or Right Inspector collapse into drawer overlays; each docked pane can be resized or dragged into a drawer, with a button to restore it. Selecting an item while the inspector isn't docked auto-opens the drawer. Multi-line inputs (e.g. task descriptions) have a drag handle to resize their height.

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
