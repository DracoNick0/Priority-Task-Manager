# Data And Persistence Architecture

This document defines persisted data ownership, model boundaries, JSON storage, and list-scoped settings behavior.

## Responsibilities

Data and persistence architecture covers:

- Domain models in `PriorityTaskManager/Models`.
- The aggregate persisted state in `DataContainer`.
- JSON-backed loading and saving through `PersistenceService`.
- ID counters and active-list state.
- User profile defaults and list-scoped setting snapshots.

## Persisted State

`DataContainer` is the in-memory aggregate loaded from and saved to JSON. It contains tasks, lists, events, user profile, the `NextDisplayId` counter, and the active list ID.

Each task, list, and event has a globally unique `Guid` `Id` assigned at creation time (`Guid.NewGuid()`), which is the sole identity used for internal references (`TaskItem.ListId`, `TaskItem.Dependencies`, `DataContainer.ActiveListId`). `TaskItem.DisplayId` remains a separate, per-list sequential `int` shown to users in the CLI (e.g. `edit 3`, `depend 3 1`); it is not a unique identifier and is reassigned during re-indexing. `NextDisplayId` is the only remaining ID counter in `DataContainer`.

`PersistenceService` stores state in separate JSON files under the runtime data directory:

| File | Data |
| --- | --- |
| `tasks.json` | Tasks plus the `NextDisplayId` counter |
| `lists.json` | Task lists |
| `events.json` | Events |
| `user_profile.json` | Global user profile defaults |

The CLI's JSON files hold a single local user's data and have no account concept. `PriorityTaskManager.API`'s Postgres-backed `PostgresPersistenceService` (see [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md)) scopes every document row by `account_id` instead.

## Model Boundaries

| Model | Architectural Role |
| --- | --- |
| `Account` | MVP email + password account (id, normalized email, hashed password); the tenant boundary for API-hosted persisted data |
| `TaskItem` | Unit of work with scheduling metadata, completion state, and dependencies |
| `TaskList` | Named task container with copied list-specific scheduling and display settings |
| `UserProfile` | Global defaults and scheduling preferences |
| `Event` | Blocked time interval used by scheduling |
| `ScheduleWindow` / `TimeSlot` | Available work time after applying work hours and events |
| `ScheduledChunk` | Scheduled portion of a task |
| `PrioritizationResult` | Scheduler output: tasks, unscheduled tasks, and history |

## List-Scoped Settings

Lists carry copied settings so each active list can diverge from global defaults.

The intended behavior is:

1. New lists copy missing defaults from `UserProfile` through `TaskList.ApplyDefaultsFrom(...)` and service setup.
2. Later global default changes do not retroactively rewrite existing list settings.
3. `TaskManagerService.BuildEffectiveUserProfile(...)` resolves list settings into the effective profile for scheduling and dashboard logic.
4. `TaskManagerService.ApplyListTimePreference(...)` applies list-specific simulated time when switching lists.

When adding settings, update the model, copy/default behavior, effective profile resolution, persistence expectations, CLI editing flow, and focused tests together.

## Persistence Principles

- Use `IPersistenceService` for persistence boundaries rather than reading or writing JSON from handlers or scheduling stages.
- Keep serialization shape changes intentional and covered by tests when existing data compatibility matters.
- Preserve the `NextDisplayId` counter when adding or deleting items; do not infer new IDs in CLI code. Real `Id` values (tasks, lists, events) are always generated via `Guid.NewGuid()` at creation time in `TaskManagerService`/`EventService`, never assigned by CLI code.
- Keep persistence ignorant of console/UI behavior.
- `PersistenceService.LoadData()` fails soft per file: if a single JSON file (tasks/lists/events/user profile) is missing, empty, or unreadable, that file's data resets to its default and a descriptive message is recorded in `DataContainer.LoadWarnings` instead of being silently discarded. Other files still load normally.
- `PersistenceService.LoadData()` also transparently migrates JSON files still using the legacy `int`-based `Id` shape (from before the Guid identity migration) to the current `Guid` shape: lists are migrated first (building an old-int-Id → new-Guid map), then tasks (remapping `ListId` and `Dependencies` via that map plus a task-specific map), then events (self-contained, no cross-references). Each migrated file adds a `DataContainer.LoadWarnings` entry noting the migration. See `PersistenceService.IsLegacyIntIdShape`/`MigrateLegacyLists`/`MigrateLegacyTasks`/`MigrateLegacyEvents`.
- `DataContainer.LoadWarnings` is populated only by `LoadData()` and is not itself persisted to disk; the CLI (`Program.cs`) prints any warnings once at startup.
- `PersistenceService.SaveData()` writes each of the 4 files atomically (write to a `.tmp` file in the same directory, then `File.Replace`/`File.Move` into place) so a crash or interruption mid-save cannot leave a destination file partially written.

## Schema Evolution Guidance

There is currently no versioning field or migration pipeline for the JSON files `PersistenceService` reads and writes. When changing persisted shape:

- Prefer additive, optional changes (new nullable/defaulted properties) over renaming or removing existing properties, so older JSON files still deserialize.
- If a change is not additive (rename, type change, removed field, restructured container), add explicit load-time migration logic in `PersistenceService` rather than assuming a clean data directory, and cover it with a test that loads a fixture representing the pre-change shape.
- Treat a breaking, non-additive change to persisted shape as a documentation trigger: update this document and `docs/STATUS.md` describing the new shape and any migration behavior.
- There is no schema/version marker in the persisted JSON files today; non-additive changes rely on `PersistenceService` migration logic described above rather than a version check.

## Invariants

- `DataContainer` should always have at least one task list after service initialization.
- `ActiveListId` should refer to an existing list after default setup.
- `NextDisplayId` should remain a monotonic per-list-scoped counter for new task display IDs. Task, list, and event `Id` values are `Guid`s generated at creation time and require no counter.
- List-scoped settings should not unexpectedly mutate global defaults or unrelated lists.