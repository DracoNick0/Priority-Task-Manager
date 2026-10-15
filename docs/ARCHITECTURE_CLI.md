# Archived CLI Architecture

> This document covers the preserved `PriorityTaskManager.CLI/` source only. The CLI is retired, unsupported, and excluded from the active solution build and routine tests; it is not an active product surface. Consult this reference only when explicitly restoring the CLI, and revalidate its details against the source and active architecture documents before relying on them.

## Responsibilities

The CLI host owns command dispatch, console interaction, and user-facing output. It delegates task, list, profile, event, and scheduling operations to the core library; it must not define business rules or scheduling decisions.

## Dispatch and Results

- `Program.cs` composes the core services, registers command handlers in a dictionary, and dispatches wired commands through `ICommandResultHandler`.
- `CommandResult` carries a status, message, and dashboard-refresh flag. The host prints non-empty messages and refreshes the dashboard when requested.
- Linear commands return results for host-level output and refresh. Interactive flows that control their own prompts, key handling, and rendering may write directly through the console facade and return an inert result.
- Keep handlers focused on input interpretation and user interaction. Put reusable business rules in core services or scheduling code.

### Current Implementation Caveats

These are observations about the preserved source, not architectural rules:

- `Program.cs` does not use `CommandResult.Status` when presenting or dispatching a result.
- The host discards dashboard-refresh errors, and some handler paths do the same. This does not relax the invariant that failures should be communicated clearly.

## Console Interaction

Reuse the existing helpers rather than adding command-specific prompt or menu infrastructure:

| Helper | Responsibility |
| --- | --- |
| `ConsoleInputHelper` | Shared input and parsing routines, including numeric, boolean, duration, date/time, and task-ID input |
| `IInteractiveConsoleFacade` | Test seam for console operations used by interactive flows, including key input, cursor-sensitive editing, and menu operations |
| `InteractiveConsoleFacade` | Default console-backed implementation of the interactive facade |
| `ConsoleMenuHelper` | Menu drawing, selection, toggle, and adjustable-value interactions |

Use the facade for interactive flows that depend on key, cursor, or menu behavior so those operations can be substituted in tests.

## Dashboard Rendering

- `ScheduleSnapshotProvider` builds and caches a schedule snapshot for the active list.
- `ConsoleHelper.ClearAndRenderDashboard(...)` renders the cached snapshot; it does not own scheduling decisions. It skips console clearing when `Console.Clear()` throws `IOException`, as can happen in a host without an attached console.
- Result-based commands request dashboard refresh through `CommandResult.ShouldRefreshDashboard`; interactive flows that render directly own their refresh timing.

## Invariants

- CLI code may depend on core services; core code must not depend on CLI code or console presentation.
- Business validation and scheduling decisions belong in the core library and scheduling components, not in command handlers.
- Command outcomes, including errors and usage guidance, must be communicated to the user; dashboard rendering alone is not command feedback.
- Interactive key, cursor, and menu behavior should use the console facade when it needs an automated test seam.
- Dashboard rendering must display prepared schedule data, not recalculate scheduling.

See [ARCHITECTURE.md](ARCHITECTURE.md) for active system boundaries and [TESTING_STRATEGY.md](TESTING_STRATEGY.md) for active testing guidance. The archived CLI test sources are excluded from routine tests.
