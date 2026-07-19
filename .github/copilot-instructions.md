applyTo: '**'

# Copilot Instructions for Priority Task Manager

## Start Here
- Before coding, review `docs/ARCHITECTURE.md` and `docs/STATUS.md`.
- For backlog or active-work context, use `docs/TODO.md` as the source of truth.
- For documentation ownership, use `docs/DOC_INDEX.md`.
- If a request conflicts with documented architecture or current status, call it out and propose an aligned path.

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
- Follow the current command orchestration migration state in `docs/ARCHITECTURE.md`, `docs/STATUS.md`, and `docs/TODO.md` before changing handler contracts.

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
