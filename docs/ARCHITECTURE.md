# Architecture

This document is the architecture map for Priority Task Manager. It explains the major system boundaries and points to focused architecture documents for implementation guidance.

For current feature reality, see [STATUS.md](STATUS.md). For planned work and active handoff, see the repository's GitHub Issues. For canonical vocabulary, see [TERMINOLOGY.md](TERMINOLOGY.md).

## Architecture Overview

The active system is organized around the core library, API, and Flutter client:

| Project | Responsibility |
| --- | --- |
| `PriorityTaskManager/` | Core models, services, persistence, and scheduling logic |
| `PriorityTaskManager.API/` | Authenticated REST endpoints wrapping core services for networked clients |
| `PriorityTaskManager.Flutter/` | Flutter web/desktop client; local-only (offline/guest) for the MVP shell, with its own `TaskRepository` abstraction (see [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md)) |
| `PriorityTaskManager.Tests/` | Tests for core behavior, scheduling, and API behavior |
| `PriorityTaskManager.CLI/` (archived) | Preserved source only; excluded from the active solution build and test suite |

## Focused Architecture Documents

Use the narrowest document that matches the task before reading the whole architecture set.

| Area | Document | Use When |
| --- | --- | --- |
| Archived CLI reference | [ARCHITECTURE_CLI.md](ARCHITECTURE_CLI.md) | Only when explicitly restoring the CLI |
| Business logic | [ARCHITECTURE_CORE.md](ARCHITECTURE_CORE.md) | Changing task, list, profile, event, dependency, or service coordination behavior |
| Data and persistence | [ARCHITECTURE_DATA.md](ARCHITECTURE_DATA.md) | Changing models, JSON storage, persisted defaults, IDs, list-scoped settings, or migration-sensitive data shape |
| Scheduling and algorithms | [ARCHITECTURE_SCHEDULING.md](ARCHITECTURE_SCHEDULING.md) | Changing prioritization, Gold Panning stages, scheduling invariants, or strategy selection |
| Integrations and expansion boundaries | [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md) | Adding APIs, external source intake, provider abstractions, import flows, or new front ends |

The complete architecture should be understandable by reading this map plus all focused architecture documents together.

## System Boundaries

- Core business logic lives in `PriorityTaskManager/`.
- The API owns authenticated network access and account-scoped Postgres persistence; the Flutter client owns user interaction and local Guest data.
- The CLI source is archived and is not an active client or supported product surface.
- Scheduling behavior is selected through `UserProfile.SchedulingMode` and executed through `IUrgencyStrategy`.
- External integrations are planned but not currently implemented; integration design should preserve the core/client separation.

## Runtime Data Flow

1. The Flutter client starts in Guest or authenticated mode and selects the corresponding task repository.
2. Guest task/list/event changes use local Hive storage; authenticated account operations call the API.
3. The API authenticates and scopes account operations, then calls core services backed by Postgres.
4. Authenticated schedule requests call the selected `IUrgencyStrategy` through the API; the Flutter client renders returned schedules, while Guests see a plain task list.

## Architectural Invariants

- Core must not reference client presentation concerns.
- Client code must not duplicate business scheduling logic.
- Active user flows must provide clear success, warning, or actionable error feedback.
- Current behavior claims must match observable code paths and current status documentation.
- Gold Panning stage order must match the active stage chain in code.
- Scheduling invariants documented as correctness rules must not be weakened to match defective implementation behavior.

## Related Specifications

- [GOLD_PANNING.md](GOLD_PANNING.md) describes the Gold Panning algorithm concept and behavior.
- [CONSTRAINT_SOLVER.md](CONSTRAINT_SOLVER.md) specifies the planned constraint solver contract.
- [TESTING_STRATEGY.md](TESTING_STRATEGY.md) defines test scope and invariant-testing expectations.
- [COMPLEXITY_GUIDE.md](COMPLEXITY_GUIDE.md) defines complexity scale semantics.

## Terminology

Use [TERMINOLOGY.md](TERMINOLOGY.md) for canonical vocabulary. In particular, use `strategy` for an overall scheduling approach and `stage` for a Gold Panning pipeline step.
