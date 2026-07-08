# Project Status

**Framework**: .NET 8 Console
**Storage**: Local JSON files

This document is the current-state snapshot for Priority Task Manager. It records what is working now, what is partial, what is broken, and what is under active revision.

## Feature Matrix

| Feature Area | Status | Notes |
| --- | --- | --- |
| Task management | 🟢 Working | Core add, edit, view, complete, uncomplete, delete, and dependency commands are available through the CLI. |
| List management | 🟢 Working | Create, switch, delete, and list flows work, and lists carry copied settings for scheduling and presentation. |
| Data persistence | 🟢 Working | JSON-backed data loads and saves through the core persistence service. |
| Settings and defaults | 🟢 Working | `defaults` controls global defaults, while list-specific settings are edited on the active list. |
| Scheduling logic | 🟡 Partially implemented | Gold Panning is active; the constraint-optimization mode is routed but not implemented in the current code path. |
| Event system | 🟡 Under review | Add, edit, list, and delete are available, but the event experience is still being refined. |
| Task dependencies | 🟡 Under review | Dependency handling is supported, but correctness and migration coverage still need attention. |
| Unit tests | 🟡 Under overhaul | Test architecture is realigned to Gold Panning naming, deterministic core-service coverage is in place, and first-pass CLI handler command-surface tests are added; scheduling invariants/replay coverage is still pending. |

## Confirmed Capabilities

- The CLI starts from `PriorityTaskManager.CLI/Program.cs` and wires the core services at startup.
- The core library owns business logic, persistence coordination, and scheduling behavior.
- Gold Panning is the currently active scheduling strategy.
- The app stores data in JSON files and loads that state into memory on startup.
- Lists can carry copied settings snapshots instead of mutating only global defaults.

## Known Limitations

- The application is designed for a single local user.
- There is no undo/redo system.
- Recurring tasks are not supported.
- The constraint-optimization scheduling path is not implemented yet.

## Known Issues and Technical Debt

- The scheduling system still needs future refinement around slack handling, intra-day focus heuristics, and backlog fairness.
- The event workflow is functional but still under UX refinement.
- The test suite overhaul is in progress; deterministic core-service coverage and first-pass CLI handler command-surface coverage are now in place, while deep interactive CLI flows and scheduling invariant/replay coverage remain pending.

## Command Surface Summary

### Top-level commands

| Command | Purpose |
| --- | --- |
| `add` | Add a new task |
| `list` | Show tasks for the active list |
| `edit` | Edit an existing task |
| `delete` | Delete tasks |
| `complete` | Mark tasks complete |
| `uncomplete` | Mark tasks incomplete |
| `depend` | Manage task dependencies |
| `view` | Show task details |
| `cleanup` | Archive completed tasks and refresh list state |
| `help` | Show command help |
| `defaults` | Edit global default settings |
| `event` | Manage events |
| `e` | Shortcut for `event` |
| `time` | View or change simulated time |
| `mode` | View or change scheduling mode |
| `exit` | Quit the CLI |

### Common subcommand groups

| Group | Examples |
| --- | --- |
| `list` | `list all`, `list create`, `list switch`, `list delete`, `list settings` |
| `event` | `event add`, `event list`, `event edit`, `event delete` |
| `time` | `time now`, `time custom` |
| `defaults` | Interactive menu for global defaults |