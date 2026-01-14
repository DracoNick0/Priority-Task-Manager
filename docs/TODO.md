## TODOs

> **Note:** Tasks are listed in priority order. Tackle them sequentially from top to bottom unless otherwise specified.

1.  **Implement ComplexityBalancerAgent Logic**
    -   Implement the core logic for ComplexityBalancerAgent to distribute task complexity density across available days, respecting all constraints and pipeline separation.

2.  **Implement Core Logic Test Suite**
    -   [x] `TaskManagerServiceTests.cs`
    -   [x] `TaskAnalyzerAgentTests.cs`
    -   [x] `SchedulePreProcessorAgentTests.cs`
    -   [x] `PrioritizationAgentTests.cs`
    -   [ ] `ComplexityBalancerAgentTests.cs`
    -   [ ] `SchedulingAgentTests.cs`
    -   [ ] `MultiAgentUrgencyStrategyTests.cs` (Integration)

3.  **Fix Dependency Scheduling Logic**
    -   The scheduling engine currently does not correctly respect task dependencies. This needs to be fixed to ensure dependent tasks are always scheduled after their prerequisites.

4.  **Improve Event Scheduling & Command Consistency**
    -   Implement support for repeating events (e.g., daily, weekly).
    -   Make the event creation process more user-friendly.
    -   Standardize command naming conventions across the application (e.g., ensure `delete` is used consistently for all entities instead of a mix of `delete`, `remove`, etc.).

5.  **Improve Settings Interface**
    -   Make the `settings` command more interactive and user-friendly for viewing and changing user profile settings.

6.  **Improve Overall CLI User Experience**
    -   Review all commands to simplify their usage, improve prompts, and provide clearer, more helpful error messages.

---

## Future Improvements

-  **Implement CLI Handler Test Suite**
    -   [ ] `AddHandlerTests.cs`
    -   [ ] `ListHandlerTests.cs`
    -   [ ] `EditHandlerTests.cs`
    -   [ ] `DeleteHandlerTests.cs`
    -   [ ] `etc...`

--  **Implement Infrastructure Test Suite**
    -   [ ] `PersistenceServiceTests.cs` (Integration)

-  **User-Driven Scheduling Enhancements**
    -   [ ] Allow user to set their current energy level to influence scheduling of complex tasks.
    -   [ ] Implement a 'put off' feature so users can defer a task to a later day.