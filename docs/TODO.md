## TODOs

> **Note:** Tasks are listed in priority order. Tackle them sequentially from top to bottom unless otherwise specified. Tasks should also be removed when they are completed.

*   **Phase 3: Critical UX & Settings Prep (Enable Dual-Mode)**
    - *Note: Follow TDD. Write `Handler` tests before modifying CLI logic.*
    - [ ] **Improve Settings Interface**: Refactor `SettingsHandler` to be interactive. Expose `SchedulingMode` so users can toggle between Gold Panning and Constraint Optimization.
    - [ ] **Improve Task Creation UI**: Streamline `AddHandler` prompts. Ensure inputs (Importance, Duration) align with V1 data needs.
    - [ ] **Standardize Commands**: Ensure consistent naming (e.g., `delete` vs `remove`) now to avoid breaking tests later.

*   **Phase 4: Implement Constraint Optimization (New Strategy)**
    -   Follow TDD for each feature slice (Red-Green-Refactor).
    -   Write failing tests before implementing behavior changes.
    -   Implement locked V1 reduced pipeline from SCHEDULING_SYSTEM_SPEC.md:
        - PolicyCoordinator + Feasibility
        - WindowBuilder
        - Dependency + Decomposition
        - Scoring
        - OptimizationPlanner (Constraint-Based)
        - Explanation
    -   Implement slack-aware urgency scoring to avoid high-importance last-minute placement.
    -   Implement front-loading intra-day sequencing (Eat the Frog) as an OptimizationPlanner sub-pass.
    -   Implement relative-density handling for backlog tasks.
    -   Enforce no-overlap ownership boundaries between stages.

*   **Phase 5: Build Migration Test Matrix (New Pipeline)**
    -   [ ] Enforce merge gate: no behavior PR merges without test-first coverage.
    -   [ ] Add tests for FS dependency correctness in the new pipeline.
    -   [ ] Add tests for must-schedule overload behavior (late + overtime).
    -   [ ] Add tests for non-must dropping and exclusion policy.
    -   [ ] Add tests for slack protection on high-importance tasks.
    -   [ ] Add deterministic replay tests for identical inputs.

*   **Phase 6: Cleanup & Refinement**
    -   [ ] Rename old MCP scheduling terminology to strategy-specific names (e.g., `GoldPanningAgent` vs `ConstraintAgent`).
    -   [ ] Update docs and CLI help text to clarify the differences between strategies.
    -   [ ] Rename `Scheduling/Optimization` to `Scheduling/Strategies`.

*   **Improve Event Scheduling (Post-V1)**
    -   Implement support for repeating events (e.g., daily, weekly).
    -   Make the event creation process more user-friendly.

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