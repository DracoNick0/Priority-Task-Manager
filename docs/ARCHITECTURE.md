# Architecture

This document is the architecture map for Priority Task Manager. It explains the major system boundaries and points to focused architecture documents for implementation guidance.

For current feature reality, see [STATUS.md](STATUS.md). For planned work and active handoff, see the repository's GitHub Issues. For canonical vocabulary, see [TERMINOLOGY.md](TERMINOLOGY.md).

## Architecture Overview

The active system is organized around the core library, API, and Flutter client:

| Project | Responsibility |
| --- | --- |
| `PriorityTaskManager/` | Core models, services, persistence, and scheduling logic |
| `PriorityTaskManager.API/` | Authenticated account, task, list, event, profile, and archive endpoints backed by core services and account-scoped Postgres, plus stateless schedule computation |
| `PriorityTaskManager.Flutter/` | Flutter web and Windows desktop client with Guest-local and Authenticated API-backed repositories, plus an API-backed schedule repository (see [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md)) |
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
- The API owns authenticated network access and account-scoped Postgres persistence for account data. Schedule computation is a separate stateless route that accepts a client-supplied snapshot and does not read or write account persistence.
- The Flutter client owns user interaction and selects local Hive storage for Guests or API-backed storage for authenticated sessions. It requests schedule computation separately through the API.
- The CLI source is archived and is not an active client or supported product surface.
- Core resolves the effective scheduling mode from profile defaults and list-scoped settings, then delegates computation through `IUrgencyStrategy`.
- External integrations are planned but not currently implemented; integration design should preserve the core/client separation.

## Runtime Data Flow

1. The Flutter client selects local storage for Guest mode or the API-backed repository for an authenticated session.
2. Guest task, list, event, and profile changes stay in Hive; authenticated account operations use the API and account-scoped Postgres persistence.
3. For authenticated sessions, Flutter sends tasks, events, the effective profile, and optional simulated time to `/api/schedule`; successful computation requires the Subscription tier. The API computes a schedule with the selected strategy and returns it without reading or writing account persistence.
4. Guests do not use online scheduling; they see a sortable task list and events instead of a computed schedule.

## Architectural Invariants

- Core must not reference client presentation concerns.
- Client code must not duplicate business scheduling logic.
- Active user flows must provide clear success, warning, or actionable error feedback.
- Gold Panning stage order and scheduling correctness invariants are defined in [ARCHITECTURE_SCHEDULING.md](ARCHITECTURE_SCHEDULING.md); implementation defects do not justify weakening them.

## Related Specifications

- [GOLD_PANNING.md](GOLD_PANNING.md) describes the Gold Panning algorithm concept and behavior.
- [CONSTRAINT_SOLVER.md](CONSTRAINT_SOLVER.md) specifies the planned constraint solver contract.
- [TESTING_STRATEGY.md](TESTING_STRATEGY.md) defines test scope and invariant-testing expectations.
- [COMPLEXITY_GUIDE.md](COMPLEXITY_GUIDE.md) defines complexity scale semantics.

## Terminology

Use [TERMINOLOGY.md](TERMINOLOGY.md) for canonical vocabulary. In particular, use `strategy` for an overall scheduling approach and `stage` for a Gold Panning pipeline step.
