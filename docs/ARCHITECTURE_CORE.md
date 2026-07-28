# Core Business Logic Architecture

This document defines the architecture for core services and business rules in `PriorityTaskManager/`.

## Responsibilities

The core library owns:

- Task, list, event, profile, and dependency operations.
- Business validation and domain invariants.
- Persistence coordination through `IPersistenceService`.
- Scheduling strategy selection and delegation through `IUrgencyStrategy`.
- Time-sensitive logic through `ITimeService` seams.

The core library must not depend on CLI handlers, console helpers, rendering, or user-prompt behavior.

## Key Services

| Service | Responsibility |
| --- | --- |
| `TaskManagerService` | Coordinates task/list/profile/event operations, active-list behavior, default application, and scheduling delegation |
| `TaskMetricsService` | Computes schedule-related metrics used by presentation and status indicators |
| `TimeService` / `ITimeService` | Provides current or simulated time for deterministic behavior |
| `PersistenceService` / `IPersistenceService` | Reads and writes persisted state |

## TaskManagerService Boundary

`TaskManagerService` is the core coordination layer. Add business operations here when they need to mutate or query persisted domain state across tasks, lists, events, or profiles.

Prefer existing service methods before adding duplicates. Important reusable responsibilities include:

- Active list access through `GetActiveListId()` and related list methods.
- Display-ID lookup through task lookup methods rather than duplicating display-ID resolution in handlers.
- Effective scheduling profile resolution through `BuildEffectiveUserProfile(...)`.
- List time preference application through `ApplyListTimePreference(...)`.
- Dependency validation and cycle prevention through existing dependency helpers.

## Avoiding Unbounded Service Growth

`TaskManagerService` is intentionally the default coordination point, which makes it a natural place for unrelated responsibilities to accumulate over time. Use these criteria before adding to it:

- Add a method to `TaskManagerService` when the operation coordinates across two or more of tasks, lists, events, or profiles, or when it must share the same persistence/transaction boundary as existing operations.
- Prefer a dedicated service (following the existing `TaskMetricsService` pattern) when new behavior is self-contained, has its own cohesive data/computation (for example, metrics, reporting, or a future notification or import service), and does not need to mutate core domain state directly.
- Do not grow `TaskManagerService` by adding formatting, console-facing, or presentation-shaping logic; that belongs in the CLI layer per `docs/ARCHITECTURE_CLI.md`.
- A new unrelated domain concern (not tasks, lists, events, profiles, or their direct coordination) is a signal to introduce a new service rather than extend `TaskManagerService`.

For current backlog status on extracting existing responsibilities, see the repository's GitHub Issues.

## Business Rule Placement

- Put rules that must hold across all interfaces in core services or models.
- Put CLI-only usage guidance, prompt behavior, and output formatting in CLI handlers or CLI utilities.
- Put algorithmic placement, ranking, and scheduling decisions under `PriorityTaskManager/Scheduling/**`.
- Put data shape and serialization behavior in models and persistence services.

## Dependency Relationships

Core services may depend on:

- Models in `PriorityTaskManager/Models`.
- Scheduling abstractions such as `IUrgencyStrategy`.
- Persistence abstractions such as `IPersistenceService`.
- Time abstractions such as `ITimeService`.

Core services must not depend on:

- `PriorityTaskManager.CLI`.
- `Console` or console helpers.
- CLI command result types.

## Invariants

- Core operations should leave persisted IDs, display IDs, active-list state, and list-scoped settings consistent.
- Core validation should throw specific exceptions or return explicit success/failure values that the CLI can translate into user feedback.
- Time-sensitive behavior should use `ITimeService` rather than `DateTime.Now` when deterministic behavior matters.
- Scheduling mode selection should route through `TaskManagerService` and `IUrgencyStrategy`, not through CLI conditionals.
- New unrelated domain concerns should be added as a new focused service rather than expanding `TaskManagerService` indefinitely; see "Avoiding Unbounded Service Growth" above.