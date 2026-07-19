applyTo: '**'

# Copilot Instructions for Priority Task Manager

## Start Here
- Before coding, review `docs/STATUS.md` and the architecture guidance relevant to the task.
- Start architecture review with `docs/ARCHITECTURE.md`, then read the focused `docs/ARCHITECTURE_*.md` document for the area being changed.
- For backlog or active-work context, use `docs/TODO.md` as the source of truth.
- For documentation ownership, use `docs/DOC_INDEX.md`.
- If a request conflicts with documented architecture or current status, call it out and propose an aligned path.

## Architecture Reading
- Use `docs/ARCHITECTURE.md` as the architecture map and shared-boundaries reference.
- Read `docs/ARCHITECTURE_CLI.md` before changing command handlers, console input/output, menus, dashboard rendering, or command result orchestration.
- Read `docs/ARCHITECTURE_CORE.md` before changing task, list, profile, event, dependency, or service coordination behavior.
- Read `docs/ARCHITECTURE_DATA.md` before changing models, JSON persistence, IDs, list-scoped settings, or persisted data shape.
- Read `docs/ARCHITECTURE_SCHEDULING.md` before changing prioritization, Gold Panning stages, scheduling invariants, strategy selection, or scheduler tests.
- Read `docs/ARCHITECTURE_INTEGRATIONS.md` before adding APIs, provider integrations, external intake, import flows, or new front-end boundaries.

## Documentation
- All project documentation must follow `docs/DOCUMENTATION_STANDARDS.md`.
- Consult `docs/DOCUMENTATION_STANDARDS.md` before creating, editing, reorganizing, or removing documentation.
- Keep documentation updates scoped to the canonical owner document defined in `docs/DOC_INDEX.md`.
- Update relevant documentation when behavior, command surface, architecture, status, backlog priority, or testing expectations change.

## Architecture Boundaries
- `PriorityTaskManager` contains core business logic, models, services, persistence, and scheduling. It must not depend on CLI/UI behavior.
- `PriorityTaskManager.CLI` owns command parsing, user interaction, output, and orchestration. It should catch core exceptions and show actionable user feedback.
- `TaskManagerService` coordinates task/list/profile operations and delegates prioritization through `IUrgencyStrategy`.
- Scheduling and prioritization logic belongs under `PriorityTaskManager/Scheduling/**`.

## CLI And Command Handling
- Keep command handlers focused on parsing, orchestration, and user feedback; do not add business scheduling logic to CLI handlers.
- Every command path must produce clear feedback: success, warning, usage guidance, or actionable error.
- Follow the current command orchestration migration state in `docs/ARCHITECTURE_CLI.md`, `docs/STATUS.md`, and `docs/TODO.md` before changing handler contracts.

## Scheduling And Tests
- Treat documented scheduling invariants as correctness requirements, not as behavior to weaken when tests expose defects.
- Separate implementation defects from incorrect or outdated test expectations before changing tests.
- Use deterministic time through `ITimeService`/test seams for time-sensitive behavior.
- Follow `docs/TESTING_STRATEGY.md` for test scope and quality expectations.

## Code Quality
- Prefer existing project patterns and small, focused changes.
- Use descriptive names.
- Add XML comments (`///`) to public members; use inline comments only for non-obvious reasoning.
- Validate changes with the narrowest relevant build or test command. If validation cannot be run or is already known to fail, state why and identify the blocking failure.
