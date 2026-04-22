# Testing Strategy

This document outlines the strategy for testing the Priority Task Manager application. The goal is to ensure the application is robust, reliable, and that all components function as expected.

## Guiding Principles: The Hybrid Testing Strategy

Because this application blends deterministic CRUD operations with complex, evolving optimization algorithms, we use a hybrid testing strategy:

-   **Strict TDD for Deterministic Logic**: Core services (`TaskManagerService`, `PersistenceService`), data models, and CLI handlers have highly predictable inputs and outputs. We use Test-Driven Development here to enforce rigid constraints and ensure a stable sandbox.
-   **Exploratory Spiking for Algorithms**: Development of scheduling algorithms (`ConstraintOptimizationStrategy`, `McpGoldPanningStrategy`) is done via exploratory programming first. We rely on sample datasets and human verification to tune the heuristics before locking them down.
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
-   **Agent Pipeline Context**:
    -   Isolated tests for distinct deterministic agents (e.g., verifying `TaskAnalyzerAgent` correctly calculates `EffectiveImportance` modifiers).

### 3. CLI Handlers

-   For each handler (`AddHandler`, `ListHandler`, etc.):
    -   Mock the `TaskManagerService`.
    -   Simulate user input by passing different `args` to the `Execute` method.
    -   Verify that the correct `TaskManagerService` methods are called with the expected parameters.

## Test Overhaul Plan

1. Reliable Core**: Enforce TDD on the `TaskManagerService` and `PersistenceService` to guarantee safe data manipulation.
2.  **Define Pipeline Invariants**: Write the rule-based property tests for scheduling (e.g., dependency ordering, timeframe limits).
3.  **Create Benchmark Datasets**: Assemble complex `tasks.json` baseline files representing varying levels of user loads (light day, heavy dependencies, over-allocated).
4.  **Implement Snapshot Testing**: Generate baseline schedule expectations for the benchmark datasets using both the V1 Solver and Legacy Gold Panning.
5.  **Refactor CLI Handlers**: Implement Behavior-Driven (BDD) style testing for the CLI sequence paths using mock dependencies