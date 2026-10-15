# Scheduling Architecture

This document defines scheduling responsibilities, strategy boundaries, and correctness invariants. See [TERMINOLOGY.md](TERMINOLOGY.md) for canonical scheduling terms.

## Responsibilities

Scheduling architecture covers:

- Selecting a strategy from the effective scheduling mode.
- Building a schedule from tasks, profile settings, events, and current or simulated time.
- Keeping algorithm-specific behavior inside its strategy and stages.
- Defining scheduling invariants that apply regardless of strategy.

Scheduling logic belongs under `PriorityTaskManager/Scheduling/**`. Clients may edit scheduling settings and render results, but must not select or implement scheduling algorithms.

## Strategy Selection

For persisted core operations, `TaskManagerService.GetPrioritizedTasks(...)` resolves the effective profile from global defaults and list-scoped settings, then selects the strategy through `IUrgencyStrategy`.

The stateless API schedule route is a separate server-side path: `ScheduleComputation.Compute(...)` receives a task, event, and profile snapshot, selects a strategy from the submitted profile, and computes without reading or writing persistence. It currently performs strategy selection directly rather than calling `TaskManagerService`; keep its mode-to-strategy mapping aligned with the core service. Clients do not select strategy implementations.

## Gold Panning Pipeline

`GoldPanningStrategy` clones active tasks, runs a fixed pipeline over a shared `SchedulingContext`, then maps scheduled parts back to the original tasks:

1. `TaskNormalizationStage` applies baseline values and normalizes task constraints.
2. `AvailabilityWindowStage` builds work windows after accounting for events and computes the scheduling horizon.
3. `TaskOccurrenceExpansionStage` expands recurring task series into ephemeral occurrences once the horizon is known.
4. `TaskRankingStage` calculates task weights.
5. `TaskDistributionStage` allocates work across days, splitting tasks when needed.
6. `DailySequencingStage` assigns task chunks to available time slots.

Keep each transformation in its owning stage rather than adding cross-cutting behavior to the strategy wrapper. See [GOLD_PANNING.md](GOLD_PANNING.md) for algorithm behavior details.

## Recurring Scheduling Inputs

- Core scheduling receives incomplete tasks for the active list, the effective profile, blocking events, and current or simulated time through `ITimeService`. The stateless API computes from the request snapshot and optional simulated time.
- Recurring task series include their occurrence state, cancellation exceptions, and occurrence overrides. Expansion is ephemeral; recurring task series cannot have dependencies.
- An occurrence cannot be scheduled before its original recurrence date. Its due date derives from that date and the series due time unless an occurrence override supplies a due date; a later `NotBefore` may defer it. Completion progress reduces that occurrence's remaining effort, and zero-effort occurrences produce no time blocks.
- Count-limited series may extend the horizon through their final non-cancelled occurrence, subject to the five-year horizon cap. Forward-moving progression modes may include one unresolved occurrence beyond the horizon; `SequentialCatchUp` exposes only the oldest unresolved occurrence.
- Pending occurrences become missed after their effective due timestamp, except in `RollForwardDisregard`. Occurrences whose effective due-date calendar day is before the current schedule day remain unscheduled; an occurrence due today may still be scheduled after its due time. The schedule API returns details for unscheduled occurrences.
- Recurring event rules are expanded into blocking event occurrences when availability windows are built.

For persisted recurrence data and API request boundaries, see [ARCHITECTURE_DATA.md](ARCHITECTURE_DATA.md) and [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md).

## Constraint Solver Boundary

The Constraint Solver contract is defined in [CONSTRAINT_SOLVER.md](CONSTRAINT_SOLVER.md). Its strategy belongs under `PriorityTaskManager/Scheduling/Optimization/**` and must implement `IUrgencyStrategy`, preserving the core scheduling boundary. See [STATUS.md](STATUS.md) for its current availability.

## Results

`PrioritizationResult` contains task results with scheduled chunks, tasks or remaining fragments that could not be scheduled within the horizon, and scheduling history for diagnostics.

## Invariants And Test Policy

Treat these as correctness requirements, not behavior to weaken when an implementation or test exposes a defect:

- Scheduled chunks do not overlap, fit inside available work windows, and avoid event blocks.
- When capacity permits, scheduled chunks preserve each task's required duration.
- A dependent task is not scheduled before its prerequisites complete. Distribution defers a task while an active prerequisite has unplaced fragments; same-day sequencing keeps prerequisites ahead of dependents. Prerequisites outside the active scheduling pass are treated as satisfied. Tasks blocked for the whole horizon, including by dependency cycles, remain unscheduled.
- A task is not scheduled before its `NotBefore` date and time. Release-day capacity is limited to usable time after `NotBefore`; any remainder that cannot be placed within the horizon remains unscheduled.
- If `NotBefore` is later than `DueDate`, normalization clamps it to `DueDate`.

When tests expose a scheduler defect, retain correct invariant coverage while fixing the implementation. Distinguish implementation defects from incorrect or outdated expectations.

## Implementation Guidance

- Use `ITimeService` for scheduler time and deterministic tests.
- Core task-service scheduling reconciles recurring-series missed state before computation; it schedules expanded occurrences rather than the series template.
- The stateless API computation requires recurring-task state metadata and does not persist generated occurrences or schedule results.
- Add scheduling behavior to the appropriate strategy or stage, not to client handlers or rendering code.
- Keep transformations isolated from persisted task objects; Gold Panning clones its active inputs before pipeline execution.
