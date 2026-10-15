# Testing Strategy

This document defines testing expectations for the active .NET and Flutter code. Current validation results and known failures belong in [STATUS.md](STATUS.md); test-overhaul work is tracked in the repository's GitHub Issues.

## Guiding Principles

- **Deterministic behavior:** Use focused unit and integration tests for core services, API contracts, and client behavior.
- **Scheduling behavior:** Exploratory work can help tune heuristics, but supported scheduling behavior must be protected by deterministic scenarios and invariant assertions. The current .NET invariant suite uses fixed xUnit scenarios rather than generated property-based tests.
- **Deterministic time:** Inject `ITimeService` into time-sensitive behavior. Use `MockTimeService` and `DeterministicTestFixtures` when a test needs a fixed current time; `TimeService` tests also verify its real-clock mode.

## Active Test Surfaces

| Area | Location | Current coverage |
| --- | --- | --- |
| Core services | `PriorityTaskManager.Tests/Services/` and `PriorityTaskManager.Tests/Integration/` | Focused service tests and `TaskManagerService` coordination, including task/list operations, validation, dependencies, and scheduling selection |
| Scheduling | `PriorityTaskManager.Tests/Scheduling/` | Shared scheduling invariants, Gold Panning stage behavior, and recurring-task scheduling |
| API contracts | `PriorityTaskManager.Tests/API/` | DTO mappings and schedule request/response contracts; add boundary tests when endpoint, authentication, or persistence behavior changes |
| Flutter client | `PriorityTaskManager.Flutter/test/` | Widget behavior, date/time and recurrence utilities, repository behavior, and development HTTP logging |
| Archived CLI | `PriorityTaskManager.Tests/CLI/` | Preserved test sources, excluded from the active test project |

## Areas to Test

### Core Services and API

- **`TaskManagerService`:** Cover task and list operations, validation and error outcomes, dependency changes, effective list-profile resolution, and strategy selection. Coordination tests live in `PriorityTaskManager.Tests/Integration/TaskManagerServiceTests.cs`.
- **Focused services:** Keep unit coverage for account and event behavior, dependency graph operations, recurrence expansion and splitting, task metrics and occurrence progression, and time handling.
- **`PersistenceService`:** Its tests exercise the core JSON load/save and archive paths using isolated temporary directories. This is not the API's Postgres persistence implementation.
- **API boundary:** The current API tests focus on DTO and schedule-contract mapping. Add tests at the changed boundary for endpoint, authentication, tenant-scoping, or persistence behavior rather than treating DTO tests as endpoint coverage.

### Flutter Client

Keep tests alongside the Flutter package under `PriorityTaskManager.Flutter/test/`. Cover changed widgets and user interactions, date/time defaults and constraints, recurrence behavior, repository and HTTP-client behavior. Run `flutter analyze` and `flutter test` from the Flutter package directory.

### Scheduling Algorithms

`GoldPanningStrategy` is the active scheduling implementation. `ConstraintOptimizationStrategy` is currently a placeholder: its service-level test checks the graceful not-implemented result, but it is not covered as a functioning scheduling strategy.

The algorithm-agnostic checks in `PriorityTaskManager.Tests.Scheduling.SchedulingInvariantTestsBase` assert these shared invariants:

- **No overlap and availability:** Scheduled chunks do not overlap, stay within configured workdays and work hours, and avoid blocking events.
- **Duration and feasibility:** When the tested workload fits available capacity, task durations are preserved and tasks are not silently left unscheduled. Split chunks sum to the task's estimated duration.
- **Dependencies:** A dependent task starts only after its prerequisite's final scheduled chunk, including when the prerequisite is split.
- **Dates:** Chunks do not start before `NotBefore`; a `NotBefore` beyond the scheduling horizon is reported as unscheduled. The current due-date assertion checks that no scheduled chunk falls on a calendar date after the task's due date.
- **Completion and input state:** Completed tasks are not scheduled, and scheduling does not alter original task properties (scheduled parts are output).
- **Determinism:** Identical inputs and time produce the same schedule. This is checked by replaying a fixed scenario, not by comparing against a committed schedule snapshot.

Add general invariants once in `SchedulingInvariantTestsBase`. Each implemented strategy should have a thin subclass that supplies its strategy factory; `GoldPanningInvariantTests` is the active example. When constraint optimization is implemented, add its invariant-suite subclass and tests for its own solver behavior. Keep Gold Panning stage tests focused on the behavior of their individual stages instead of duplicating shared invariants.

## Archived CLI Tests

The CLI project and its test sources are retained for possible future restoration but are excluded from routine builds and tests. `PriorityTaskManager.Tests.csproj` explicitly excludes `PriorityTaskManager.Tests/CLI/`. Do not modify, build, or run these sources as part of routine work; restore the CLI only when explicitly requested.

## Test Verification Standards

- **Core and API changes:** Add unit or integration tests for deterministic behavior, success and error outcomes, and relevant domain invariants.
- **Flutter changes:** Add or update package tests for affected widget, utility, repository, or HTTP-client behavior, and run `flutter analyze`.
- **Scheduling changes:** Run the shared invariant suite against the active strategy. Preserve correct invariant tests when they expose an implementation defect; distinguish those defects from incorrect or outdated expectations before changing a test.
- **Running the suites:** Run `dotnet test` from the repository root for the active .NET solution. From `PriorityTaskManager.Flutter/`, run `flutter analyze` and `flutter test`. See [WORKFLOW.md](WORKFLOW.md) and the [Flutter README](../PriorityTaskManager.Flutter/README.md#verifying-changes) for setup details.
