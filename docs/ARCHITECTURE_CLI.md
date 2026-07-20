# CLI Architecture

This document defines the architecture for command-line interaction, command handlers, console input/output, and dashboard rendering.

## Responsibilities

`PriorityTaskManager.CLI/` owns:

- CLI startup and service wiring.
- Command parsing and handler dispatch.
- User interaction, prompts, menus, and console output.
- Dashboard refresh and rendering policy.
- Translation of core exceptions or failed operations into actionable user feedback.

The CLI must not own business scheduling decisions, task/list persistence rules, or core data invariants.

## Key Entry Points

| Type or File | Role |
| --- | --- |
| `Program.cs` | Builds services, maps command names to handlers, owns result-based refresh/message orchestration |
| `Handlers/*Handler.cs` | Parse command arguments, call `TaskManagerService`, and report user-facing outcomes |
| `ICommandResultHandler` | Canonical command contract implemented by every wired handler; returns a structured `CommandResult` |
| `CommandResult` | Structured command outcome with status, message, and dashboard refresh flag |

## Command Handling Principles

- Keep handlers thin: parse input, call services, handle expected errors, and report outcomes.
- Put reusable non-interactive parsing and usage-message logic in `NonInteractiveCommandResultHelper` instead of duplicating it in handlers.
- Every handler implements `ICommandResultHandler` and returns a `CommandResult`; interactive/menu-driven handlers that already own their own console rendering may return an inert `CommandResult` (no message, no refresh).
- Preserve clear command feedback for every path: success, warning, usage guidance, or actionable error.
- Do not add scheduling or persistence business rules to handlers; add them to core services or scheduling stages.

## Console Input And Interaction

Use existing console helpers before adding new prompt logic:

| Helper | Use For |
| --- | --- |
| `ConsoleInputHelper` | Shared user-input routines such as integer prompts, boolean prompts, duration parsing, date input, time input, and task ID parsing |
| `IInteractiveConsoleFacade` | Testable seam for interactive menus, key input, cursor-sensitive editing, and dashboard clearing in interactive flows |
| `InteractiveConsoleFacade` | Default implementation of the interactive console seam |
| `ConsoleMenuHelper` | Menu drawing and adjustable/toggle menu support |

When adding user-input behavior, extend or reuse `ConsoleInputHelper` if the behavior is general-purpose and console-oriented. Use `IInteractiveConsoleFacade` for interactive flows that need tests or cursor/key handling. Avoid creating command-local prompt utilities unless the behavior is truly specific to one command.

## Dashboard And Schedule Rendering

- `ScheduleSnapshotProvider` builds and caches the latest active-list schedule snapshot.
- `ConsoleHelper.ClearAndRenderDashboard(...)` renders from the cached snapshot and should not own scheduling decisions.
- Handlers that mutate schedule-relevant data should refresh the snapshot through the established orchestration path.
- Result-based handlers should use `CommandResult.ShouldRefreshDashboard` and let `Program.cs` perform refresh/rendering.

## Current Command Dispatch

Every handler wired in `Program.cs` implements `ICommandResultHandler`. `Program.cs` dispatches through a single `Dictionary<string, ICommandResultHandler>` and calls `ExecuteWithResult(...)` for every command; there is no remaining legacy `ICommandHandler` contract or multi-contract branching.

- New command behavior must implement `ICommandResultHandler`.
- Interactive/menu-driven handlers (`HelpHandler`, `EditHandler`, `ListHandler`, `EventCommandHandler`) still own their console rendering end-to-end through `IInteractiveConsoleFacade` and return an inert `CommandResult`; do not collapse their rendering into `CommandResult.Message`.

## Invariants

- CLI code may depend on core services; core code must not depend on CLI code.
- Interactive console APIs should be behind test seams when behavior needs automated coverage.
- Command handlers must not silently succeed or fail; every command path should produce feedback.
- Dashboard rendering must not be the only place where a command communicates success or failure.
- All wired command handlers implement the single `ICommandResultHandler` contract; do not reintroduce a parallel legacy dispatch path.