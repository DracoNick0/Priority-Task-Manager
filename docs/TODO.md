# Project TODO List

> **Note:** This file is the source of truth for the backlog, roadmap, and currently in-progress work. Keep current-state feature reality in `docs/STATUS.md`; use this file for concise handoff details, blockers, dependencies, and next steps. Tasks are listed in priority order.

## (B) 1/5 Testing Overhaul and Scheduling Invariants

Status: In progress.

Completed:

- CLI handler migration to the `CommandResult`/thin-wrapper pattern is functionally complete for every handler currently wired in `Program.cs` (full `ExecuteWithResult` conversion or an explicit thin wrapper where a handler stays facade-owned). See `docs/STATUS.md` for the per-handler breakdown.
- CLI Migration Consolidation is complete: every wired handler implements only `ICommandResultHandler`, the legacy `ICommandHandler` contract and its compatibility bridges have been removed, `Program.cs` dispatches through a single `Dictionary<string, ICommandResultHandler>`, and handler tests call `ExecuteWithResult(...)` directly.

Completed (continued):

- Audited existing Gold Panning tests (`PriorityTaskManager.Tests/Scheduling/GoldPanning`): `AvailabilityWindowStageTests`, `TaskNormalizationStageTests`, `TaskDistributionStageTests`, `TaskRankingStageTests`, and `DailySequencingStageTests` all test live, wired stages and only assert stage-owned mechanics — no brittle hard-coded full-schedule assertions or general-invariant duplication found; no changes needed. `GoldPanningInvariantTests.cs` is already invariant-based and is the migration target for step 2 below.
- Identified and marked `WorkloadBalancingStage`/`WorkloadBalancingStageTests.cs` as legacy and not wired into the active `GoldPanningStrategy` pipeline (superseded by `TaskDistributionStage` + `DailySequencingStage`); both now carry explanatory code comments and are excluded from stage-audit and invariant-suite scope.
- Identified two further unwired legacy stages with the same treatment: `DependencyAwarePlacementStage` (dependency-aware placement with bump/reflow) and `TaskOrderingStage` (superseded by `TaskRankingStage`) — both now carry explanatory code comments. Critically, this confirms the active Gold Panning pipeline currently has **no dependency-ordering enforcement at all**; the future "Dependency Chain" invariant test is expected to fail against `GoldPanningStrategy` until this gap is fixed (see Blockers / Dependencies below).
- Built `SchedulingInvariantTestsBase` (`PriorityTaskManager.Tests.Scheduling`), an algorithm-agnostic invariant suite written against `IUrgencyStrategy`/`PrioritizationResult`. Migrated the existing invariant coverage (no overlap, time bounds, event blocking, duration adherence, task dropping, idempotency) out of `GoldPanningInvariantTests` and into the base; `GoldPanningInvariantTests` is now a thin subclass supplying only the `GoldPanningStrategy` factory. Documented the convention in `docs/TESTING_STRATEGY.md` for future `IUrgencyStrategy` implementations.

Remaining:

- Expand interactive console seam adoption to remaining interactive handlers.
- Revisit removal of the legacy `WorkloadBalancingStage`/`WorkloadBalancingStageTests.cs`, `DependencyAwarePlacementStage`, and `TaskOrderingStage` separately if confirmed permanently obsolete.
- Implement the remaining scheduling invariant tests in `SchedulingInvariantTestsBase` from `docs/TESTING_STRATEGY.md` (Scheduling Algorithms): Dependency Chain, Completed Task Exclusion, Task Splitting Logic, State Immutability, Respect `NotBefore`. Note: Dependency Chain is expected to fail against `GoldPanningStrategy` until dependency-ordering is implemented — keep it as an isolated, clearly-labeled red test per the scheduler validation policy rather than weakening or skipping it silently.

Notes:

- The interactive branches of `SettingsHandler`, plus `HelpHandler`, `EditHandler`, `ListHandler`, and `EventCommandHandler`, remain on facade-owned rendering; do not collapse their interactive/menu-driven console output into `CommandResult.Message` — `IInteractiveConsoleFacade` is reserved for genuinely interactive (menu/key-input) flows and should not be bypassed for them.
- Invariant test ownership: general scheduling invariants (dependency chain, no overlap, duration adherence, `NotBefore`/`DueDate`, idempotency, state immutability, task splitting, completed-task exclusion, event blocking, task dropping) belong in the shared `SchedulingInvariantTestsBase` suite and should be asserted once, not re-derived per stage test file. Stage test files own only the algorithm-internal mechanics specific to that stage.

Blockers / Dependencies:

- The test suite is green; keep it green as remaining migration and scheduling work proceeds.
- Dependency-order invariant tests may expose real scheduler defects; keep correct failing tests as isolated red tests while fixing the implementation instead of weakening expected behavior.

Scheduler validation policy:

- Write invariant tests from documented scheduling rules, not from current accidental output.
- Separate failures into implementation defects, incorrect test expectations, and unclear requirements.
- Do not use characterization baselines to bless behavior that violates hard invariants.

Next steps:

1. **Implement Core Invariant Tests**: Add the remaining focused invariant tests (dependency-order and others in `docs/TESTING_STRATEGY.md`) to `SchedulingInvariantTestsBase`, using minimal, deterministic task sets.
2. **Classify Failures**: As tests are added, classify any failures as implementation defects, incorrect/outdated expectations, or unclear requirements before broadening coverage.
3. **Broaden Scheduling Characterization Coverage**: Add broader scheduling characterization coverage only after the hard invariants from step 1 are protected.

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
