# Project Status

**Framework**: .NET 10 (ASP.NET Core API) and Flutter (web + Windows)
**Storage**: Postgres (API/hosted), Hive (Flutter local data)

This document is the current-state snapshot for Priority Task Manager. It records what is working now, what is partial, what is broken, and what is under active revision.

## Status Snapshot

- The CLI has been retired from the product and is no longer supported.
- `PriorityTaskManager.API` is deployed to a real, publicly reachable hosted instance backed by Postgres, providing account/JWT auth and an authenticated, Subscription-tier-gated schedule-computation endpoint. This gives the app a genuine networked path to its core differentiator (computed scheduling), not just a per-developer local process. See [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md) for hosting/deployment details.
- The Flutter client (web + Windows) has real login/register and a guest-first entry flow, and can be configured at build time to target either a local development API or the hosted production API.
- Gold Panning is the active scheduling strategy; constraint optimization is not yet available as a selectable mode.
- The active test suite is under overhaul; the latest .NET run has one known unrelated archive-persistence failure. See the repository's GitHub Issues for remaining testing-overhaul work.

## Feature Matrix

### Core (C# backend)

`PriorityTaskManager` is the shared business-logic library used by the API.

| Feature Area | Status | Notes |
| --- | --- | --- |
| Task management | 🟢 Working | Add, edit, view, complete, uncomplete, archive, restore, and permanent deletion from the archive are implemented. Recurring task series also persist per-date completion counts, missed/skipped/disregarded states, progression mode, missed-indicator preference, and optional current/best streaks. |
| List management | 🟢 Working | Create, switch, delete, and settings flows work, and lists carry copied settings for scheduling and presentation. |
| Data persistence | 🟢 Working | The API persists account data in Postgres; the Flutter Guest store uses Hive. JSON persistence remains only in the archived CLI source. |
| Settings and defaults | 🟢 Working | Global defaults and per-list settings overrides are both supported. |
| Scheduling logic | 🟡 Partially implemented | Gold Panning is active; the constraint-optimization mode is routed but not implemented in the current code path. |
| Event system | 🟢 Working | Recurring events can be edited or have one occurrence, this and following occurrences, or the whole series archived and restored. Exceptions and moved occurrences retain their original series date; scheduling expands them on demand. |
| Task dependencies | 🟢 Working | Dependency add/remove is supported, and the active Gold Panning pipeline enforces dependency-order placement (a dependent task is never scheduled before its prerequisite completes). |
| Unit tests | 🟡 Passing with one known failure / under overhaul | Deterministic core-service and Gold Panning invariant/replay coverage (including dependency-order scheduling) exist. The latest .NET run has one unrelated archive-persistence failure; see Validation Notes. |
| Networked API | 🟢 Working | `PriorityTaskManager.API` has account/JWT auth and authenticated task/list/event CRUD. Task APIs accept and return recurring rule/progression preferences, return per-date occurrence state, and expose occurrence complete/undo/skip operations. Scheduling remains exclusively served through the authenticated, Subscription-tier-gated `/api/schedule` route: no unauthenticated schedule route exists, and every request requires Postgres. |

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
| Task management | 🟡 Partially integrated | One-off add, edit, complete, and delete are supported. Authenticated recurring occurrences show their scheduled date, completion progress, missed state, optional streaks, and complete/undo/skip actions. Guest deletion is permanent; authenticated deletion archives the task. Recurrence setup and series/occurrence editing forms are not yet available. |
| List management | 🟢 Integrated | List switching, create, delete, and a settings form (name/description plus per-list scheduling overrides) are all supported through the Left Rail and Right Inspector. |
| Data persistence | 🟢 Integrated | Guest data is Hive-backed; authenticated account data is stored through the API rather than the archived CLI's JSON persistence. |
| Settings and defaults | 🟢 Integrated | A global defaults form (Left Rail Settings) and per-list overrides (sort option, scheduling mode, work hours/days, urgency thresholds) are Hive-backed; unset list fields inherit the global defaults, mirroring `TaskList.ApplyDefaultsFrom`. A per-list frozen simulated-time override is also editable here and is threaded into `/api/schedule` calls for Authenticated sessions; it has no effect for Guests, since they never compute a schedule. |
| Scheduling logic | 🟡 Partially integrated | The Command Center computes real schedules through the authenticated, Subscription-gated `/api/schedule` route. Authenticated recurring task dates are expanded into occurrence placeholders before scheduling; scheduled task chunks and fixed events appear in chronological order in each daily column, and split tasks show their part number across the schedule. Guest users see a plain, user-sortable one-off task list and have no recurring-task support. |
| Event system | 🟢 Integrated | Guests use Hive-backed plain events and delete them permanently. Authenticated deletion archives a plain event or the selected scope of a recurring series; occurrences from the same series are grouped and can be restored together. Authenticated sessions can create recurring series, view upcoming occurrences, and edit or archive one occurrence, this and following, or the whole series. Edited occurrences can move to another date without losing their original series identity. The inspector also offers a replacement recurrence pattern for future or whole-series edits. |
| Task dependencies | 🟢 Integrated | Dependency management is supported in the inline task inspector form. |
| Networked API | 🟢 Integrated | Real login/register screens, secure JWT storage (`flutter_secure_storage`), and Guest-vs-Authenticated session state are implemented (issue #44 core), with a guest-first entry flow (one-time "Continue as Guest" vs. "Log in / Create account" choice, never a forced login). Guests can later opt into logging in from a Left Rail account control. Expired or server-rejected sessions return to Guest mode; temporary server/network failures do not clear the session. Logging in from Guest only switches session state; it does not yet migrate local Hive task/list data to the account (tracked by issue #46, targeted for V1). Authenticated sessions now use an API-backed `TaskRepository` (calling `/api/tasks`/`/api/lists`/`/api/events`/`/api/profile`) instead of local Hive storage; Guests still use the local Hive store. Server-side events are not list-scoped (unlike local `FixedEvent`), so `/api/events` returns/accepts all of an account's events regardless of list — a known limitation. The dev-only, define-gated auto-login shim (`PTM_DEV_AUTOLOGIN`) has been removed (issue #41 cleanup); testing the authenticated route now goes through real login with a seeded dev account. |

## Confirmed Capabilities

- Gold Panning is the currently active scheduling strategy.
- Authenticated users can complete recurring task occurrences in configurable per-occurrence counts, retain or disregard missed occurrences, skip occurrences, undo completion progress, and optionally track current/best streaks. The Flutter client can display and progress occurrences but does not yet provide recurring-task setup or series/occurrence editing forms.
- Lists can carry their own settings snapshot instead of always inheriting only global defaults.
- `PriorityTaskManager.API` exposes authenticated REST endpoints (`/api/tasks`, `/api/lists`, `/api/events`, `/api/profile`) for CRUD, and an authenticated, Subscription-tier-gated `/api/schedule` endpoint that computes a real Gold Panning (or constraint-optimization) schedule from posted task data for callers with a `Subscription`-tier JWT claim, without persisting anything server-side. Scheduling has no unauthenticated route; every request requires Postgres. Task/event delete endpoints archive items for authenticated users. The authenticated `/api/archive` surface lists and restores tasks/events, permanently deletes individual archived items, and clears the archive; task restore defaults to the original list and requires an explicit `targetListId` (returning `409 Conflict`) if that list no longer exists.
- `PriorityTaskManager.Flutter/` is a web + Windows desktop client with its own task/list/event/settings CRUD, supporting add/edit/complete/delete, dependency management, per-list scheduling overrides, and global defaults. Its single-screen "Three-Pane Command Center" (Left Rail, Center Stage, Right Inspector) computes real schedules for logged-in accounts — including fixed events and the active list's effective settings — by calling a separately-running local `PriorityTaskManager.API` instance's authenticated `/api/schedule` route rather than using mock data (tracked by issue #47); Guests see a plain, user-sortable task list instead, since scheduling is online-exclusive. The client does not start or manage this API process itself (see [WORKFLOW.md](WORKFLOW.md) for how to run it during development). Real login/register screens and Guest-vs-Authenticated session state exist (issue #44 core); authenticated sessions now persist tasks/lists/events/profile through an API-backed repository instead of local Hive storage, and guest-to-account data migration is tracked separately by issue #46.
- The Flutter Command Center layout: a persistent Left Rail (list switcher, Archive/Settings nav, Engine Status clock/mode indicator), a horizontally scrolling Center Stage (daily columns — Today, Tomorrow, ..., Unscheduled when tasks are present, and Completed), and a Right Inspector with an inline CRUD form for the selected task, event, list (including its scheduling-settings overrides), or the global defaults. Scheduled task chunks and events are interleaved by start time within each day; split-task part numbers make continuations visible across events and day columns. At narrow/medium window widths the Left Rail and/or Right Inspector collapse into drawer overlays; each docked pane can be resized or dragged into a drawer, with a button to restore it. Selecting an item while the inspector isn't docked auto-opens the drawer. Multi-line inputs (e.g. task descriptions) have a drag handle to resize their height.
- Task and event forms reject empty or whitespace-only names with an inline field warning and the standard warning notification instead of creating an unnamed item.
- The Left Rail's Archive nav item opens a dialog listing individual archived items and grouped task/event items with per-item Restore and permanent-delete actions, a Restore all action for multi-item groups, and a confirmed Clear archive action. Archive is Authenticated-only: Guests get an in-app message instead of the dialog, and `LocalTaskRepository`'s archive methods fail fast (`UnsupportedError`) since the UI never calls them for a Guest session. If a task group's original list no longer exists, restoring prompts the user to pick a target list before retrying.
- In debug builds, a "Dev Log" entry in the Left Rail opens a resizable, dockable bottom panel (similar to an IDE's terminal/debug console) listing recent API calls and Guest domain-event mutations (timestamp, success/failure, duration), for diagnosing integration issues without attaching a debugger (issue #59). It is not present in release builds.

## Known Limitations

- There is no undo/redo system.
- Recurring-task setup and per-occurrence edit/delete forms are not yet available in Flutter; Guest/local recurring tasks are unsupported. Core task-service scheduling reconciles missed state, while recurrence setup and editing UX remain separate work.
- The constraint-optimization scheduling mode is not implemented yet.
- The event pipeline shows occurrences in the current 14-day window; it does not provide a historical series browser. Event timestamps use local wall-clock dates without an explicit event time-zone identifier.

## Known Issues and Technical Debt

- The scheduling system still needs future refinement around slack handling, intra-day focus heuristics, and backlog fairness.
- The event workflow is functional but still under UX refinement.
- The test suite overhaul is in progress; see the repository's GitHub Issues for current scope and remaining work.
## Validation Notes

- Build check: `dotnet build .\Priority-Task-Manager.sln` succeeds; the archived CLI is excluded.
- Test check: `dotnet test .\PriorityTaskManager.Tests\PriorityTaskManager.Tests.csproj` excludes archived CLI tests; the latest run reports 205 passed, 1 skipped, and 1 known unrelated failure in `PersistenceServiceTests.ArchiveTasks_AppendsArchivedTasksToArchiveFile`.
- Build check: `flutter build web` and `flutter build windows` succeed in `PriorityTaskManager.Flutter/`; `flutter analyze` and `flutter test` pass.
- Live check: register, login, and an authenticated schedule computation have been verified end to end against the hosted production API, including a Flutter Windows client build pointed at the hosted instance.
- Use the repository's GitHub Issues for the current active-work sequence, blockers, and next steps.
