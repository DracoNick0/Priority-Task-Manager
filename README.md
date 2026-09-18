# Priority Task Manager

Priority Task Manager is an automated, constraint-aware scheduling engine and productivity platform designed to reduce decision fatigue. Unlike conventional task managers that act as passive backlogs, Priority Task Manager evaluates task complexity, deadlines, topological dependencies, and calendar constraints to generate realistic, conflict-free daily work schedules.

---

## Core Differentiators

Traditional task management applications require users to manually plan and re-triage their schedules. Priority Task Manager automates this process through its staged **Gold Panning** scheduling engine:

* **Constraint-Aware Time Packing**: Dynamically places tasks into available working windows around fixed calendar events and commitments.
* **Intelligent Task Chunking**: Automatically segments large tasks across open time slots and day boundaries to maximize schedule throughput without manual intervention.
* **Cognitive Load & Deadline Sequencing**: Anchors imminent deadlines first and sequences high-complexity tasks during peak focus windows.
* **Topological Dependency Resolution**: Enforces prerequisite relationships to ensure dependent tasks are never scheduled before prerequisites are completed.
* **Realistic Capacity & Risk Visibility**: Evaluates available working hours against task workloads to surface deadline risks and prevent overcommitment.

---

## Architecture Overview

Priority Task Manager is built with a decoupled, clean architecture supporting both local-first offline workflows and cloud synchronization:

* **Core Engine (`PriorityTaskManager/`)**: Domain models, persistence interfaces, and the multi-stage scheduling pipeline implemented in .NET Core.
* **Web API (`PriorityTaskManager.API/`)**: ASP.NET Core REST API backed by PostgreSQL and deployed to Fly.io, providing authentication, data sync, and server-side schedule computation.
* **Cross-Platform Client (`PriorityTaskManager.Flutter/`)**: Desktop and Web application built with Flutter, featuring guest-first onboarding, local offline storage (Hive), and cloud synchronization.
* **Command-Line Interface (`PriorityTaskManager.CLI/`)**: Standalone .NET console application supporting interactive menus and direct commands with local JSON persistence.

![alt text](docs/images/defaults_image.png)

![alt text](docs/images/edit_image.png)

## Quick Start

Prerequisite:

- .NET SDK 8.0 or later

Build the solution:

```bash
dotnet build
```

Run the CLI:

```bash
cd PriorityTaskManager.CLI
dotnet run
```

### Flutter Client

Prerequisite:

- Flutter SDK matching the version pinned in `PriorityTaskManager.Flutter/pubspec.yaml` (`environment.sdk`). If `flutter pub get` fails with an SDK version solving error, run `flutter upgrade` (stable channel) and retry.

Fetch dependencies and run:

```bash
cd PriorityTaskManager.Flutter
flutter pub get
flutter run -d windows   # or -d chrome for web
```

The Flutter client requires a running `PriorityTaskManager.API` instance to compute schedules for logged-in accounts (Guests see a plain, sortable task list instead). By default it targets a local API at `http://127.0.0.1:5299`; pass `--dart-define=API_BASE_URL=https://tpm-api.fly.dev` to point it at the hosted production API instead (see the "Flutter (Windows, Fly.io prod)" launch config in `.vscode/launch.json`). See [docs/ARCHITECTURE_INTEGRATIONS.md](docs/ARCHITECTURE_INTEGRATIONS.md) for hosting/deployment details and [docs/WORKFLOW.md](docs/WORKFLOW.md) for running the API locally.

## Repository Map

| Path | Purpose |
| --- | --- |
| `PriorityTaskManager/` | Core models, services, persistence, and scheduling logic |
| `PriorityTaskManager.CLI/` | Command-line entry point, handlers, and console rendering |
| `PriorityTaskManager.API/` | ASP.NET Core Web API surface sharing the core service composition; hosted in production on Fly.io (`https://tpm-api.fly.dev`) |
| `PriorityTaskManager.Flutter/` | Flutter web/desktop client with local-only Hive data plus networked accounts/login and API-backed scheduling |
| `pt_prototyping/` | Standalone Flutter sandbox for UI/UX prototyping, not part of the shipped product |
| `PriorityTaskManager.Tests/` | Unit test project |
| `docs/` | Architecture, status, workflow, testing, and roadmap docs |

## Documentation Map

Use these docs as the canonical references:

- [docs/VISION.md](docs/VISION.md): long-term desired outcome, target users, and core differentiator.
- [docs/STATUS.md](docs/STATUS.md): current capabilities, limitations, known issues, and command surface.
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md): architecture map, shared boundaries, and links to focused architecture documents.
- [docs/ARCHITECTURE_INTEGRATIONS.md](docs/ARCHITECTURE_INTEGRATIONS.md): API surface, external integrations, and shared service composition.
- [docs/WORKFLOW.md](docs/WORKFLOW.md): contribution workflow and day-to-day development process.
- [docs/TESTING_STRATEGY.md](docs/TESTING_STRATEGY.md): test philosophy and quality approach.
- Repository GitHub Issues: backlog and planned work.

## For Contributors

Start with [docs/WORKFLOW.md](docs/WORKFLOW.md) if you are changing code. Use [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) to understand the system boundaries, and check [docs/STATUS.md](docs/STATUS.md) before assuming a feature is already implemented.

## License

This project is licensed under the MIT License.