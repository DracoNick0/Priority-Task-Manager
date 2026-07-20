# Project TODO List

> **Note:** This file is the source of truth for the backlog, roadmap, and currently in-progress work. Keep current-state feature reality in `docs/STATUS.md`; use this file for concise handoff details, blockers, dependencies, and next steps. Tasks are listed in priority order.

## (B) 1/5 Testing Overhaul and Scheduling Invariants

Status: In progress.

Completed:

- CLI handler migration to the `CommandResult`/thin-wrapper pattern is functionally complete for every handler currently wired in `Program.cs` (full `ExecuteWithResult` conversion or an explicit thin wrapper where a handler stays facade-owned). See `docs/STATUS.md` for the per-handler breakdown.
- CLI Migration Consolidation is complete: every wired handler implements only `ICommandResultHandler`, the legacy `ICommandHandler` contract and its compatibility bridges have been removed, `Program.cs` dispatches through a single `Dictionary<string, ICommandResultHandler>`, and handler tests call `ExecuteWithResult(...)` directly.

Remaining:

- Expand interactive console seam adoption to remaining interactive handlers.
- Audit and refactor existing Gold Panning tests (`PriorityTaskManager.Tests/Scheduling/GoldPanning`):
  - Revise brittle tests that assert exact, hard-coded schedules.
  - Convert them into the invariant tests defined in `docs/TESTING_STRATEGY.md` (Scheduling Algorithms), or into snapshot/characterization tests.
  - Ensure tests for individual pipeline stages include checks for stage-specific invariants (e.g., `DailySequencingStage` correctly orders tasks by priority).
  - Validate active Gold Panning dependency-order behavior specifically before broader scheduling characterization baselines are accepted.
- Implement the scheduling invariant tests from `docs/TESTING_STRATEGY.md` (Scheduling Algorithms): Dependency Chain, Time Bounds, Task Dropping, No Overlapping Tasks, Task Duration Adherence, Respect `NotBefore`/`DueDate`, Idempotency, State Immutability, Task Splitting Logic, Completed Task Exclusion, Event Blocking.

Notes:

- The interactive branches of `SettingsHandler`, plus `HelpHandler`, `EditHandler`, `ListHandler`, and `EventCommandHandler`, remain on facade-owned rendering; do not collapse their interactive/menu-driven console output into `CommandResult.Message` — `IInteractiveConsoleFacade` is reserved for genuinely interactive (menu/key-input) flows and should not be bypassed for them.

Blockers / Dependencies:

- The test suite is green; keep it green as remaining migration and scheduling work proceeds.
- Dependency-order invariant tests may expose real scheduler defects; keep correct failing tests as isolated red tests while fixing the implementation instead of weakening expected behavior.

Scheduler validation policy:

- Write invariant tests from documented scheduling rules, not from current accidental output.
- Separate failures into implementation defects, incorrect test expectations, and unclear requirements.
- Do not use characterization baselines to bless behavior that violates hard invariants.

Next steps:

1. **Audit and Refactor Gold Panning Tests**: Audit and refactor the tests in `PriorityTaskManager.Tests/Scheduling/GoldPanning` to align with the testing strategy, converting brittle tests into invariant checks.
2. **Implement Core Invariant Tests**: Add focused scheduler validation tests for dependency-order and the other invariants in `docs/TESTING_STRATEGY.md`, using minimal, deterministic task sets.
3. **Classify Failures**: As tests are added, classify any failures as implementation defects, incorrect/outdated expectations, or unclear requirements before broadening coverage.
4. **Broaden Scheduling Characterization Coverage**: Add broader scheduling characterization coverage only after the hard invariants from step 2 are protected.

## (B) 2/5 CI Quality Gates

Status: Not started.

Prerequisite:

- Complete (B) 1/5.

Implementation targets:

- Add CI workflow to run build and tests on push and pull request.
- Fail CI on test failures.
- Add lightweight docs/link validation.
- Add coverage reporting and baseline threshold (initially modest, then raise over time).

## (B) 3/5 Constraint Solver MVP (Narrow Scope)

Status: Blocked.

Prerequisite:

- Complete (B) 1/5 and (B) 2/5.

Implementation targets:

- Deliver a minimal, testable solver path behind existing scheduling mode selection.
- Keep Gold Panning as stable fallback.
- Add explicit explanation output for solver scheduling decisions.
- Keep scope intentionally small and defer full solver depth to (A) 2/2.

## (B) 4/5 Benchmark Scenarios and Strategy Comparison

Status: Blocked.

Prerequisite:

- Complete (B) 3/5.

Implementation targets:

- Create fixed benchmark datasets (light, dependency-heavy, overloaded, event-heavy).
- Compare Gold Panning and Solver outputs on measurable metrics.
- Publish benchmark results in documentation for repeatable comparison over time.

## (B) 5/5 Release and Demo Polish

Status: Blocked.

Prerequisite:

- Complete (B) 4/5.

Implementation targets:

- Produce reproducible CLI release artifacts.
- Add a short demo section (quick run path and sample scenario).
- Add concise engineering highlights and measurable outcomes for portfolio use.

## Platform and Interface Expansion

Status: Not started.

- Add a service/API layer to support additional front ends.
- Explore cross-platform clients (mobile and desktop) after API stabilization.
- Add LLM-assisted intake for external planning sources (documents, GitHub projects/repos, todo lists, Canvas content).
- Add extraction pipelines that normalize imported source content into candidate tasks, lists, and events.
- Add review-and-confirm UX so generated tasks/events are editable before persistence.
- Add provider abstraction and guardrails (rate limits, retries, validation, and source/decision traceability) for LLM-backed generation.

## (A) 1/2 Scheduling Improvements (Gold Panning First)

Status: Not started.

- Implement slack-aware urgency to reduce high-importance last-minute placement.
- Improve focus-window sequencing so high-complexity tasks align with high-focus periods.
- Add anti-starvation behavior for backlog tasks with no due date.

Candidate anti-starvation approaches:

- Maintenance quota (reserve a percentage of daily capacity for backlog work).
- Virtual aging (increase urgency for older backlog tasks over time).
- Opportunistic fill (prefer backlog tasks on underloaded days).

## (A) 2/2 Constraint Solver Full Implementation Path (Post-MVP)

Status: Blocked.

Prerequisite:

- Complete (A) 1/2.
- Complete (B) 3/5.

Implementation targets:

- Expand solver beyond MVP using the reduced V1 pipeline in docs/CONSTRAINT_SOLVER.md:
  - PolicyCoordinator + Feasibility
  - WindowBuilder
  - Dependency + Decomposition
  - Scoring
  - OptimizationPlanner
  - Explanation
- Enforce no-overlap ownership boundaries between stages.
- Add full invariants and characterization coverage for the expanded path.

## Event System and Scheduling UX

Status: In progress.

Completed:

- Event add, edit, list, delete, and clear command paths exist.
- Selected event interactive paths use the interactive console facade seam.

Remaining:

- Improve event command UX and schedule-view integration.
- Keep past events retained but hidden from the default schedule view.
- Add an event history command such as `event all` for full event visibility.

Blockers / Dependencies:

- Console-seam cleanup from `(B) 1/5` should happen first for testable event command changes.

Next steps:

1. Finish console-seam coverage for event command paths used by tests.
2. Define default schedule visibility for past events.
3. Add the event history command and focused command-surface tests.

## User-Controlled Scheduling Enhancements

Status: Not started.

- Support dynamic per-day work hours and recalculate average daily capacity.
- Allow users to provide current energy level as a scheduling input.
- Add a defer or put-off workflow for task postponement.
- Add load warnings when daily complexity exceeds configured thresholds.

## Parking Lot

Status: Not started.

- Add optional scheduling attributes:
  - Earliest start date.
  - Preferred start time.
