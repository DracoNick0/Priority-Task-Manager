# Project Status

**Framework**: .NET 10 (ASP.NET Core API) and Flutter (web + Windows)
**Storage**: Postgres (API/hosted), Hive (Flutter local data)

This document summarizes current observable capabilities, limitations, known issues, and verified validation results.

## Status Snapshot

- The CLI has been retired from the product and is no longer supported.
- `PriorityTaskManager.API` is deployed to a real, publicly reachable hosted instance backed by Postgres, providing account/JWT auth and an authenticated, Subscription-tier-gated schedule-computation endpoint. This gives the app a genuine networked path to its core differentiator (computed scheduling), not just a per-developer local process. See [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md) for hosting/deployment details.
- The Flutter client (web + Windows) has real login/register and a guest-first entry flow, and can be configured at build time to target either a local development API or the hosted production API.
- Gold Panning is the active scheduling strategy; constraint optimization is not yet available as a selectable mode.
- The latest local validation results are recorded under Validation Notes; the .NET test suite still has one known failure in the retained JSON archive-storage test.

## Feature Matrix

### Core (C# backend)

`PriorityTaskManager` is the shared business-logic library used by the API.

| Feature Area | Status | Notes |
| --- | --- | --- |
| Task management | 🟢 Working | Tasks support add, edit, view, complete, undo, archive, restore, permanent deletion, descriptions, and optional links. Authenticated recurring tasks track per-occurrence progress, missed/skipped/disregarded states, and optional streaks; occurrence edits and deletes can target one occurrence, this and following, or the whole series. |
| List management | 🟢 Working | Create, switch, delete, and settings flows work, and lists carry copied settings for scheduling and presentation. |
| Data persistence | 🟢 Working | Authenticated account data is stored through the API; Guest data stays on the device. The two stores are separate and are not synchronized. |
| Settings and defaults | 🟢 Working | Global defaults and per-list settings overrides are both supported. |
| Scheduling logic | 🟡 Partially implemented | Gold Panning is the only available scheduling strategy; constraint optimization is not implemented. |
| Event system | 🟢 Working | Fixed and recurring events support descriptions and optional links. Authenticated recurring events can be edited, moved, archived, and restored one occurrence, this and following occurrences, or the whole series. |
| Task dependencies | 🟢 Working | Dependency add/remove is supported, and the active Gold Panning pipeline enforces dependency-order placement (a dependent task is never scheduled before its prerequisite completes). |
| Unit tests | 🟡 One known failure | Deterministic core-service and Gold Panning invariant/replay coverage (including dependency-order scheduling) exist. The latest .NET run reports 222 passed, 1 skipped, and 1 failure in the retained JSON archive-storage test; see Validation Notes. |
| Networked API | 🟢 Working | Authenticated task, list, event, profile, and archive operations are available, including recurring-task occurrence progress and scoped edits. Subscription-tier users can request Gold Panning schedules; computed schedules are not saved. |

### Archived CLI

The CLI has been retired from the product and is excluded from routine builds and test suites. Its historical design is preserved for reference only in [ARCHITECTURE_CLI.md](ARCHITECTURE_CLI.md).

### Flutter client integration

Tracks how much of the Core feature set is exposed through `PriorityTaskManager.Flutter` (guest local store and authenticated web + Windows desktop client).

| Feature Area | Status | Notes |
| --- | --- | --- |
| Task management | 🟢 Integrated | One-off tasks support add, edit, complete, delete, descriptions, and optional links. Authenticated users can create recurring series, view occurrence progress and missed state, and edit or delete one occurrence, this and following occurrences, or the whole series. Guest deletion is permanent; authenticated deletion archives the task. |
| List management | 🟢 Integrated | List switching, create, delete, and a settings form (name/description plus per-list scheduling overrides) are all supported through the Left Rail and Right Inspector. |
| Data persistence | 🟢 Integrated | Guest data is stored locally; authenticated account data is stored through the API. |
| Settings and defaults | 🟢 Integrated | A global defaults form (Left Rail Settings) and per-list overrides (sort option, scheduling mode, work hours/days, urgency thresholds) are supported; unset list fields inherit the global defaults. A per-list simulated-time override is also editable here and is threaded into schedule calls for authenticated sessions; it has no effect for Guests. |
| Scheduling logic | 🟡 Partially integrated | The Command Center computes real schedules through the authenticated, Subscription-gated schedule route. Authenticated recurring task series and their per-date state, along with recurring event rules, are sent to the API and expanded within the scheduler's computed horizon; scheduled task chunks and fixed and recurring event occurrences appear in chronological order in each daily column, overdue pending occurrences are projected as missed, and missed recurring backlog is shown in Unscheduled. The oldest unresolved occurrence in Wait for Each Occurrence mode appears untimed at the end of the current workday while it remains open, or the next configured workday after it closes. Split tasks show their part number across the schedule. Guest users see a plain, user-sortable one-off task list and have no recurring-task support. |
| Event system | 🟢 Integrated | Guests use local plain events with descriptions and optional links, and delete them permanently. Authenticated deletion archives a plain event or the selected scope of a recurring series; occurrences from the same series are grouped and can be restored together. Authenticated sessions can create recurring series, view upcoming occurrences, and edit or archive one occurrence, this and following, or the whole series. Edited occurrences can move to another date without losing their original series identity. The inspector also offers a replacement recurrence pattern for future or whole-series edits. |
| Task dependencies | 🟢 Integrated | Dependency management is supported in the inline task inspector form. |
| Networked API | 🟢 Integrated | Real login/register screens, secure token storage, and Guest-vs-Authenticated session state are implemented with a guest-first entry flow (one-time "Continue as Guest" vs. "Log in / Create account" choice, never a forced login). Guests can opt into logging in from a Left Rail account control. Expired or server-rejected sessions return to Guest mode; temporary server/network failures do not clear the session. Logging in from Guest only switches session state; guest-to-account data migration is not yet implemented. Authenticated sessions persist tasks/lists/events/profile through the API; Guests use local device storage. |

## Confirmed Capabilities

- Gold Panning is the currently available scheduling strategy. Authenticated recurring tasks retain separate progress for each occurrence, and future occurrences are not scheduled before their recurrence dates. Zero-effort occurrences do not create time blocks.
- Authenticated users can create recurring task series, edit or delete one occurrence, this and following occurrences, or the whole series, and complete occurrences in configurable per-occurrence counts, retain or disregard missed occurrences, skip occurrences and undo a skip by uncompleting it, undo completion progress, and optionally track current/best streaks. Guest/local task storage does not support recurring tasks.
- Lists can carry their own settings snapshot instead of always inheriting only global defaults.
- Tasks and events support descriptions and optional links. New task due dates and event times default from the configured work hours.
- The authenticated API supports task/list/event/profile and archive operations, including scoped recurring-task actions. Subscription-tier users can request on-demand Gold Panning schedules; generated schedules are not saved. Authenticated task and event deletion archives items. Archived items can be restored, permanently deleted, or cleared; task restore uses the original list unless it no longer exists.
- `PriorityTaskManager.Flutter/` is a web + Windows desktop client with its own task/list/event/settings CRUD, supporting add/edit/complete/delete, dependency management, per-list scheduling overrides, and global defaults. Its single-screen "Three-Pane Command Center" (Left Rail, Center Stage, Right Inspector) computes real schedules for logged-in accounts — including fixed events and the active list's effective settings — by calling a separately-running API instance's authenticated `/api/schedule` route; Guests see a plain, user-sortable task list instead, since scheduling is online-exclusive. Real login/register screens and Guest-vs-Authenticated session state exist with an API-backed repository for authenticated sessions and local storage for guests.
- The Flutter Command Center layout: a persistent Left Rail (list switcher, Archive/Settings nav, Engine Status clock/mode indicator), a horizontally scrolling Center Stage (daily columns — Today, Tomorrow, ..., Unscheduled when tasks are present, and Completed), and a Right Inspector with an inline CRUD form for the selected task, event, list (including its scheduling-settings overrides), or the global defaults. Scheduled task chunks and events are interleaved by start time within each day; split-task part numbers make continuations visible across events and day columns. At narrow/medium window widths the Left Rail and/or Right Inspector collapse into drawer overlays; each docked pane can be resized or dragged into a drawer, with a button to restore it. Selecting an item while the inspector isn't docked auto-opens the drawer. Multi-line inputs (e.g. task descriptions) have a drag handle to resize their height.
- Task and event forms reject empty or whitespace-only names with an inline field warning and notification instead of creating an unnamed item.
- The Left Rail's Archive nav item opens a dialog listing individual archived items and grouped task/event items with per-item Restore and permanent-delete actions, a Restore all action for multi-item groups, and a confirmed Clear archive action. Archive is Authenticated-only: Guests get an in-app message instead of the dialog, and local storage archive operations are disabled. If a task group's original list no longer exists, restoring prompts the user to pick a target list before retrying.
- In debug builds, a "Dev Log" entry in the Left Rail opens a resizable, dockable bottom panel listing recent API calls and local domain-event mutations for diagnosing integration issues without attaching a debugger. It is not present in release builds.

## Known Limitations

- There is no undo/redo system. (Ctrl + Z / Ctrl + Y)
- Guest mode supports one-off tasks and plain events only; recurring tasks and events are available only to authenticated users.
- Guest data is not migrated into an account when a user logs in.
- The constraint-optimization scheduling mode is not implemented yet.
- The event pipeline shows occurrences in the current 14-day window; it does not provide a historical series browser. Event timestamps use local wall-clock dates without an explicit event time-zone identifier.
- Authenticated events are account-scoped rather than list-scoped.

## Known Issues and Technical Debt

- One retained JSON archive-storage test fails because it expects the archive file in the process working directory while configuring the store with a temporary data directory. The active API and Guest data stores do not use this file-backed persistence path.

## Validation Notes

- Build: `dotnet build .\Priority-Task-Manager.sln` succeeds with three nullable-reference warnings in scheduling tests.
- .NET tests: `dotnet test .\PriorityTaskManager.Tests\PriorityTaskManager.Tests.csproj --configuration Release` reports 222 passed, 1 skipped, and 1 failed in `PersistenceServiceTests.ArchiveTasks_AppendsArchivedTasksToArchiveFile`.
- Flutter: `flutter test --reporter compact` and `flutter analyze` pass when run from `PriorityTaskManager.Flutter/`.
- Hosted API reachability: an unauthenticated `GET https://tpm-api.fly.dev/api/lists` returned HTTP 401, confirming the endpoint is reachable and protected. An authenticated scheduling request was not re-tested for this update.
