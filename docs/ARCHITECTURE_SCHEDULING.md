# Scheduling Architecture

This document defines scheduling strategy ownership, algorithm boundaries, and scheduler invariants.

## Responsibilities

Scheduling architecture covers:

- Strategy selection through `SchedulingMode` and `IUrgencyStrategy`.
- Gold Panning pipeline execution.
- Future constraint solver implementation boundaries.
- Scheduling invariants and test expectations.
- Schedule inputs from tasks, user/list settings, events, and time services.

## Strategy Selection

`TaskManagerService.GetPrioritizedTasks(...)` builds the effective profile for the active list and selects the scheduling strategy.

Current behavior:

- `GoldPanningStrategy` is the active implementation.
- Constraint optimization mode is routed but not implemented in the current runtime path.

Do not select scheduling strategies in client handlers. Clients may change settings, but core services own strategy routing.

## Gold Panning Pipeline

`GoldPanningStrategy` runs a fixed stage chain over a shared `SchedulingContext`:

1. `TaskNormalizationStage`
2. `AvailabilityWindowStage`
3. `TaskRankingStage`
4. `TaskDistributionStage`
5. `DailySequencingStage`

Each stage should have one clear responsibility. When adding scheduling behavior, prefer placing it in the stage that owns that transformation rather than adding cross-cutting logic to the strategy wrapper.

| Stage | Responsibility |
| --- | --- |
| `TaskNormalizationStage` | Apply defaults and clean task data before scheduling |
| `AvailabilityWindowStage` | Build available windows from work hours and events |
| `TaskRankingStage` | Rank tasks by urgency and importance |
| `TaskDistributionStage` | Pack tasks into available windows and split tasks when needed, gating placement so a task is never placed until its prerequisites are fully placed |
| `DailySequencingStage` | Order tasks within each day for focus and deadline safety, while keeping same-day prerequisites ahead of their dependents |

See [GOLD_PANNING.md](GOLD_PANNING.md) for the algorithm concept and behavior details.

## Constraint Solver Boundary

The constraint solver is specified separately in [CONSTRAINT_SOLVER.md](CONSTRAINT_SOLVER.md). Keep MVP and full solver work aligned with that specification.

Constraint solver code should live under `PriorityTaskManager/Scheduling/Optimization/**` and remain behind `IUrgencyStrategy` so the core service boundary remains stable.

## Scheduler Inputs And Outputs

Inputs:

- Incomplete tasks from the active list.
- Authenticated clients may submit expanded recurring-task occurrences as ordinary task placeholders; the stateless schedule endpoint does not expand or persist recurrence rules itself.
- Effective profile built from global defaults plus list-scoped overrides.
- Events that block available work time.
- Scheduling clients preserve recurring event rules in the schedule request so the availability stage can expand each blocking occurrence across the computed horizon.
- Current or simulated time from `ITimeService`.

Output:

- `PrioritizationResult.Tasks` with scheduled parts attached to tasks.
- `PrioritizationResult.UnscheduledTasks` for tasks or remaining fragments that could not be scheduled within the horizon.
- `PrioritizationResult.History` for diagnostics and explanation.

## Invariants And Test Policy

Treat documented scheduling invariants as correctness requirements:

- Scheduled chunks must not overlap on the same user timeline.
- Scheduled chunks must fit inside available work windows unless an explicit future policy allows otherwise.
- Scheduled chunks must avoid event blocks.
- Scheduled duration should preserve task duration when capacity is sufficient.
- A dependent task must never be scheduled before its prerequisite(s) complete. `TaskDistributionStage` defers a task until every id in its `Dependencies` has no remaining unplaced fragments (treating a prerequisite id outside the active scheduling pass, e.g. an already-completed task, as satisfied), and `DailySequencingStage` orders same-day tasks with a priority-guided topological sort so a same-day prerequisite always precedes its dependent. A task whose prerequisite never clears within the horizon (e.g. a dependency cycle) is reported via `PrioritizationResult.UnscheduledTasks` rather than force-placed.
- A task must never be scheduled before its `TaskItem.NotBefore` date and time. `TaskDistributionStage` gates eligible days and budgets only usable post-release capacity on the release day; a task whose `NotBefore` falls beyond the horizon is reported via `PrioritizationResult.UnscheduledTasks`. Within a day, `DailySequencingStage` picks the highest-priority task that is both dependency-ready and NotBefore-ready for the current slot cursor time; ready tasks can fill the gap ahead of a blocked task. Any duration that cannot fit within the horizon remains unscheduled, including fragments left after sequencing; it must not be forced into an unavailable slot or silently omitted. `TaskNormalizationStage` clamps `NotBefore` back to `DueDate` when `NotBefore` is later than `DueDate`, to avoid an unschedulable inverted range.

When tests expose scheduler defects, keep correct invariant tests as focused red tests while fixing the implementation. Separate implementation defects from incorrect or outdated test expectations before changing tests.

## Implementation Guidance

- Use `ITimeService` in scheduler code and tests for deterministic time behavior.
- Core task-service scheduling reconciles recurring series missed-date state using `ITimeService`; it does not schedule the series template as a one-off task.
- Prefer transformations over mutating original task lists directly; `GoldPanningStrategy` clones active tasks before pipeline execution and maps scheduled parts back to originals.
- Do not add scheduling logic to client handlers or rendering helpers.
- Do not use characterization tests to bless behavior that violates hard invariants.