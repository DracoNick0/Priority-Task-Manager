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

The Flutter client computes schedules by calling a separately-running `PriorityTaskManager.API` instance; it no longer starts this process itself, so start it first. Scheduling is an online-exclusive, Subscription-gated capability (see [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md)), so a reachable Postgres database is required (see `PriorityTaskManager.API/appsettings.json`'s default connection string):

```bash
dotnet run --project PriorityTaskManager.API --no-launch-profile
```
Set `ASPNETCORE_URLS` to `http://127.0.0.1:5299` to match the Flutter client's default, and `ASPNETCORE_ENVIRONMENT=Development` to enable dev account seeding (`free@dev.local` / `subscriber@dev.local`, see `PriorityTaskManager.API/Dev/DevAccountSeeder.cs`) for manual login testing. In VS Code, use the "API (Cloud, dev seed :5299)" launch config, or the "API + Flutter (Windows)" compound to start both together with debugging.

**Run the Flutter client:**
```bash
cd PriorityTaskManager.Flutter
flutter run -d windows   # or -d chrome for web
```

**Run the API and Postgres together via Docker Compose:**

`docker-compose.yml` (repo root) runs the API container alongside a Postgres container, as an alternative to a manually-run Postgres container plus `dotnet run`. Copy `.env.example` to `.env` and set a real `JWT_KEY` first, then:
```bash
docker compose up
```
The API listens on `http://127.0.0.1:5299` (mapped to its internal port 8080), matching the Flutter client's default.

**Deploying the API (Fly.io):**

`PriorityTaskManager.API/Dockerfile` and `fly.toml` (repo root) define the production container and Fly.io app (`tpm-api`, internal port 8080, HTTPS terminated at Fly's edge). The Dockerfile must be built with the repo root as the build context (`docker build -f PriorityTaskManager.API/Dockerfile .`), since it needs the `PriorityTaskManager` core project reference alongside `PriorityTaskManager.API`. Deploy from the repo root with:
```bash
flyctl deploy -a tpm-api
```
Production secrets (`ConnectionStrings__Postgres`, `Jwt__Key`, etc.) are set via `flyctl secrets set` and never committed; `appsettings.json` intentionally ships no `Jwt:Key` so a missing production secret fails fast at startup. ASP.NET Core's config binder needs a double underscore for nested keys (`Jwt__Key`, not `JWT_KEY`) — a single underscore or all-caps flat name silently fails to bind.

To point a local Flutter build at the hosted production API instead of a local one, pass `--dart-define=API_BASE_URL=https://tpm-api.fly.dev` (or use the "Flutter (Windows, Fly.io prod)" launch config).

## Testing

The project `PriorityTaskManager.Tests/` contains the unit tests for the core library.

To run the tests:
```bash
dotnet test
```