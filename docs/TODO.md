# Project TODO List

> **Note:** This file is the source of truth for the backlog, roadmap, and currently in-progress work. Keep current-state feature reality in `docs/STATUS.md`; use this file for concise handoff details, blockers, dependencies, and next steps. Tasks are listed in priority order.

## (B) 1/5 Testing Overhaul and Scheduling Invariants

Status: In progress.

Completed:

- Deterministic core-service coverage is in place for current service behavior.
- First-pass CLI handler command-surface coverage exists for current handlers.
- `DeleteHandler` and `CompleteHandler` use the result-based command path.
- Shared parsing and usage behavior for migrated non-interactive handlers is centralized in `NonInteractiveCommandResultHelper`.
- Interactive console seam coverage exists for HelpHandler, EditHandler, list settings, and selected event interactive paths.
- Gold Panning invariant coverage and deterministic replay coverage exist.
- CLI handler command-surface tests no longer fail on real console clearing; `ConsoleHelper.ClearAndRenderDashboard` now tolerates environments with no attached console handle (e.g. the test host). The full test suite passes.
- `UncompleteHandler`, `DependHandler`, `TimeHandler`, `ModeHandler`, `CleanupHandler`, `AddHandler`, `ViewHandler`, and the flag-based branch of `SettingsHandler` are migrated to Program-owned `CommandResult` orchestration, each with result-path test coverage.
- `AddHandler` gained a flag-driven non-interactive path (`add <Title> [--importance] [--complexity] [--pinned|--not-pinned] [--duration] [--due]`) so task creation can be scripted/tested without the cursor-based due-date picker; the argument-free interactive flow is unchanged.
- `HelpHandler`, `EditHandler`, `ListHandler`, `EventCommandHandler`, and the unwired `EventHandler` implement `ICommandResultHandler` as thin wrappers only (unchanged interactive logic, inert result); they are not part of the Program-owned refresh/message path because they already own console rendering via `IInteractiveConsoleFacade`.

Remaining:

- Validate active Gold Panning dependency-order behavior before broad scheduling characterization baselines are accepted.
- The interactive branches of `SettingsHandler`, plus `HelpHandler`, `EditHandler`, `ListHandler`, and `EventCommandHandler`, remain on facade-owned rendering; do not collapse their interactive/menu-driven console output into `CommandResult.Message` — `IInteractiveConsoleFacade` is reserved for genuinely interactive (menu/key-input) flows and should not be bypassed for them.
- Expand interactive console seam adoption to remaining interactive handlers.
- Add dependency-order scheduling invariants and characterization tests.
- Complete the migration consolidation checklist below.

Blockers / Dependencies:

- The test suite is green; keep it green as remaining migration and scheduling work proceeds.
- CLI migration consolidation depends on every handler using one canonical command contract.
- Dependency-order invariant tests may expose real scheduler defects; keep correct failing tests as isolated red tests while fixing the implementation instead of weakening expected behavior.

Scheduler validation policy:

- Write invariant tests from documented scheduling rules, not from current accidental output.
- Separate failures into implementation defects, incorrect test expectations, and unclear requirements.
- Do not use characterization baselines to bless behavior that violates hard invariants.

Next steps:

1. Add a focused scheduler validation slice for dependency-order invariants using minimal deterministic tasks.
2. Classify any scheduler test failure as implementation defect, incorrect/outdated expectation, or unclear requirement before broadening coverage.
3. Repeat handler migration until `Program.cs` no longer needs runtime multi-contract branching.
4. Re-baseline final command orchestration tests and remove compatibility-only assertions.
5. Add broader scheduling characterization coverage only after hard invariants are protected.

### Required CLI Migration Consolidation (Do Not Skip)

- Consolidate command dispatch to one canonical handler contract after migration completes.
- Remove compatibility bridges introduced for phased migration:
  - Handler-level passthrough `Execute(...)` methods that only forward to result-based execution.
  - Runtime multi-contract branching in `Program.cs` used only for mixed legacy/result handlers.
- Preserve user feedback guarantees for every command path: success, warning, usage, or actionable error.
- Re-baseline tests for final-state dispatch and remove temporary compatibility-only assertions.
- Update docs to remove migration-only wording and document final-state command orchestration behavior.

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
