# Architecture

This document describes the current system design of Priority Task Manager: its major projects, boundaries, data flow, and scheduling architecture. It is intentionally concise and should stay focused on stable design concepts rather than current status or roadmap detail.

For domain-specific complexity rules, see [COMPLEXITY_GUIDE.md](COMPLEXITY_GUIDE.md). For canonical vocabulary, see [TERMINOLOGY.md](TERMINOLOGY.md).

## Architecture Overview

The solution is organized into three projects with clear separation of concerns:

- `PriorityTaskManager/` is the core library. It contains the domain models, persistence layer, services, and scheduling logic.
- `PriorityTaskManager.CLI/` is the command-line shell. It parses user input, invokes core services, and renders output.
- `PriorityTaskManager.Tests/` is the test project for validating core behavior.

The architecture is designed so the core library remains independent of console presentation concerns.

## System Boundaries

The main boundaries are:

- Core business logic lives in `PriorityTaskManager/`.
- CLI orchestration and user interaction live in `PriorityTaskManager.CLI/`.
- Persistence is handled through `PersistenceService`, which reads and writes JSON data files through a data-directory path supplied at runtime.
- Scheduling behavior is selected through `UserProfile.SchedulingMode` and executed through the `IUrgencyStrategy` abstraction.

Current scheduling strategy routing is:

- `GoldPanningStrategy` is the active implementation.
- The constraint-optimization path is routed in `TaskManagerService`, but the current code path raises `NotImplementedException` when that mode is selected.

## Core Components

### `TaskManagerService`

`TaskManagerService` is the core coordination layer. It owns the in-memory `DataContainer`, applies default list setup when needed, performs CRUD-style operations, and delegates prioritization to the active urgency strategy.

### `PersistenceService`

`PersistenceService` is the storage layer for the JSON-backed application state. It loads the `DataContainer` at startup and saves changes back to disk when data mutates.

### `TimeService`

`TimeService` abstracts current time and supports simulated time so time-sensitive logic stays deterministic and testable.

### `GoldPanningStrategy`

`GoldPanningStrategy` is the current scheduling pipeline. It converts a task list into scheduled chunks by running a fixed stage chain over a shared scheduling context.

### CLI entry point

`PriorityTaskManager.CLI/Program.cs` wires the application together, constructs the service graph, and maps commands to handlers.

The CLI currently supports two handler execution contracts:

- Legacy handlers implement `ICommandHandler.Execute(...)` and manage their own command-level output flow.
- Result-based handlers implement `ICommandResultHandler.ExecuteWithResult(...)` and return a structured `CommandResult`.

For result-based handlers, `Program.cs` owns post-command dashboard refresh and message rendering based on the returned result flags.

Current migration state:

- `DeleteHandler` and `CompleteHandler` are on the result-based contract.
- Remaining handlers are still on the legacy contract and are migrated incrementally.

## Data Model Overview

The most important shared types are:

| Type | Role |
| --- | --- |
| `TaskItem` | Single unit of work with scheduling metadata and dependencies |
| `TaskList` | A list-scoped collection of tasks and copied settings |
| `UserProfile` | Global defaults and scheduling preferences |
| `Event` | A blocked-out time interval |
| `ScheduleWindow` | A free window available for work |
| `ScheduledChunk` | A scheduled portion of a task |
| `DataContainer` | The persisted application state loaded into memory |
| `SchedulingContext` | Shared pipeline state passed between stages |

## Data Flow

The runtime flow is:

1. The CLI reads a command and resolves a handler.
2. If the handler supports `ICommandResultHandler`, it returns a `CommandResult` and `Program.cs` handles refresh/message orchestration.
3. Otherwise, `Program.cs` runs the legacy `Execute(...)` path.
4. `TaskManagerService` reads or mutates the in-memory `DataContainer`.
5. When persistence is needed, `PersistenceService` writes the updated data back to JSON files.
6. For prioritization, `TaskManagerService` builds the effective profile for the active list and calls the selected `IUrgencyStrategy` implementation.
7. `GoldPanningStrategy` produces a `PrioritizationResult` containing scheduled tasks, unscheduled tasks, and history.
8. When a structured command result requests UI refresh, `Program.cs` refreshes the schedule snapshot and renders the dashboard.

## List-Scoped Settings Model

Each `TaskList` carries its own copied settings snapshot for scheduling and presentation. The active list can therefore diverge from global defaults without rewriting other lists.

The intended behavior is copy-on-create:

1. New lists copy the current global defaults from `UserProfile` when they are created.
2. Later changes to global defaults do not retroactively rewrite existing lists.
3. `TaskManagerService.BuildEffectiveUserProfile(...)` resolves the active list settings into the effective profile used by scheduling and dashboard logic.
4. `TaskManagerService.ApplyListTimePreference(...)` applies the active list's saved simulated time when switching lists.

This keeps list-specific configuration separate from global defaults while still making new lists inherit sensible starting values.

## Scheduling Strategy Architecture

The scheduling system is intentionally strategy-based so the project can evolve beyond a single algorithm without rewriting the surrounding services.

### Gold Panning

Gold Panning is the current staged pipeline. It uses a shared `SchedulingContext` and a fixed chain of independent stages to transform tasks into scheduled work.

The active stage order is:

1. `TaskNormalizationStage`
2. `AvailabilityWindowStage`
3. `TaskRankingStage`
4. `TaskDistributionStage`
5. `DailySequencingStage`

Each stage has a narrow responsibility:

| Stage | Responsibility |
| --- | --- |
| `TaskNormalizationStage` | Apply defaults and clean up task data before scheduling |
| `AvailabilityWindowStage` | Build available time windows from work hours and events |
| `TaskRankingStage` | Rank tasks by urgency and importance |
| `TaskDistributionStage` | Pack tasks into available windows and split tasks when needed |
| `DailySequencingStage` | Order tasks within a day for better focus and energy use |

### Constraint Solver

The system is designed to support an alternate optimization-based strategy selected by scheduling mode. The current code path reserves that route, but the implementation is not yet available.

## Extension Points

The main extension points are:

- Add or replace strategy implementations behind `IUrgencyStrategy`.
- Add or adjust Gold Panning stages inside the stage chain.
- Extend `TaskManagerService` for list, task, and profile coordination.
- Extend CLI handlers without moving business logic into the presentation layer.

## Architectural Invariants

The most important rules are:

- The core library must stay independent of CLI presentation concerns.
- CLI handlers must not contain business scheduling logic.
- Current behavior claims should match the observable code path.
- Command routing should match the runtime entry point.
- Gold Panning stage order should match the active stage chain in code.

## Terminology

Use the canonical terms in [TERMINOLOGY.md](TERMINOLOGY.md) when reading or modifying this system. In particular:

- Use `strategy` for the overall scheduling approach.
- Use `stage` for a Gold Panning pipeline step.
- Use `command surface` for the set of CLI commands.

## Related Files

- [PriorityTaskManager.CLI/Program.cs](../PriorityTaskManager.CLI/Program.cs)
- [PriorityTaskManager/Services/TaskManagerService.cs](../PriorityTaskManager/Services/TaskManagerService.cs)
- [PriorityTaskManager/Scheduling/GoldPanning/GoldPanningStrategy.cs](../PriorityTaskManager/Scheduling/GoldPanning/GoldPanningStrategy.cs)
