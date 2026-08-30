# Development Workflow

This document outlines the standard workflow for contributing to the Priority Task Manager project. It is designed to provide context and guidance for developers, including LLM-based coding assistants.

## Guiding Principles

-   **Task-Driven Development.** All work should be guided by a task list, tracked as GitHub Issues in this repository.
-   **Architecture First.** Before implementing a new feature, refer to `docs/ARCHITECTURE.md` to understand the existing design patterns and ensure your changes are consistent with the project's structure.
-   **Check the Status.** For a high-level overview of the project's current capabilities and state, refer to `docs/STATUS.md`.
-   **Small, iterative changes.** Prefer small, well-defined commits over large, monolithic ones.
-   **Interactive CLI Rendering.** For keyboard-driven menus, avoid calling full-screen clear/redraw on every keypress. Prefer anchored line updates via cursor positioning and preserve existing input semantics.
-   **Menu vs Input Helpers.** Put selectable menu rendering and shared selector widgets in `ConsoleMenuHelper`; keep `ConsoleInputHelper` for date/time and field-style input only.
-   **Time Mode and Refresh.** Keep the background snapshot refresher active only for real-time mode; when simulated time is applied, pause periodic refresh and resume it when returning to real-time.

## Working with AI Assistants

When using AI tools (GitHub Copilot, etc.) to contribute to this project:

1.  **Reference Definitions**: Use the terms defined in `ARCHITECTURE.md` (e.g., "Ask the TaskRankingStage to...", not "Ask the thing that puts tasks on the calendar").
2.  **Consult Documentation First**: As per `copilot.instructions.md`, always ask the AI to verify its plan against `ARCHITECTURE.md` and `STATUS.md`.
3.  **Update Documentation**: If you or the AI refactor code, you **must** update the corresponding documentation. The AI is instructed to help with this.
4.  **Hybrid Testing**: Instruct the AI to use strict TDD for deterministic code (Core services, CLI), but use exploratory spiking and property-based invariant testing for scheduling algorithms (see `TESTING_STRATEGY.md`).

## Standard Workflow

1.  **Select a Task**: Choose the highest-priority open GitHub Issue that has not yet been completed.
2.  **Understand the Goal**: Read the task description and analyze the relevant parts of the codebase. Refer to `docs/ARCHITECTURE.md` and `docs/STATUS.md` to understand how the feature fits into the overall system.
3.  **Implement the Changes**: Write the necessary code, following the established patterns.
4.  **Build and Verify**: Ensure the project builds and runs without errors.
5.  **Update Documentation**: If the changes affect the architecture, workflow, or status, update the relevant `.md` files in the `docs/` folder.
6.  **Update the GitHub Issue**: Mark the issue as complete or update its status.
7.  **Commit**: Write a clear and concise commit message describing the changes.

## Building and Running the Application

**Prerequisites:**
*   .NET SDK (8.0 or higher)

**Build the solution:**
```bash
dotnet build
```

**Run the CLI application:**
```bash
cd PriorityTaskManager.CLI
dotnet run
```

**Run the API (required before running the Flutter client):**

The Flutter client computes schedules by calling a separately-running `PriorityTaskManager.API` instance; it no longer starts this process itself, so start it first:

```bash
dotnet run --project PriorityTaskManager.API --no-launch-profile
```
Set the `LocalOnly` environment variable to `true` and `ASPNETCORE_URLS` to `http://127.0.0.1:5299` to match the Flutter client's default (no Postgres required in this mode). In VS Code, use the "API (LocalOnly :5299)" launch config, or the "API + Flutter (Windows)" compound to start both together with debugging.

**Run the API in cloud mode with dev auto-login (to exercise the authenticated `/api/schedule` route):**

The Flutter client has no login screen yet. To test the authenticated, Subscription-gated `/api/schedule` route without one, build the client with the `PTM_DEV_AUTOLOGIN` define set to `free` or `subscriber`; on startup it logs in as one of two fixed dev accounts seeded automatically by the API (`free@dev.local` / `subscriber@dev.local`, see `PriorityTaskManager.API/Dev/DevAccountSeeder.cs`) and uses the resulting token instead of calling the unauthenticated local route. This requires a reachable Postgres database (see `PriorityTaskManager.API/appsettings.json`'s default connection string) and `ASPNETCORE_ENVIRONMENT=Development` — do **not** set `LocalOnly`, since dev seeding and auth are both skipped in `LocalOnly` mode.

```bash
flutter run -d windows --dart-define=PTM_DEV_AUTOLOGIN=free        # or =subscriber
```
In VS Code, use the "API (Cloud, dev seed :5299)" + "Flutter (Windows, auto-login: Free)" (or "...Subscription)") launch configs, or the matching "API + Flutter (Windows, auto-login: ...)" compounds.

**Run the Flutter client:**
```bash
cd PriorityTaskManager.Flutter
flutter run -d windows   # or -d chrome for web
```

## Testing

The project `PriorityTaskManager.Tests/` contains the unit tests for the core library.

To run the tests:
```bash
dotnet test
```