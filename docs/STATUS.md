# Project Status

**Framework**: .NET 10 (ASP.NET Core API) and Flutter (web + Windows)
**Storage**: Postgres (API/hosted), Hive (Flutter local data)

This document is the current-state snapshot for Priority Task Manager. It records what is working now, what is partial, what is broken, and what is under active revision.

## Status Snapshot

- The CLI has been retired from the product and is no longer supported.
- `PriorityTaskManager.API` is deployed to a real, publicly reachable hosted instance backed by Postgres, providing account/JWT auth and an authenticated, Subscription-tier-gated schedule-computation endpoint. This gives the app a genuine networked path to its core differentiator (computed scheduling), not just a per-developer local process. See [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md) for hosting/deployment details.
- The Flutter client (web + Windows) has real login/register and a guest-first entry flow, and can be configured at build time to target either a local development API or the hosted production API.
- Gold Panning is the active scheduling strategy; constraint optimization is not yet available as a selectable mode.
- The test suite passes. See the repository's GitHub Issues for remaining testing-overhaul work.

## Feature Matrix

### Core (C# backend)

`PriorityTaskManager` is the shared business-logic library used by the API.

| Feature Area | Status | Notes |
| --- | --- | --- |
| Task management | 🟢 Working | Add, edit, view, complete, uncomplete, archive, restore, and permanent deletion from the archive are implemented in core services. |
| List management | 🟢 Working | Create, switch, delete, and settings flows work, and lists carry copied settings for scheduling and presentation. |
| Data persistence | 🟢 Working | The API persists account data in Postgres; the Flutter Guest store uses Hive. JSON persistence remains only in the archived CLI source. |
| Settings and defaults | 🟢 Working | Global defaults and per-list settings overrides are both supported. |
| Scheduling logic | 🟡 Partially implemented | Gold Panning is active; the constraint-optimization mode is routed but not implemented in the current code path. |
| Event system | 🟢 Working | Recurring events can be edited or have one occurrence, this and following occurrences, or the whole series archived and restored. Exceptions and moved occurrences retain their original series date; scheduling expands them on demand. |
| Task dependencies | 🟢 Working | Dependency add/remove is supported, and the active Gold Panning pipeline enforces dependency-order placement (a dependent task is never scheduled before its prerequisite completes). |
| Unit tests | 🟢 Passing / under overhaul | Deterministic core-service and Gold Panning invariant/replay coverage (including dependency-order scheduling) exist. See the repository's GitHub Issues for remaining testing-overhaul work. |
| Networked API | 🟢 Working | `PriorityTaskManager.API` has account/JWT auth (see [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md)) and minimal REST endpoints for task/list/event CRUD wrapping core services. The Flutter client authenticates against it (issue #44 core). Scheduling is now exclusively served through the authenticated, Subscription-tier-gated `/api/schedule` route (issue #41): the unauthenticated `/api/local/schedule` route and the `LocalOnly` startup flag have been removed, and every request now requires Postgres. `Account.SubscriptionTier` (`Free`/`Subscription`) is carried as a JWT claim; Development-environment startup seeds fixed `free@dev.local`/`subscriber@dev.local` test accounts (see `PriorityTaskManager.API/Dev/DevAccountSeeder.cs`). This login->JWT->authenticated-`/api/schedule` round trip has been live-smoke-tested end to end against a real local Postgres instance (2026-08-30), including confirming Free-tier callers get `403 Forbidden`. New account registration (`POST /api/auth/register`) defaults to Subscription tier (not Free) during the MVP/beta grace period, config-driven via `BetaGracePeriod:DefaultNewAccountsToSubscription` (issue #50); the auth response carries a `BetaGracePeriodNotice` for a future client to display. |

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
| Task management | 🟢 Integrated | Add, edit, complete, and delete are supported. Guest deletion is permanent; authenticated deletion archives the task. New tasks default to the next configured workday's end of work; authenticated lists use simulated time when enabled. A `Not before` time after `due date - estimated duration` is cleared with a warning. |
| List management | 🟢 Integrated | List switching, create, delete, and a settings form (name/description plus per-list scheduling overrides) are all supported through the Left Rail and Right Inspector. |
| Data persistence | 🟢 Integrated | Guest data is Hive-backed; authenticated account data is stored through the API rather than the archived CLI's JSON persistence. |
| Settings and defaults | 🟢 Integrated | A global defaults form (Left Rail Settings) and per-list overrides (sort option, scheduling mode, work hours/days, urgency thresholds) are Hive-backed; unset list fields inherit the global defaults, mirroring `TaskList.ApplyDefaultsFrom`. A per-list frozen simulated-time override is also editable here and is threaded into `/api/schedule` calls for Authenticated sessions; it has no effect for Guests, since they never compute a schedule. |
| Scheduling logic | 🟢 Integrated | The Command Center computes real schedules by sending Hive task/event data and the active list's effective settings to a separately-running `PriorityTaskManager.API` instance, not mock data (tracked by issue #47), via the authenticated, Subscription-gated `/api/schedule` route (issue #41; see [WORKFLOW.md](WORKFLOW.md)). Scheduling is online-exclusive: logged-in accounts see the computed Daily Column pipeline, while Guests (no account) see a plain, user-sortable task list (`GuestTaskList`: Importance/Due Date/Alphabetical) instead, since they have no access to online-only features (see [VISION.md](VISION.md)). The client no longer spawns the API process itself; it must already be running. |
| Event system | 🟢 Integrated | Guests use Hive-backed plain events and delete them permanently. Authenticated deletion archives a plain event or the selected scope of a recurring series; archived events can be restored or permanently deleted. Authenticated sessions can create recurring series, view upcoming occurrences, and edit or archive one occurrence, this and following, or the whole series. Edited occurrences can move to another date without losing their original series identity. The inspector also offers a replacement recurrence pattern for future or whole-series edits. |
| Task dependencies | 🟢 Integrated | Dependency management is supported in the inline task inspector form. |
| Networked API | 🟢 Integrated | Real login/register screens, secure JWT storage (`flutter_secure_storage`), and Guest-vs-Authenticated session state are implemented (issue #44 core), with a guest-first entry flow (one-time "Continue as Guest" vs. "Log in / Create account" choice, never a forced login). Guests can later opt into logging in from a Left Rail account control. Logging in from Guest only switches session state; it does not yet migrate local Hive task/list data to the account (tracked by issue #46, targeted for V1). Authenticated sessions now use an API-backed `TaskRepository` (calling `/api/tasks`/`/api/lists`/`/api/events`/`/api/profile`) instead of local Hive storage; Guests still use the local Hive store. Server-side events are not list-scoped (unlike local `FixedEvent`), so `/api/events` returns/accepts all of an account's events regardless of list — a known limitation. The dev-only, define-gated auto-login shim (`PTM_DEV_AUTOLOGIN`) has been removed (issue #41 cleanup); testing the authenticated route now goes through real login with a seeded dev account. |

## Confirmed Capabilities

- Gold Panning is the currently active scheduling strategy.
- Lists can carry their own settings snapshot instead of always inheriting only global defaults.
- `PriorityTaskManager.API` exposes authenticated REST endpoints (`/api/tasks`, `/api/lists`, `/api/events`, `/api/profile`) for CRUD, and an authenticated, Subscription-tier-gated `/api/schedule` endpoint that computes a real Gold Panning (or constraint-optimization) schedule from posted task data for callers with a `Subscription`-tier JWT claim, without persisting anything server-side. Scheduling has no unauthenticated route; every request requires Postgres. Task/event delete endpoints archive items for authenticated users. The authenticated `/api/archive` surface lists and restores tasks/events, permanently deletes individual archived items, and clears the archive; task restore defaults to the original list and requires an explicit `targetListId` (returning `409 Conflict`) if that list no longer exists.
- `PriorityTaskManager.Flutter/` is a web + Windows desktop client with its own task/list/event/settings CRUD, supporting add/edit/complete/delete, dependency management, per-list scheduling overrides, and global defaults. Its single-screen "Three-Pane Command Center" (Left Rail, Center Stage, Right Inspector) computes real schedules for logged-in accounts — including fixed events and the active list's effective settings — by calling a separately-running local `PriorityTaskManager.API` instance's authenticated `/api/schedule` route rather than using mock data (tracked by issue #47); Guests see a plain, user-sortable task list instead, since scheduling is online-exclusive. The client does not start or manage this API process itself (see [WORKFLOW.md](WORKFLOW.md) for how to run it during development). Real login/register screens and Guest-vs-Authenticated session state exist (issue #44 core); authenticated sessions now persist tasks/lists/events/profile through an API-backed repository instead of local Hive storage, and guest-to-account data migration is tracked separately by issue #46.
- The Flutter Command Center layout: a persistent Left Rail (list switcher, Archive/Settings nav, Engine Status clock/mode indicator), a horizontally scrolling Center Stage (daily columns — Today, Tomorrow, ..., Unscheduled — with scheduled task cards and fixed event cards), and a Right Inspector with an inline CRUD form for the selected task, event, list (including its scheduling-settings overrides), or the global defaults. At narrow/medium window widths the Left Rail and/or Right Inspector collapse into drawer overlays; each docked pane can be resized or dragged into a drawer, with a button to restore it. Selecting an item while the inspector isn't docked auto-opens the drawer. Multi-line inputs (e.g. task descriptions) have a drag handle to resize their height.
- Task and event forms reject empty or whitespace-only names with an inline field warning and the standard warning notification instead of creating an unnamed item.
- The Left Rail's Archive nav item opens a dialog listing archived tasks and events with per-item Restore and permanent-delete actions plus a confirmed Clear archive action. Archive is Authenticated-only: Guests get an in-app message instead of the dialog, and `LocalTaskRepository`'s archive methods fail fast (`UnsupportedError`) since the UI never calls them for a Guest session. If a task's original list no longer exists, restoring prompts the user to pick a target list before retrying.
- In debug builds, a "Dev Log" entry in the Left Rail opens a resizable, dockable bottom panel (similar to an IDE's terminal/debug console) listing recent API calls and Guest domain-event mutations (timestamp, success/failure, duration), for diagnosing integration issues without attaching a debugger (issue #59). It is not present in release builds.

## Known Limitations

- There is no undo/redo system.
- Recurring tasks are not supported.
- The constraint-optimization scheduling mode is not implemented yet.
- The event pipeline shows occurrences in the current 14-day window; it does not provide a historical series browser. Event timestamps use local wall-clock dates without an explicit event time-zone identifier.

## Known Issues and Technical Debt

- The scheduling system still needs future refinement around slack handling, intra-day focus heuristics, and backlog fairness.
- The event workflow is functional but still under UX refinement.
- The test suite overhaul is in progress; see the repository's GitHub Issues for current scope and remaining work.
## Validation Notes

- Build check: `dotnet build .\Priority-Task-Manager.sln` succeeds; the archived CLI is excluded.
- Test check: `dotnet test .\PriorityTaskManager.Tests\PriorityTaskManager.Tests.csproj` excludes archived CLI tests; the current run reports 176 passed, 1 skipped, and 1 unrelated failure in `PersistenceServiceTests.ArchiveTasks_AppendsArchivedTasksToArchiveFile`.
- Build check: `flutter build web` and `flutter build windows` succeed in `PriorityTaskManager.Flutter/`; `flutter analyze` and `flutter test` pass.
- Live check: register, login, and an authenticated schedule computation have been verified end to end against the hosted production API, including a Flutter Windows client build pointed at the hosted instance.
- Use the repository's GitHub Issues for the current active-work sequence, blockers, and next steps.
