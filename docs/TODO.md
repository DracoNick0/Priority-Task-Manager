# Project TODO List

> **Note:** This file is the backlog and roadmap for upcoming work. It should only contain planned work, not current-state reporting. Tasks are listed in priority order.

## (B) 1/5 Testing Overhaul and Scheduling Invariants

- Overhaul test coverage according to docs/TESTING_STRATEGY.md:
  - Rebuild deterministic tests for core service behavior.
  - Rebuild CLI handler tests against the current command surface.
  - Add scheduling invariants and characterization tests.
  - Add deterministic replay tests for identical inputs.

## (B) 2/5 CI Quality Gates

Prerequisite:

- Complete (B) 1/5.

Implementation targets:

- Add CI workflow to run build and tests on push and pull request.
- Fail CI on test failures.
- Add lightweight docs/link validation.
- Add coverage reporting and baseline threshold (initially modest, then raise over time).

## (B) 3/5 Constraint Solver MVP (Narrow Scope)

Prerequisite:

- Complete (B) 1/5 and (B) 2/5.

Implementation targets:

- Deliver a minimal, testable solver path behind existing scheduling mode selection.
- Keep Gold Panning as stable fallback.
- Add explicit explanation output for solver scheduling decisions.
- Keep scope intentionally small and defer full solver depth to (A) 2/2.

## (B) 4/5 Benchmark Scenarios and Strategy Comparison

Prerequisite:

- Complete (B) 3/5.

Implementation targets:

- Create fixed benchmark datasets (light, dependency-heavy, overloaded, event-heavy).
- Compare Gold Panning and Solver outputs on measurable metrics.
- Publish benchmark results in documentation for repeatable comparison over time.

## (B) 5/5 Release and Demo Polish

Prerequisite:

- Complete (B) 4/5.

Implementation targets:

- Produce reproducible CLI release artifacts.
- Add a short demo section (quick run path and sample scenario).
- Add concise engineering highlights and measurable outcomes for portfolio use.

## Platform and Interface Expansion

- Add a service/API layer to support additional front ends.
- Explore cross-platform clients (mobile and desktop) after API stabilization.
- Explore LLM-assisted control flows after API boundaries are defined.

## (A) 1/2 Scheduling Improvements (Gold Panning First)

- Implement slack-aware urgency to reduce high-importance last-minute placement.
- Improve focus-window sequencing so high-complexity tasks align with high-focus periods.
- Add anti-starvation behavior for backlog tasks with no due date.

Candidate anti-starvation approaches:

- Maintenance quota (reserve a percentage of daily capacity for backlog work).
- Virtual aging (increase urgency for older backlog tasks over time).
- Opportunistic fill (prefer backlog tasks on underloaded days).

## (A) 2/2 Constraint Solver Full Implementation Path (Post-MVP)

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

- Improve event command UX and schedule-view integration.
- Keep past events retained but hidden from the default schedule view.
- Add an event history command such as event all for full event visibility.

## User-Controlled Scheduling Enhancements

- Support dynamic per-day work hours and recalculate average daily capacity.
- Allow users to provide current energy level as a scheduling input.
- Add a defer or put-off workflow for task postponement.
- Add load warnings when daily complexity exceeds configured thresholds.

## Parking Lot

- Add optional scheduling attributes:
  - Earliest start date.
  - Preferred start time.