## TODOs

> **Note:** Tasks are listed in priority order. Tackle them sequentially from top to bottom unless otherwise specified. Tasks should also be removed when they are completed.

1.  **Phase 1: File Organization & Strategy Implementation**
    -   [x] Reorganize `PriorityTaskManager.Tests` folders (LegacyMCP, V1Optimization, Infrastructure, Integration).
    -   [x] Update test namespaces to match new folder structure.
    -   [x] Create `PriorityTaskManager/Scheduling` directory structure (Core, Optimization).
    -   [x] Add `SchedulingMode` to `UserProfile` (`GoldPanning`, `ConstraintOptimization`).
    -   [x] Rename `MultiAgentUrgencyStrategy` to `McpGoldPanningStrategy`.
    -   [ ] Implement `ConstraintOptimizationStrategy` skeleton.
    -   [x] Update `TaskManagerService` to instantiate `GoldPanning` or `ConstraintOptimization` strategy based on user settings.

2.  **Phase 2: Remove Legacy Scheduling Paths (Branch Strategy)**
    -   [ ] Remove old scheduling paths and legacy scheduling tests that are being replaced.
    -   [ ] Remove feature flags and compatibility branches tied to old scheduling behavior.
    -   [ ] Keep one short migration note in docs describing branch fallback strategy.

3.  **Phase 3: Refine Scheduling Algorithm (Elastic Constraints)**
    -   Follow TDD for each feature slice (Red-Green-Refactor).
    -   Write failing tests before implementing behavior changes.
    -   Implement locked V1 reduced pipeline from SCHEDULING_SYSTEM_SPEC.md:
        - PolicyCoordinator + Feasibility
        - WindowBuilder
        - Dependency + Decomposition
        - Scoring
        - OptimizationPlanner (with internal in-day sequencing and cross-day balancing)
        - Explanation
    -   Implement slack-aware urgency scoring to avoid high-importance last-minute placement.
    -   Implement front-loading intra-day sequencing (Eat the Frog) as an OptimizationPlanner sub-pass.
    -   Implement relative-density handling for backlog tasks.
    -   Enforce no-overlap ownership boundaries between stages.

3.  **Phase 4: Build Migration Test Matrix (New Pipeline)**
    -   [ ] Enforce merge gate: no behavior PR merges without test-first coverage.
    -   [ ] Add tests for FS dependency correctness in the new pipeline.
    -   [ ] Add tests for must-schedule overload behavior (late + overtime).
    -   [ ] Add tests for non-must dropping and exclusion policy.
    -   [ ] Add tests for slack protection on high-importance tasks.
    -   [ ] Add deterministic replay tests for identical inputs.

4.  **Phase 5: Terminology Cleanup (Post-Stabilization)**
    -   [ ] Rename legacy MCP scheduling terminology to pipeline-oriented names.
    -   [ ] Update docs and CLI help text to match the finalized architecture vocabulary.
    -   [ ] Ensure naming-only changes do not alter runtime behavior.

5.  **Improve Event Scheduling & Command Consistency**
    -   Implement support for repeating events (e.g., daily, weekly).
    -   Make the event creation process more user-friendly.
    -   Standardize command naming conventions across the application (e.g., ensure `delete` is used consistently for all entities instead of a mix of `delete`, `remove`, etc.).

6.  **Improve Settings Interface**
    -   Make the `settings` command more interactive and user-friendly for viewing and changing user profile settings.

7.  **Improve Overall CLI User Experience**
    -   Review all commands to simplify their usage, improve prompts, and provide clearer, more helpful error messages.

---

## Future Improvements

-  **Implement CLI Handler Test Suite**
    -   [ ] `AddHandlerTests.cs`
    -   [ ] `ListHandlerTests.cs`
    -   [ ] `EditHandlerTests.cs`
    -   [ ] `DeleteHandlerTests.cs`
    -   [ ] `etc...`

-  **Implement Infrastructure Test Suite**
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