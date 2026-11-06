# Development Workflow

This document outlines the standard workflow for contributing to the Priority Task Manager project. It is designed to provide context and guidance for developers, including LLM-based coding assistants.

## Guiding Principles

-   **Task-Driven Development.** All work should be guided by a task list, tracked as GitHub Issues in this repository.
-   **Architecture First.** Before implementing a new feature, refer to `docs/ARCHITECTURE.md` to understand the existing design patterns and ensure your changes are consistent with the project's structure.
-   **Check the Status.** For a high-level overview of the project's current capabilities and state, refer to `docs/STATUS.md`.
-   **Reviewable commits.** Keep each commit focused on one coherent outcome, with its implementation, tests, and relevant documentation together. Split independent changes only when each resulting commit remains meaningful and reviewable; do not split solely by file, project, or line count.

## Working with AI Assistants

When using AI tools (GitHub Copilot, etc.) to contribute to this project:

1.  **Reference Definitions**: Use the terms defined in `ARCHITECTURE.md` (e.g., "Ask the TaskRankingStage to...", not "Ask the thing that puts tasks on the calendar").
2.  **Consult Documentation First**: As per `copilot.instructions.md`, always ask the AI to verify its plan against `ARCHITECTURE.md` and `STATUS.md`.
3.  **Update Documentation**: If you or the AI refactor code, you **must** update the corresponding documentation. The AI is instructed to help with this.
4.  **Testing**: Use focused tests for deterministic core behavior and deterministic scenario-based invariant tests for scheduling algorithms (see `TESTING_STRATEGY.md`).

### Plan-Proceed-Review Workflow

For Copilot-assisted issue, feature, or fix work, plan first and implement one reviewable segment at a time. A request to implement still starts with a plan; the contributor can explicitly ask to skip these checkpoints.

1. **Plan:** Read the issue and check its prerequisites when provided. For a standalone request, clarify the intended outcome and acceptance criteria when needed. Consult the relevant status and architecture guidance before implementation.
2. **Define segments:** Present an ordered set of coherent, commit-worthy outcomes. Each segment should remain understandable and buildable after its prerequisites, and include acceptance criteria, focused automated checks, and manual user-test steps when the behavior is user-observable. If no useful split exists, use one segment rather than fragmenting by file or size.
3. **Wait for `proceed`:** Do not start implementation after presenting the plan. Each `proceed` authorizes exactly the next segment.
4. **Implement and verify one segment:** Change only that segment, keep its implementation, tests, and relevant documentation together, and run its targeted checks. Report the changes, validation results, and manual test steps where useful. If a segment is not meaningfully user-testable, say why and report its automated checks.
5. **Stop for review:** Wait for the contributor to review or test the segment. If they give feedback, revise and revalidate the current segment before advancing. Start the next segment only after another `proceed`.

Segments are prepared for independent review and possible commits, but `proceed` is not permission to stage or commit. The contributor controls when commits are made.

## Standard Workflow

1.  **Select a Task**: Choose the highest-priority open GitHub Issue that has not yet been completed.
2.  **Understand the Goal**: Read the task description and analyze the relevant parts of the codebase. Refer to `docs/ARCHITECTURE.md` and `docs/STATUS.md` to understand how the feature fits into the overall system.
3.  **Implement the Changes**: Write the necessary code, following the established patterns. For AI-assisted issue, feature, or fix work, follow the Plan-Proceed-Review workflow above.
4.  **Build and Verify**: Ensure the project builds and runs without errors; AI-assisted work should verify each segment before presenting it for review.
5.  **Update Documentation**: If the changes affect the architecture, workflow, or status, update the relevant `.md` files in the `docs/` folder.
6.  **Update the GitHub Issue**: Mark the issue as complete or update its status.
7.  **Commit**: Review the staged diff and write a clear Conventional Commit message describing the outcome. Choose one commit or a split based on useful review boundaries, not a fixed size target. Keep tests and documentation with the behavior they verify or describe. See the [commit-message skill](../.github/skills/commit-message/SKILL.md) for detailed boundary and message guidance.

### Git Worktrees on Windows (Short Path Requirement)

When creating a Git worktree on Windows (especially when developing or running the Flutter client):
1. **Always use a short root path**: Create the worktree under a short root directory such as `<drive>:\wt\<branch>` (e.g. `E:\wt\<branch>`).
   ```powershell
   git worktree add "E:\wt\my-feature" -b feature/my-feature
   code "E:\wt\my-feature"
   ```
2. **Avoid deep or nested paths**: Never place worktrees inside nested folders (like `<repo-dir>.worktrees\<branch>`) or paths with spaces. Flutter Windows desktop builds generate ephemeral plugin symlinks (`flutter_secure_storage_windows`) that consume ~160 characters. When the base worktree path exceeds ~100 characters, the total path exceeds the Windows 260-character `MAX_PATH` limit and CMake builds abort.
3. **Maintain consistent paths**: Windows CMake hardcodes absolute source and build paths in `CMakeCache.txt`. Always open and build the worktree from the exact same path and drive letter. Switching between physical paths and virtual drive letters (`subst`) invalidates the CMake cache and requires `flutter clean` and `flutter pub get`.

## Building and Running the Application

**Prerequisites:**
*   .NET SDK (10.0 or higher)
*   Flutter SDK satisfying the `environment.sdk` constraint in `PriorityTaskManager.Flutter/pubspec.yaml` (to run the web or Windows client)

**Build the solution:**
```bash
dotnet build
```

**Run the API (for authenticated API-backed features):**

The Flutter client can start in Guest mode without the API. Start the API to test authenticated account features and schedule computation; the API does not start with the Flutter client. Scheduling is an online-exclusive, Subscription-gated capability (see [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md)), so a reachable Postgres database is required (see `PriorityTaskManager.API/appsettings.json`'s default connection string):

```bash
dotnet run --project PriorityTaskManager.API --no-launch-profile
```
Set `ASPNETCORE_URLS` to `http://127.0.0.1:5299` to match the Flutter client's default, and `ASPNETCORE_ENVIRONMENT=Development` to enable dev account seeding (`free@dev.local` / `subscriber@dev.local`, see `PriorityTaskManager.API/Dev/DevAccountSeeder.cs`) for manual login testing. In VS Code, use the "API (Cloud, dev seed :5299)" launch config, or the "API + Flutter (Windows)" compound to start both together with debugging.

**Run the Flutter client:**

Flutter package configuration and native plugin links are generated locally and are not shared between Git worktrees. When opening the repository root in VS Code, the "Flutter: Get dependencies" task runs on folder open and before each Flutter debug launch. If running from a terminal or creating a worktree outside VS Code, fetch dependencies from the Flutter package directory first:

```bash
cd PriorityTaskManager.Flutter
flutter pub get
flutter run -d windows   # or -d chrome for web
```

For Windows desktop builds, create each worktree under a short path such as `E:\wt\<branch>` and always open it through that same path. Windows CMake caches absolute source and build paths; opening one worktree sometimes through a `subst` drive and sometimes through its original path makes the cache invalid. If you use a `subst` drive for an existing worktree, keep using that drive for every build.

```powershell
subst T: "E:\path\to\worktree"
```

The VS Code Windows Flutter launch configurations run `tool/prepare_windows_build.ps1` before debugging. It restores Flutter dependencies, removes only the generated `build\windows` output when its CMake cache belongs to a different path, and stops with an actionable message if the generated plugin header path reaches 260 characters. This prevents stale-cache errors but cannot make an overlong worktree path buildable.

When launching Windows from a terminal, run the same preflight from the repository root before `flutter run -d windows`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\PriorityTaskManager.Flutter\tool\prepare_windows_build.ps1 -FlutterProjectDirectory (Resolve-Path .\PriorityTaskManager.Flutter).Path
```

If the preflight reports an overlong path, create the worktree in a short location and open that exact location in VS Code. For other stale generated plugin state, run `flutter clean` followed by `flutter pub get` from `PriorityTaskManager.Flutter`.

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

**Deploying the Web Client (Cloudflare Workers Static Assets):**

`PriorityTaskManager.Flutter/wrangler.jsonc` configures the static asset deployment to the Cloudflare Worker (`priority-task-manager`, serving `./build/web`). Cloudflare Workers Builds runs the configured build and deploy commands for GitHub Preview and Production builds. Preview builds use `npx wrangler preview`, which requires the `previews` block in the Wrangler config; Production builds use `npx wrangler deploy`.

1. Build the Flutter web application targeting the production API:
   ```bash
   cd PriorityTaskManager.Flutter
   flutter build web --dart-define=API_BASE_URL=https://tpm-api.fly.dev --release
   ```
2. Deploy the static assets to Cloudflare:
   ```bash
   npx wrangler deploy
   ```

3. Configure CORS on Fly.io (if origin restrictions are added):
   Set the allowed origin in Fly secrets or environment variables:
   ```bash
   flyctl secrets set Cors__AllowedOrigins__0="https://<your-cloudflare-pages-domain>" -a tpm-api
   ```

**The API deployment is manual; the web client uses Cloudflare Workers Builds.** Cloudflare runs the configured build/deploy commands for the connected GitHub branches. Changes under `PriorityTaskManager.API/` or `PriorityTaskManager/` (new/changed endpoints, routes, request/response contracts, persistence, auth) are **not live** until the API is redeployed with `flyctl deploy`. Run `flyctl releases -a tpm-api` to check the last deploy date/commit before assuming the hosted API matches `main`, and redeploy after merging such a change if the hosted instance needs it (e.g. before testing the Flutter client against `https://tpm-api.fly.dev`).

## Testing

The active .NET tests are under `PriorityTaskManager.Tests/` and cover core services, scheduling, and API contracts; archived CLI test sources are excluded. The Flutter client has a separate suite under `PriorityTaskManager.Flutter/test/`.

To run the tests:
```bash
dotnet test
```

From the Flutter package directory, run:
```bash
flutter analyze
flutter test
```