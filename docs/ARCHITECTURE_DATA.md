# Data And Persistence Architecture

This document defines persisted data ownership, model boundaries, storage formats, and list-scoped settings behavior.

## Responsibilities

Data and persistence architecture covers:

- Domain models in `PriorityTaskManager/Models`.
- The in-memory aggregate state in `DataContainer`.
- API account and account-scoped Postgres persistence.
- Guest-local Flutter Hive persistence.
- The retained JSON-backed `PersistenceService`, used by archived CLI code and tests rather than the active clients.
- Entity identities, the task display-ID counter, and client active-list state.
- User profile defaults and list-scoped setting snapshots.

## Data Ownership And Stores

`DataContainer` is the core in-memory aggregate for tasks, lists, events, user profile, the `NextDisplayId` counter, and `ActiveListId`. `IPersistenceService` has two implementations: the API uses `PostgresPersistenceService`, while the retained file-based `PersistenceService` is used by the archived CLI. Neither implementation saves `ActiveListId`; active-list selection is client state.

| Store | Ownership and shape |
| --- | --- |
| API / Postgres | `PostgresAccountRepository` stores accounts in `accounts`. `PostgresPersistenceService` stores account-scoped JSONB documents in `persisted_documents`: `tasks`, `lists`, `events`, `user_profile`, `archive`, and `archive_events`. The first four hold active task/list/event/profile data; archives are separate documents. See [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md) for the API boundary. |
| Flutter Guest / Hive | `LocalTaskRepository` stores task lists, tasks, profile, and events in the `task_lists`, `tasks`, `user_profile`, and `events` Hive boxes, creating a default list and profile when their boxes are empty. Guest data is local-only, not an API cache or synchronized account data. |
| Retained JSON / archived CLI | `PersistenceService` reads and writes JSON files under its configured data directory. This implementation is not the active product's persistence path. |

The retained JSON implementation uses these files under its configured data directory:

| File | Data |
| --- | --- |
| `taskitems.json` | Tasks plus the `NextDisplayId` counter |
| `lists.json` | Task lists |
| `events.json` | Events |
| `user_profile.json` | Global user profile defaults |
| `archive.json` | Archived tasks |
| `archive_events.json` | Archived events and recurring-series segments |

The API account rows are separate from the per-account `persisted_documents` rows. The Postgres document payloads use JSON serialization of the same core models; unlike the retained JSON-file loader, the Postgres loader does not run the legacy integer-ID migration.

Archived tasks and events may carry an `ArchiveGroupId` to group related entries, such as a task cleanup batch or recurring-event series segment. Restored active entities clear this archive-only ID. API archive documents are account-scoped in Postgres; Guests cannot archive, so Guest deletion is permanent.

Guest events have a list association in Hive. The Core `Event` model and API event documents are account-scoped, not list-scoped; event list ownership therefore does not round-trip through authenticated storage. Guest data is separate from account data, so any future sync must define reconciliation rather than treat the local store as a disposable cache.

Core assigns each task, list, and event a `Guid` `Id` when created; the Guest Hive repository generates UUIDs for Guest entities. IDs support references such as `TaskItem.ListId` and `TaskItem.Dependencies`; Flutter represents them as strings. `TaskItem.DisplayId` is a separate integer, allocated by Core from the shared `DataContainer.NextDisplayId` counter and exposed in the API response. It is not an entity identity and clients must not use it for references or generate it.

## Model Boundaries

| Model | Architectural Role |
| --- | --- |
| `Account` | MVP email + password account (id, normalized email, hashed password, `SubscriptionTier`); the tenant boundary for API-hosted persisted data |
| `TaskItem` | Unit of work with scheduling metadata, completion state, dependencies, and an optional associated link; recurring series also carry a `RecurrenceRule`, progression preferences, persisted per-date occurrence state, cancellation exceptions, and editable occurrence overrides |
| `TaskList` | Named task container with copied list-specific scheduling and display settings |
| `UserProfile` | Global defaults and scheduling preferences |
| `Event` | Blocked time interval used by scheduling |
| `RecurrenceRule` | Shared recurrence pattern/end-condition foundation for recurring tasks and recurring events, modeled as a polymorphic hierarchy (see below); embedded on both `Event` and recurring `TaskItem` series |
| `TaskOccurrenceState` | Persisted completion count, status, and completion timestamp for one recurring task's scheduled date; future occurrence placeholders are expanded on demand and are not persisted as task rows |
| `RecurrenceException` | A single cancelled occurrence of a `RecurrenceRule` series, keyed by original occurrence date |
| `TaskOccurrenceOverride` | Editable snapshot for one recurring task occurrence, keyed by original occurrence date so moved due dates retain series identity |
| `EventOccurrenceOverride` | A single edited (not cancelled) occurrence of a recurring `Event`, keyed by original occurrence date, carrying its own `Name`/`Description`/`Link`/`StartTime`/`EndTime`; distinct from `RecurrenceException`, which is cancellation-only |
| `ScheduleWindow` / `TimeSlot` | Available work time after applying work hours and events |
| `ScheduledChunk` | Scheduled portion of a task |
| `PrioritizationResult` | Scheduler output: tasks, unscheduled tasks, and history |

### RecurrenceRule Polymorphic Shape

`RecurrenceRule` and `RecurrenceEndCondition` are abstract polymorphic hierarchies serialized by `System.Text.Json` with string type discriminators. Derived types represent recurrence patterns and end conditions, allowing each persisted rule to contain only its relevant fields. Occurrence dates are expanded on demand for a bounded range; projected occurrences are not persisted as task or event rows.

Recurring task series persist per-date progress and status, cancellation exceptions, and editable overrides, plus progression settings and optional streak data. Recurring events persist cancellation exceptions and editable occurrence overrides. Mutations address the series and the original recurrence date, even when an override moves the displayed task date or event time. A "this and following" edit splits a series; task occurrence state, exceptions, and overrides are partitioned between the old and new series. Projected task IDs are transport identifiers, not persisted identities.

## List-Scoped Settings

Lists carry copied settings so each active list can diverge from global defaults.

The intended behavior is:

1. New lists copy missing defaults from `UserProfile`; explicit list values remain independent.
2. Later global default changes do not retroactively rewrite existing list settings.
3. Core resolves list settings over global defaults for scheduling. List-specific simulated time is applied when the active list changes.

When adding settings, update the model, copy/default behavior, effective profile resolution, persistence expectations, active client editing flows, and focused tests together.

## Persistence Principles

- Use `IPersistenceService` for core persistence boundaries rather than reading or writing storage directly from handlers or scheduling stages. The API's active implementation is `PostgresPersistenceService`; do not add behavior to the archived JSON path unless restoring the CLI is explicitly requested.
- Keep serialization shape changes intentional and covered by tests when existing data compatibility matters.
- Preserve `NextDisplayId` when adding or deleting tasks; do not infer display IDs in client code. Core generates IDs for authenticated entities, while the Guest Hive repository generates local UUIDs.
- Keep persistence ignorant of console/UI behavior.
- The retained JSON loader uses defaults for missing core-data files. An unreadable core-data file resets that file's data and adds a transient `DataContainer.LoadWarnings` entry; other files still load. The archived CLI prints these warnings at startup.
- The retained JSON loader migrates legacy integer task, list, and event IDs to `Guid`s, remapping task list and dependency references. Postgres JSONB loading does not run this migration.
- The retained JSON saver writes its four core-data files atomically via a temporary file. This guarantee does not describe Postgres writes.

## Schema Evolution Guidance

Neither the retained JSON files nor Postgres JSONB documents carry an explicit schema version. The JSON loader has a specific migration for legacy integer IDs; Postgres and Hive have no general migration pipeline. When changing persisted shape:

- Prefer additive, optional changes (new nullable/defaulted properties) over renaming or removing existing properties, so previously stored JSON can still deserialize.
- For a non-additive change, define and implement a migration/compatibility path for every affected store (Postgres JSONB, Hive, and retained JSON files where applicable) and cover it with tests using pre-change data.
- Treat a breaking persisted-shape change as a documentation trigger: update this document and `docs/STATUS.md` with the new shape and migration behavior.

## Invariants

- `DataContainer` should always have at least one task list after service initialization.
- `ActiveListId` should refer to an existing list after default setup.
- `NextDisplayId` should remain a monotonically increasing shared counter for new task display IDs. Task, list, and event `Id` values are `Guid`s generated at creation time and require no counter.
- List-scoped settings should not unexpectedly mutate global defaults or unrelated lists.
- Authenticated event persistence is account-scoped and not list-scoped; changes to this boundary must be coordinated with client behavior and documented explicitly.