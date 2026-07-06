# Project TODO List

> **Note:** Tasks are listed in priority order. Tackle them sequentially from top to bottom unless otherwise specified. Tasks should also be removed when they are completed.

---

### Core Fixes & Refinements
- **Extend per-list configurations.**
    - *Description*: Build upon the list scheduling overrides by allowing different scheduling windows (e.g., work hours) per list and additional per-list settings such as name, description, and sort option to further separate contexts.
    - *Notes*: Prefer an interactive settings menu navigable with arrow keys, while still allowing direct commands for faster editing when needed.
- **Combine list settings commands.**
    - *Description*: Combine the separate `list sort` commands into a unified `list settings` experience so list configuration is managed from one interactive place instead of scattered commands.
    - *Notes*: Keep direct command forms available as an optional shortcut, not the only way to access settings.

- **Overhaul the testing strategy.**
    - *Description*: Rebuild the testing approach across the solution by following `docs/TESTING_STRATEGY.md`, including strict TDD for deterministic core and CLI logic, invariant/property-based coverage for scheduling behavior, and snapshot/characterization tests where schedule shape needs to stay stable.
    - *Notes*: Treat this as a broad test-suite migration rather than a narrow bug fix; align the existing `PriorityTaskManager.Tests` project with the current architecture and replace outdated coverage incrementally.

- **Revise the documentation and rewrite the README.**
    - *Description*: Perform a complete update of the project documentation so `docs/ARCHITECTURE.md`, `docs/STATUS.md`, `docs/TODO.md`, `docs/WORKFLOW.md`, and related docs reflect the current codebase, then replace the root `README.md` with a fresh, accurate overview of the project.
    - *Notes*: Treat this as a full documentation pass, not a light edit; remove stale references, align terminology with the current architecture, and make the README suitable as the first entry point for new contributors.

- **Implement mock schedules**
    - *Description*: Provide selectable mock scenarios that temporarily replace the current assignments and available time slots so algorithms can be tested against pre-defined scenarios without modifying the user's persisted data.
    - *Notes*: Mock runs must be isolated (in-memory or separate temp storage), configurable via CLI, reversible, and easy to discover from the terminal.
    - *Plan*:
        - Add a `mock` CLI command family with `mock list`, `mock run <scenario>`, and an interactive scenario picker.
        - Define scenario files in `PriorityTaskManager.CLI/MockScenarios/` with sample tasks, events, work-hour overrides, and optional simulated time.
        - Build an in-memory sandbox around the active data container so mock runs never touch persisted JSON data.
        - Reuse the normal schedule output renderer, but label mock results clearly as simulations.
        - Add tests for scenario loading, command parsing, and persistence isolation.

### Scheduling Related & Event Enhancements
- **Improve Event Scheduling System.**
    - *Description*: The `event` command UI needs a visual and usability overhaul. Past events should be retained but hidden from the main schedule view.
    - *New Command*: Implement an `event all` (or similar) command to display a comprehensive list of both past and future events.

### User-Driven Scheduling Enhancements
- **Dynamic/Custom Work Hours**: Allow different work hours per day.
    - Calculate `AvgDailyWorkCapacity` from user profile for dynamic slack awareness.
- Allow user to set their current energy level to influence scheduling.
- Implement a 'put off' feature to defer a task.
- Warn user when Daily Load exceeds a `MaxDailyComplexityLoad` threshold.

### Minor CLI Interactive Menu Fix
- **Interactive menu re-draw fix:**
    - For interactive select menus, prefer `Console.SetCursorPosition()`-based redraws (see `ListHandler`'s `DrawListMenu` method) so we only refresh the affected menu area instead of clearing and reprinting the entire console (see `HelpHandler`'s `RunInteractiveHelp` method).

### 1/3: Gold Panning Strategy Refinements (Pre-Constraint-Solver)
- **Implement slack-aware urgency** to avoid high-importance last-minute placement.
- **Utilize User Focus Windows Intelligently.**
    - *Description*: Instead of simply front-loading all complex tasks ("Eat the Frog"), revise the intra-day sequencing. The goal is to place high-complexity tasks during the user's defined high-focus windows, while ensuring that tasks due today (or finishing on their due date) are always prioritized to prevent last-mianute placement and deadline risk.
- **Anti-Starvation Logic for Backlog Tasks**
    - Address the issue where tasks with no due date are perpetually pushed to the end of the schedule by urgent tasks.
    - *Option A (Maintenance Quota)*: Reserve a set percentage of daily capacity (e.g., 20%) specifically for non-urgent tasks.
    - *Option B (Virtual Aging)*: Implement an "Effective Due Date" or "Age Score" that increases over time, eventually treating old backlog items as urgent.
    - *Option C (Opportunistic Fill)*: Fill low-intensity days (e.g., <50% load) with backlog tasks to smooth out the complexity curve.

### 2/3: Implement Constraint Solver (New Strategy)
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

### 3/3: Build Migration Test Matrix (New Pipeline)
-   Enforce merge gate: no behavior PR merges without test-first coverage.
-   Add tests for FS dependency correctness in the new pipeline.
-   Add tests for must-schedule overload behavior (late + overtime).
-   Add tests for non-must dropping and exclusion policy.
-   Add tests for slack protection on high-importance tasks.
-   Add deterministic replay tests for identical inputs.

### Future API & Service Expansions
- **Add a mobile and Windows interface (Android/iOS).**
    - *Description*: Develop a cross-platform application (e.g., using React Native) which will require creating a web API to serve data from the core C# application.
- **Integrate an LLM for conversational control.**
    - *Description*: Allow users to manage tasks via natural language. This would involve creating an API that an LLM can call to translate commands into application actions.

-  **Additional Attributes for Scheduling**
    -   **Earliest Start Date**: Support tasks that cannot begin until a future date.
    -   **Start Time preference**: Support tasks that must start at a specific time.
