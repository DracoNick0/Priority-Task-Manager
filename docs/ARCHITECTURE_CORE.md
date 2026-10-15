# Core Business Logic Architecture

This document describes the service boundaries and business-rule placement in `PriorityTaskManager/`.

## Responsibilities

The core library owns task, list, event, profile, account, and dependency operations; business validation; persistence coordination; and scheduling delegation. Time-dependent behavior uses the `ITimeService` abstraction where deterministic time is required.

Core must not depend on client interaction, presentation, rendering, console helpers, or client-specific result types.

## Key Services

| Service or abstraction | Responsibility |
| --- | --- |
| `TaskManagerService` | Coordinates task, list, and profile operations; resolves effective list settings for scheduling; and delegates event operations to `EventService`. |
| `EventService` / `IEventService` | Owns event operations, including occurrence expansion, scoped recurring-event changes, and archive/restore behavior. |
| `TaskMetricsService` / `ITaskMetricsService` | Computes the target workday and task slack metrics, both work-hours-adjusted and actual. |
| `AccountService` / `IAccountRepository` | Registers accounts and verifies credentials through a separate account-storage abstraction. Account identity is distinct from task/list/event persistence. |
| `RecurrenceExpansionService` / `IRecurrenceExpansionService` | Expands recurrence rules into occurrence dates for a requested range; used by task and event operations and scheduling. |
| `RecurrenceSplitHelper` | Shares "this and following" series-split semantics between recurring tasks and events. |
| `DependencyGraphHelper` | Traverses task dependency relationships and detects cycles during task updates. |
| `TimeService` / `ITimeService` | Supplies current or simulated time to time-dependent core behavior. |
| `IPersistenceService` | Defines loading, saving, and archive operations independently of the host’s storage implementation. |
| `ServiceComposer` | Builds the shared core service graph around a host-supplied `IPersistenceService`, including the runtime time service, task manager, and task metrics service. |

For persisted data shapes and storage ownership, see [ARCHITECTURE_DATA.md](ARCHITECTURE_DATA.md). For scheduling strategy and algorithm contracts, see [ARCHITECTURE_SCHEDULING.md](ARCHITECTURE_SCHEDULING.md).

## Task Coordination Boundary

`TaskManagerService` coordinates operations involving tasks, lists, and profiles, and provides the core scheduling entry point. Scheduling uses the effective profile for the requested list and selects the strategy in core; clients should not duplicate strategy-selection conditionals. Event-specific operations are owned by `EventService` and exposed through the task manager where needed by existing callers.

Add an operation to `TaskManagerService` when it coordinates two or more core areas or must share their persistence boundary. Prefer a focused service for self-contained behavior with cohesive data or computation that does not need to mutate core domain state directly, as with `EventService` and `TaskMetricsService`. Treat an unrelated domain concern as a signal to introduce a separate service. Do not add presentation logic to the task manager.

## Business Rule Placement

- Put rules that must hold across clients in core services or models.
- Keep client interaction, prompt behavior, and output formatting in the relevant client.
- Keep algorithmic placement, ranking, and scheduling decisions under `PriorityTaskManager/Scheduling/**`.
- Keep data shape and serialization behavior in models and persistence services.

## Dependencies And Invariants

Core services may depend on domain models and core abstractions such as `IUrgencyStrategy`, `IPersistenceService`, and `ITimeService`. They must not depend on archived `PriorityTaskManager.CLI` code, `Console`, or client-specific types.

- Core operations preserve domain and reference consistency and honor list-scoped settings.
- Group restoration validates every task destination before changing archive or active data.
- Validation reports failures explicitly, using specific exceptions or success/failure results for the caller to translate into user feedback.
- Time-dependent behavior uses `ITimeService` where deterministic behavior matters.
- Scheduling mode selection routes through core services and `IUrgencyStrategy`, not client-side conditionals.
