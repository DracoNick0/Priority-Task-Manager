# Project TODO List

> **Note:** This file is the source of truth for the backlog, roadmap, and currently in-progress work. Keep current-state feature reality in `docs/STATUS.md`; use this file for concise handoff details, blockers, dependencies, and next steps. Tasks are listed in priority order.

## Fix Unhandled Crash When Selecting Constraint Optimization Mode

Status: Not started.

Implementation targets:

- Guard `TaskManagerService.GetPrioritizedTasks` (or `ModeHandler`) so selecting `SchedulingMode.ConstraintOptimization` does not propagate an unhandled `NotImplementedException` through `ScheduleSnapshotProvider.RefreshActiveListSnapshot` into the CLI main loop and crash the app.
- Either return a graceful "not implemented" outcome (a `CommandResult`/history entry indicating the solver is unavailable) from `GetPrioritizedTasks`, or block `mode constraint` at the handler level with a clear warning until the solver ships.
- Add a regression test that selects constraint mode and asserts the CLI/dashboard refresh path does not throw.

## Core Service Boundary Review

Status: Not started.

Implementation targets:

- Review `TaskManagerService` responsibilities against the growth criteria in `docs/ARCHITECTURE_CORE.md` ("Avoiding Unbounded Service Growth") and identify any responsibilities that are already self-contained enough to extract into a dedicated service (following the `TaskMetricsService` pattern).
- Extract identified candidates into focused services with their own interfaces and tests, updating `docs/ARCHITECTURE_CORE.md` if new services are introduced.
- Re-check this item periodically as new commands or coordination logic are added rather than treating it as a one-time cleanup.
- Add an event history command such as `event all` for full event visibility.

Blockers / Dependencies:

- Console-seam cleanup should happen first for testable event command changes.

Next steps:

1. Finish console-seam coverage for event command paths used by tests.
2. Define default schedule visibility for past events.
3. Add the event history command and focused command-surface tests.

## Harden Persistence Layer Error Handling and Write Atomicity

Status: Not started.

Implementation targets:

- Replace the empty `catch { }` blocks in `PersistenceService.LoadData()` with logged/surfaced errors so corrupt or partially-written JSON does not silently reset user data with no warning.
- Make `PersistenceService.SaveData()` write-atomic per file (e.g. temp file + rename) across tasks/lists/events/profile so a mid-save crash cannot leave the four files inconsistent with each other.
- Add tests covering corrupt-file load behavior and interrupted-save recovery.

## Repository and Solution Hygiene Cleanup

Status: Not started.

Implementation targets:

- Remove or populate `PriorityTaskManager.API/` (currently only `bin/`/`obj/` with zero source files) so the solution does not carry empty scaffolding.
- Consolidate the two solution files (`Priority-Task-Manager.sln` and `PriorityTaskManager.Tests/PriorityTaskManager.Tests.sln`) into a single canonical solution, or document why both must exist.

## Reassess Dual CLI Rendering Ownership Model

Status: Not started.

Implementation targets:

- Review whether the two coexisting rendering ownership models (result-based `ICommandResultHandler` returns vs. handlers that own rendering directly through `IInteractiveConsoleFacade`) should be collapsed to one model as more commands adopt the facade seam.
- If the split is kept, document explicit decision criteria in `docs/ARCHITECTURE_CLI.md` for choosing one model per new command, so contributors have a checklist rather than tribal knowledge.
- Note: `docs/ARCHITECTURE_CLI.md` currently treats this split as a deliberate design decision, not migration debt; this item is about periodically re-validating that stance as the command surface grows, not necessarily reversing it.

## (A) 1/4 CI Quality Gates

Status: Not started.

Prerequisite:

- Complete (B) 1/5.

Implementation targets:

- Add CI workflow to run build and tests on push and pull request.
- Fail CI on test failures.
- Add lightweight docs/link validation.
- Add coverage reporting and baseline threshold (initially modest, then raise over time).
- Add an automated architecture-boundary check (for example, a dependency-direction test such as NetArchTest, or a simple project-reference assertion) that fails CI if `PriorityTaskManager` ever references `PriorityTaskManager.CLI` or console types, so the core/CLI boundary in `docs/ARCHITECTURE_CORE.md` is enforced mechanically rather than by convention alone.

## (A) 2/4 React Frontend for Web and Desktop

Status: Not started.

Prerequisite:

- Complete (B) 2/5.

Implementation targets:

- Add a service/API layer to support a React-based frontend and other future clients.
- Implement a React frontend targeting both web and desktop (e.g., via Electron or Tauri) against that API.
- Explore additional cross-platform clients (e.g., mobile) after the web/desktop frontend and API stabilize.

## (A) 3/4 LLM-Assisted Intake for External Planning Sources

Status: Not started.

Prerequisite:

- Complete (B) 4/5.

Implementation targets:

- Add LLM-assisted intake for external planning sources (documents, GitHub projects/repos, todo lists, Canvas content).
- Add extraction pipelines that normalize imported source content into candidate tasks, lists, and events.
- Add review-and-confirm UX so generated tasks/events are editable before persistence.
- Add provider abstraction and guardrails (rate limits, retries, validation, and source/decision traceability) for LLM-backed generation.

## (A) 4/4 Release and Demo Polish

Status: Not started.

Prerequisite:

- Complete (B) 3/5.

Implementation targets:

- Produce reproducible CLI (and, once available, frontend) release artifacts.
- Add a short demo section (quick run path and sample scenario).
- Add concise engineering highlights and measurable outcomes for portfolio use.

## (B) 1/4 Scheduling Improvements (Gold Panning First)

Status: Not started.

- Implement slack-aware urgency to reduce high-importance last-minute placement.
- Improve focus-window sequencing so high-complexity tasks align with high-focus periods.
- Add anti-starvation behavior for backlog tasks with no due date.

Candidate anti-starvation approaches:

- Maintenance quota (reserve a percentage of daily capacity for backlog work).
- Virtual aging (increase urgency for older backlog tasks over time).
- Opportunistic fill (prefer backlog tasks on underloaded days).

Note: This item covers heuristic/quality improvements to Gold Panning and is independent of the `(B)` chain. It is distinct from `(B) 1/5`, which only fixes correctness gaps required to satisfy the hard scheduling invariants (dependency ordering, `NotBefore`).

## (B) 2/4 Constraint Solver MVP (Narrow Scope)

Status: Blocked.

Prerequisite:

- Complete (A) 1/4.
- Complete (B) 2/5 (CI Quality Gates).

Implementation targets:

- Deliver a minimal, testable solver path behind existing scheduling mode selection.
- Keep Gold Panning as stable fallback.
- Add explicit explanation output for solver scheduling decisions.
- Keep scope intentionally small and defer full solver depth to (A) 4/4.

## (B) 3/4 Benchmark Scenarios and Strategy Comparison

Status: Blocked.

Prerequisite:

- Complete (A) 2/4.

Implementation targets:

- Create fixed benchmark datasets (light, dependency-heavy, overloaded, event-heavy).
- Compare Gold Panning and Solver outputs on measurable metrics.
- Publish benchmark results in documentation for repeatable comparison over time.

## (B) 4/4 Constraint Solver Full Implementation Path (Post-MVP)

Status: Blocked.

Prerequisite:

- Complete (A) 1/4.
- Complete (A) 2/4.

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
