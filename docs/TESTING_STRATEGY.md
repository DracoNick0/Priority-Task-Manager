# Testing Strategy

This document outlines the strategy for testing the Priority Task Manager application. The goal is to ensure the application is robust, reliable, and that all components function as expected.

## Guiding Principles

-   **Test Core Logic Thoroughly**: The primary focus of our testing efforts will be on the `PriorityTaskManager` core library, which contains all business logic.
-   **Leverage Dependency Injection**: Use mock objects for dependencies (like `IPersistenceService` and `ITimeService`) to isolate components under test.
-   **Test for Edge Cases**: Ensure tests cover not just the "happy path" but also edge cases, invalid inputs, and potential failure scenarios.
-   **Use the `TimeService`**: All tests for time-sensitive logic (scheduling, due dates, etc.) **must** use a mocked or controlled `ITimeService` to ensure deterministic and repeatable results.

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

### 2. The Agent Pipeline (MCP)

This is the most critical area to test. We need to test both the individual agents and their integration.

-   **`SchedulePreProcessorAgent`**:
    -   Given a `UserProfile` and a list of `Events`, verify it generates the correct `AvailableScheduleWindow`.
    -   Use a simulated `ITimeService` to test scenarios where the current time is inside, outside, or during a workday.
    -   Test with no events, one event, multiple events, and overlapping events.
-   **`TaskAnalyzerAgent`**:
    -   Verify it correctly calculates `EffectiveImportance`.
-   **`PrioritizationAgent`**:
    -   Verify it correctly sorts tasks by `DueDate` and then `Complexity`.
-   **`ComplexityBalancerAgent`**:
    -   Test that it moves high-complexity tasks earlier in a day.
    -   Test that it distributes complexity across multiple days while respecting due dates.
-   **`SchedulingAgent`**:
    -   Given a pre-ordered list of tasks and available slots, verify it generates the correct `ScheduledChunk`s.
    -   Test with tasks that are too large for any single slot and need to be split.
-   **`MultiAgentUrgencyStrategy` (Integration Test)**:
    -   Test the entire pipeline from end to end.
    -   Provide a set of tasks, a user profile, and events.
    -   Assert that the final `PrioritizationResult` contains a valid schedule and a logical history log from the agents.

### 3. CLI Handlers

-   For each handler (`AddHandler`, `ListHandler`, etc.):
    -   Mock the `TaskManagerService`.
    -   Simulate user input by passing different `args` to the `Execute` method.
    -   Verify that the correct `TaskManagerService` methods are called with the expected parameters.

## Test Overhaul Plan

1.  **Establish a Test Foundation**: Create a `MockPersistenceService` and a mockable `ITimeService` to be used across all tests.
2.  **Fix `TaskManagerService` Tests**: Start by fixing the tests for the main service to ensure basic data operations are reliable.
3.  **Test Individual Agents**: Write focused unit tests for each agent, mocking their dependencies.
4.  **Write Pipeline Integration Tests**: Create tests for `MultiAgentUrgencyStrategy` that verify the end-to-end scheduling process.
5.  **Review and Update Existing Tests**: Go through all old test files, deleting what is no longer relevant and updating what can be salvaged.
