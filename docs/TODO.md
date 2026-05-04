# Project TODO List

> **Note:** Tasks are listed in priority order. Tackle them sequentially from top to bottom unless otherwise specified. Tasks should also be removed when they are completed.

---

### Core Fixes & Refinements
- [ ] **Rethink the purpose and functionality of "Lists".**
    - *Description*: The concept of task lists is currently underutilized. Explore ways to make them more meaningful, such as associating different settings, views, or even scheduling strategies per list.
    - Consider adding more sorting options to the `list` command as well.
        - *Description*: Enhance `list sort` to include strategies like `alphabetical`, `due date`, or `ID`.

### Scheduling Related & Event Enhancements
- [ ] **Improve Event Scheduling System.**
    - *Description*: The `event` command UI needs a visual and usability overhaul. Past events should be retained but hidden from the main schedule view.
    - *New Command*: Implement an `event all` (or similar) command to display a comprehensive list of both past and future events.
- [ ] **Implement mock schedules**
    - *Description*: Provide selectable mock scenarios that temporarily replace the current list of assignments and available time slots so algorithms can be tested against pre-defined scenarios without modifying the user's persisted data.
    - *Notes*: Mock runs must be isolated (in-memory or separate temp storage), configurable via CLI (e.g., `mock run <scenario>`), and reversible.

## UI/UX Improvements
- [ ] **Create a persistent, top-aligned schedule view.**
    - *Description*: Implement a "dashboard" mode where the schedule view stays locked to the top of the terminal. When commands are run, their output should appear in a scrolling region below the schedule, preventing the schedule from being pushed off-screen. The view should also auto-refresh periodically (e.g., every 15 minutes).

### User-Driven Scheduling Enhancements
- [ ] **Dynamic/Custom Work Hours**: Allow different work hours per day.
    - Calculate `AvgDailyWorkCapacity` from user profile for dynamic slack awareness.
- [ ] Allow user to set their current energy level to influence scheduling.
- [ ] Implement a 'put off' feature to defer a task.
- [ ] Warn user when Daily Load exceeds a `MaxDailyComplexityLoad` threshold.

### Gold Panning Strategy Refinements (Pre-Phase 4)
- [ ] **Implement slack-aware urgency** to avoid high-importance last-minute placement.
- [ ] **Utilize User Focus Windows Intelligently.**
    - *Description*: Instead of simply front-loading all complex tasks ("Eat the Frog"), revise the intra-day sequencing. The goal is to place high-complexity tasks during the user's defined high-focus windows, while ensuring that tasks due today (or finishing on their due date) are always prioritized to prevent last-mianute placement and deadline risk.
- [ ] **Anti-Starvation Logic for Backlog Tasks**
    - Address the issue where tasks with no due date are perpetually pushed to the end of the schedule by urgent tasks.
    - *Option A (Maintenance Quota)*: Reserve a set percentage of daily capacity (e.g., 20%) specifically for non-urgent tasks.
    - *Option B (Virtual Aging)*: Implement an "Effective Due Date" or "Age Score" that increases over time, eventually treating old backlog items as urgent.
    - *Option C (Opportunistic Fill)*: Fill low-intensity days (e.g., <50% load) with backlog tasks to smooth out the complexity curve.

### Phase 4: Implement Constraint Solver (New Strategy)
-   Complete the Gold Panning refinements above before starting this phase.
-   Follow Hybrid Testing (Exploratory Spiking + Invariants) for the algorithm logic.
-   Write snapshot/characterization tests to lock down stable scheduling shapes.
-   Implement locked V1 reduced pipeline from `SCHEDULING_SYSTEM_SPEC.md`:
    - PolicyCoordinator + Feasibility
    - WindowBuilder
    - Dependency + Decomposition
    - Scoring
    - OptimizationPlanner (Constraint-Based)
    - Explanation
-   Enforce no-overlap ownership boundaries between stages.

### Phase 5: Build Migration Test Matrix (New Pipeline)
-   [ ] Enforce merge gate: no behavior PR merges without test-first coverage.
-   [ ] Add tests for FS dependency correctness in the new pipeline.
-   [ ] Add tests for must-schedule overload behavior (late + overtime).
-   [ ] Add tests for non-must dropping and exclusion policy.
-   [ ] Add tests for slack protection on high-importance tasks.
-   [ ] Add deterministic replay tests for identical inputs.

### Future UI/UX Expansions
-  **Implement CLI Handler Test Suite**
    -   [ ] `AddHandlerTests.cs`
    -   [ ] `ListHandlerTests.cs`
    -   [ ] `EditHandlerTests.cs`, `DeleteHandlerTests.cs`, etc.

### Future API & Service Expansions
- [ ] **Add a mobile interface (Android/iOS).**
    - *Description*: Develop a cross-platform mobile application (e.g., using React Native) which will require creating a web API to serve data from the core C# application.
- [ ] **Integrate an LLM for conversational control.**
    - *Description*: Allow users to manage tasks via natural language. This would involve creating an API that an LLM can call to translate commands into application actions.
-  **Implement Infrastructure Test Suite**
    -   [ ] `PersistenceServiceTests.cs` (Integration)

-  **Additional Attributes for Scheduling**
    -   [ ] **Earliest Start Date**: Support tasks that cannot begin until a future date.
    -   [ ] **Start Time preference**: Support tasks that must start at a specific time.
