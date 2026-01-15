## TODOs

> **Note:** Tasks are listed in priority order. Tackle them sequentially from top to bottom unless otherwise specified. Tasks should also be removed when they are completed.

1.  **Refine Scheduling Algorithm (Elastic Constraints)**
    -   Implement the "Gold Panning" Algorithm (RFC_ELASTIC_SCHEDULING.md).
    -   Rethink the `ComplexityBalancerAgent` and `PrioritizationAgent` to work cooperatively (Pour and Sift).
    -   Implement "Front-Loading" intra-day sequencing (Eat the Frog).
    -   Implement "Relative Density" weighting for Backlog tasks.

2.  **Implement Core Logic Test Suite**
    -   [ ] `SchedulingAgentTests.cs` (Update needed for new Algo)
    -   [x] `ComplexityBalancerAgentTests.cs` (Will need major refactor)
    -   [ ] `MultiAgentUrgencyStrategyTests.cs` (Integration)

3.  **Fix Dependency Scheduling Logic**

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
    -   [ ] Warn user when calculated Daily Load exceeds a 'MaxDailyComplexityLoad' threshold (e.g. 50), rather than refusing to schedule.

-  **Anti-Starvation Logic for Backlog Tasks**
    -   Address the issue where tasks with no due date are perpetually pushed to the end of the schedule by urgent tasks.
    -   *Option A (Maintenance Quota)*: Reserve a set percentage of daily capacity (e.g., 20%) specifically for non-urgent tasks.
    -   *Option B (Virtual Aging)*: Implement an "Effective Due Date" or "Age Score" that increases over time, eventually treating old backlog items as urgent.
    -   *Option C (Opportunistic Fill)*: Fill low-intensity days (e.g., <50% load) with backlog tasks to smooth out the complexity curve.