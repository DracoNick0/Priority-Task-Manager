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

## Invariants

- `DataContainer` should always have at least one task list after service initialization.
- `ActiveListId` should refer to an existing list after default setup.
- `NextTaskId`, `NextDisplayId`, `NextListId`, and `NextEventId` should remain monotonic counters for new records.
- List-scoped settings should not unexpectedly mutate global defaults or unrelated lists.