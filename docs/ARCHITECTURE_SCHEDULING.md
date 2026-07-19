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

Do not select scheduling strategies in CLI handlers. CLI code may change settings, but core services own strategy routing.

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
| `TaskDistributionStage` | Pack tasks into available windows and split tasks when needed |
| `DailySequencingStage` | Order tasks within each day for focus and deadline safety |

See [GOLD_PANNING.md](GOLD_PANNING.md) for the algorithm concept and behavior details.

## Constraint Solver Boundary

The constraint solver is specified separately in [CONSTRAINT_SOLVER.md](CONSTRAINT_SOLVER.md). Keep MVP and full solver work aligned with that specification.

Constraint solver code should live under `PriorityTaskManager/Scheduling/Optimization/**` and remain behind `IUrgencyStrategy` so the CLI and core service boundary does not change.

## Scheduler Inputs And Outputs

Inputs:

- Incomplete tasks from the active list.
- Effective profile built from global defaults plus list-scoped overrides.
- Events that block available work time.
- Current or simulated time from `ITimeService`.

Output:

- `PrioritizationResult.Tasks` with scheduled parts attached to tasks.
- `PrioritizationResult.UnscheduledTasks` for tasks that could not be scheduled.
- `PrioritizationResult.History` for diagnostics and explanation.

## Invariants And Test Policy

Treat documented scheduling invariants as correctness requirements:

- Scheduled chunks must not overlap on the same user timeline.
- Scheduled chunks must fit inside available work windows unless an explicit future policy allows otherwise.
- Scheduled chunks must avoid event blocks.
- Scheduled duration should preserve task duration when capacity is sufficient.
- Dependency-order rules should be validated before broad characterization baselines are accepted.

When tests expose scheduler defects, keep correct invariant tests as focused red tests while fixing the implementation. Separate implementation defects from incorrect or outdated test expectations before changing tests.

## Implementation Guidance

- Use `ITimeService` in scheduler code and tests for deterministic time behavior.
- Prefer transformations over mutating original task lists directly; `GoldPanningStrategy` clones active tasks before pipeline execution and maps scheduled parts back to originals.
- Do not add scheduling logic to CLI handlers or rendering helpers.
- Do not use characterization tests to bless behavior that violates hard invariants.