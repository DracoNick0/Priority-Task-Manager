# Architecture

This document is the architecture map for Priority Task Manager. It explains the major system boundaries and points to focused architecture documents for implementation guidance.

For current feature reality, see [STATUS.md](STATUS.md). For planned work and active handoff, see [TODO.md](TODO.md). For canonical vocabulary, see [TERMINOLOGY.md](TERMINOLOGY.md).

## Architecture Overview

The solution is organized into three projects with clear separation of concerns:

| Project | Responsibility |
| --- | --- |
| `PriorityTaskManager/` | Core models, services, persistence, and scheduling logic |
| `PriorityTaskManager.CLI/` | Command parsing, user interaction, console output, and orchestration |
| `PriorityTaskManager.Tests/` | Tests for core behavior, scheduling, and CLI handler behavior |

The core library must remain independent of console presentation concerns. CLI code may call core services, but core services must not depend on CLI handlers, console helpers, or command output policy.

## Focused Architecture Documents

Use the narrowest document that matches the task before reading the whole architecture set.

| Area | Document | Use When |
| --- | --- | --- |
| CLI and user interaction | [ARCHITECTURE_CLI.md](ARCHITECTURE_CLI.md) | Changing command handlers, console input/output, dashboard refresh, menus, or command result orchestration |
| Business logic | [ARCHITECTURE_CORE.md](ARCHITECTURE_CORE.md) | Changing task, list, profile, event, dependency, or service coordination behavior |
| Data and persistence | [ARCHITECTURE_DATA.md](ARCHITECTURE_DATA.md) | Changing models, JSON storage, persisted defaults, IDs, list-scoped settings, or migration-sensitive data shape |
| Scheduling and algorithms | [ARCHITECTURE_SCHEDULING.md](ARCHITECTURE_SCHEDULING.md) | Changing prioritization, Gold Panning stages, scheduling invariants, or strategy selection |
| Integrations and expansion boundaries | [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md) | Adding APIs, external source intake, provider abstractions, import flows, or new front ends |

The complete architecture should be understandable by reading this map plus all focused architecture documents together.

## System Boundaries

- Core business logic lives in `PriorityTaskManager/`.
- CLI orchestration and user interaction live in `PriorityTaskManager.CLI/`.
- Persistence is handled by `PersistenceService` through JSON files loaded into a `DataContainer`.
- Scheduling behavior is selected through `UserProfile.SchedulingMode` and executed through `IUrgencyStrategy`.
- External integrations are planned but not currently implemented; integration design should preserve the same core/CLI separation.

## Runtime Data Flow

1. `Program.cs` starts the CLI, loads persisted data, builds services, and maps command names to handlers.
2. The CLI reads a command and resolves the handler.
3. Result-based handlers return `CommandResult`; `Program.cs` owns message rendering and dashboard refresh for that path.
4. Legacy handlers still run through `ICommandHandler.Execute(...)` during migration.
5. `TaskManagerService` reads or mutates the in-memory `DataContainer` and persists changes through `IPersistenceService`.
6. Scheduling requests build an effective profile for the active list and call the selected `IUrgencyStrategy`.
7. The active scheduler returns a `PrioritizationResult` with scheduled tasks, unscheduled tasks, and history.
8. CLI rendering uses a refreshed `ScheduleSnapshot` rather than making rendering helpers own scheduling decisions.

## Architectural Invariants

- Core must not reference CLI presentation concerns.
- CLI handlers must not contain business scheduling logic.
- Command feedback must remain explicit: success, warning, usage guidance, or actionable error.
- Current behavior claims must match observable code paths and current status documentation.
- Gold Panning stage order must match the active stage chain in code.
- Scheduling invariants documented as correctness rules must not be weakened to match defective implementation behavior.

## Related Specifications

- [GOLD_PANNING.md](GOLD_PANNING.md) describes the Gold Panning algorithm concept and behavior.
- [CONSTRAINT_SOLVER.md](CONSTRAINT_SOLVER.md) specifies the planned constraint solver contract.
- [TESTING_STRATEGY.md](TESTING_STRATEGY.md) defines test scope and invariant-testing expectations.
- [COMPLEXITY_GUIDE.md](COMPLEXITY_GUIDE.md) defines complexity scale semantics.

## Terminology

Use [TERMINOLOGY.md](TERMINOLOGY.md) for canonical vocabulary. In particular, use `strategy` for an overall scheduling approach, `stage` for a Gold Panning pipeline step, and `command surface` for the supported CLI commands.
