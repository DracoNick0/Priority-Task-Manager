applyTo: '**'

# Copilot Instructions for Priority Task Manager

## Always Follow
- Before coding, review `docs/ARCHITECTURE.md` and `docs/STATUS.md`.
- Use `docs/DOC_INDEX.md` for canonical documentation ownership.
- Keep docs in sync with behavior changes. After code changes, update relevant `docs/*.md` and ask the user to confirm doc updates.
- Be consultative: if a request conflicts with architecture, call it out and propose an aligned approach.

## Project Conventions
- Separation of concerns:
    - `PriorityTaskManager` (Core): business logic only; no CLI/UI dependencies; validate inputs and throw specific exceptions.
    - `PriorityTaskManager.CLI`: command parsing, I/O, and orchestration only; no business logic; catch Core exceptions and show user-friendly errors.
- Scheduling ownership:
    - Scheduling/prioritization logic lives under `PriorityTaskManager/Scheduling/**`.
    - `TaskManagerService` coordinates CRUD and delegates prioritization to `IUrgencyStrategy`.
    - In scheduling pipelines, prefer transformed collections over in-place mutation.
- Coding conventions:
    - Use descriptive names.
    - Add XML comments (`///`) to public members; use inline comments only for non-obvious reasoning.
    - Every CLI command path must return clear user feedback (success or actionable error).
