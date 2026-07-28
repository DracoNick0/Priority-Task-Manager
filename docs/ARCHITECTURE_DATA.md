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

`DataContainer` is the in-memory aggregate loaded from and saved to JSON. It contains tasks, lists, events, user profile, ID counters, and the active list ID.

`PersistenceService` stores state in separate JSON files under the runtime data directory:

| File | Data |
| --- | --- |
| `tasks.json` | Tasks plus task ID counters |
| `lists.json` | Task lists plus list ID counter |
| `events.json` | Events plus event ID counter |
| `user_profile.json` | Global user profile defaults |

## Model Boundaries

| Model | Architectural Role |
| --- | --- |
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
- Preserve ID counters when adding or deleting items; do not infer new IDs in CLI code.
- Keep persistence ignorant of console/UI behavior.
- `PersistenceService.LoadData()` fails soft per file: if a single JSON file (tasks/lists/events/user profile) is missing, empty, or unreadable, that file's data resets to its default and a descriptive message is recorded in `DataContainer.LoadWarnings` instead of being silently discarded. Other files still load normally.
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
- `NextTaskId`, `NextDisplayId`, `NextListId`, and `NextEventId` should remain monotonic counters for new records.
- List-scoped settings should not unexpectedly mutate global defaults or unrelated lists.