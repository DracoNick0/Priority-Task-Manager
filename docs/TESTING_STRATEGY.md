# Testing Strategy

This document outlines the strategy for testing the Priority Task Manager application. The goal is to ensure the application is robust, reliable, and that all components function as expected.

## Guiding Principles: The Hybrid Testing Strategy

Because this application blends deterministic CRUD operations with complex, evolving optimization algorithms, we use a hybrid testing strategy:

-   **Strict TDD for Deterministic Logic**: Core services (`TaskManagerService`, `PersistenceService`), data models, and CLI handlers have highly predictable inputs and outputs.
-   **Exploratory Spiking for Algorithms**: Development of scheduling algorithms (`ConstraintOptimizationStrategy`, `GoldPanningStrategy`) is done via exploratory programming first. We rely on sample datasets and human verification to tune the heuristics before locking them down.
-   **Property-Based & Invariant Testing**: Instead of writing brittle assertions for precise algorithm outputs (e.g., "Task A must be at 9:00 AM"), we write invariant tests that check if the algorithm violated core rules (e.g., "No task is scheduled before its dependency", "Must-schedule tasks are never dropped").
-   **Use the `TimeService`**: All time-sensitive logic must use a mocked `ITimeService` to guarantee deterministic boundaries.

## Areas to Test

### 1. Core Services

-   **`TaskManagerService`**:
    -   Verify all CRUD operations for tasks, lists, and events.
    -   Test adding/removing dependencies.
    -   Test `GetPrioritizedTasks` and ensure it correctly invokes the urgency strategy.
-   **`PersistenceService`**:
    -   Mock file system interactions to test `LoadData` and `SaveData` logic in isolation.
-   **`TimeService`**:
    -   Test `SetSimulatedTime` and `ClearSimulatedTime`.
    -   Verify `GetCurrentTime` returns the correct time in both real and simulated modes.

### 2. Scheduling Algorithms (Constraint Solver & Gold Panning)

This is the most complex area of the application. We avoid brittle unit tests that assert exact schedules, as the objective functions are constantly being tuned.

-   **Invariant Rules (Property-Based Testing)**: 
    -   *Dependency Chain*: A dependent task is never scheduled before its prerequisites.
    -   *Time Bounds*: Total scheduled time in a day never exceeds the user's defined `ScheduleWindow` limits (unless authorized by specific Overtime flags).
    -   *Task Dropping*: `MustSchedule` tasks are prioritized and never placed in the unscheduled bucket unless totally unfeasible.
-   **Snapshot / Characterization Tests**:
    -   For stable algorithm outputs, we save the generated schedule against a complex benchmark dataset. Future refactors test against these text-based snapshots to catch unintended regressions in scheduling shape.
-   **Stage Pipeline Context**:
    -   Isolated tests for distinct deterministic stages (e.g., verifying `TaskNormalizationStage` correctly calculates `EffectiveImportance` modifiers).

### 3. CLI Handlers

-   For each non-interactive handler (`delete`, `complete`, etc.) on the result-based path:
    -   Unit test `ExecuteWithResult(...)` as pure command orchestration logic.
    -   Verify `CommandResult.Status`, `CommandResult.Message`, and `CommandResult.ShouldRefreshDashboard`.
    -   Verify service-side mutations separately from console rendering.
    -   Add focused tests for shared helper behavior (`NonInteractiveCommandResultHelper`) to cover mixed valid/invalid/not-found ID parsing and usage-message construction once and reuse across handlers.
-   For legacy handlers not yet migrated:
    -   Continue command-surface tests through `Execute(...)` to protect behavior during migration.
-   For interactive handlers with an input/output seam (for example, `HelpHandler`):
    -   Drive key input through the seam abstraction instead of real console buffers.
    -   Assert navigation behavior, exit behavior, and emitted help content without relying on `Console.Clear` or cursor APIs.

### 4. CLI Orchestration and Rendering Policy

-   Add focused tests for `Program.cs` orchestration behavior:
    -   Result-based handlers trigger dashboard refresh only when `ShouldRefreshDashboard` is true.
    -   Result messages are printed by `Program.cs` for result-based handlers.
    -   Legacy handlers continue to execute unchanged.
-   Keep dashboard rendering tests separate from handler business assertions so console buffer requirements do not block core command tests.

### 5. Migration Completion and Consolidation Tests

-   When handler migration is complete, add/adjust tests that assert the final single-contract dispatch path in `Program.cs`.
-   Remove tests that exist only to preserve temporary dual-contract compatibility behavior.
-   Add regression tests ensuring each command still returns clear user feedback categories after compatibility layer removal.

## Test Overhaul Plan

1. Reliable Core**: Enforce TDD on the `TaskManagerService` and `PersistenceService` to guarantee safe data manipulation.
2.  **Define Pipeline Invariants**: Write the rule-based property tests for scheduling (e.g., dependency ordering, timeframe limits).
3.  **Create Benchmark Datasets**: Assemble complex `tasks.json` baseline files representing varying levels of user loads (light day, heavy dependencies, over-allocated).
4.  **Implement Snapshot Testing**: Generate baseline schedule expectations for the benchmark datasets using both the V1 Solver and Gold Panning.
5.  **Refactor CLI Handlers**: Incrementally migrate non-interactive handlers to `CommandResult` and add unit coverage around `ExecuteWithResult(...)` before tackling deep interactive flows.
6.  **Consolidate CLI Command Contract**: Remove transitional dual-contract dispatch and compatibility methods after migration completion, then re-baseline command orchestration tests against the final single-contract model.