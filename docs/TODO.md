# Project TODO List

> **Note:** This file is the backlog and roadmap for upcoming work. It should only contain planned work, not current-state reporting. Tasks are listed in priority order. 

## Testing Overhaul

- Overhaul test coverage according to `docs/TESTING_STRATEGY.md`:
  - Rebuild deterministic tests for core service behavior.
  - Rebuild CLI handler tests against the current command surface.
  - Add scheduling invariants and characterization tests.

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

## (A) 2/2 Constraint Solver Implementation Path

Prerequisite:

- Complete Gold Panning refinements.

Implementation targets:

- Implement the reduced V1 pipeline defined in `docs/CONSTRAINT_SOLVER.md`:
  - PolicyCoordinator + Feasibility
  - WindowBuilder
  - Dependency + Decomposition
  - Scoring
  - OptimizationPlanner
  - Explanation
- Enforce no-overlap ownership boundaries between stages.
- Add deterministic replay and invariants coverage for the new path.

## Event System and Scheduling UX

- Improve event command UX and schedule-view integration.
- Keep past events retained but hidden from the default schedule view.
- Add an event history command such as `event all` for full event visibility.

## User-Controlled Scheduling Enhancements

- Support dynamic per-day work hours and recalculate average daily capacity.
- Allow users to provide current energy level as a scheduling input.
- Add a defer or put-off workflow for task postponement.
- Add load warnings when daily complexity exceeds configured thresholds.

## Parking Lot

- Add optional scheduling attributes:
  - Earliest start date.
  - Preferred start time.
