# Integrations Architecture

This document defines boundaries for future integrations, external intake, APIs, and additional front ends.

## Scope

This document defines the architectural boundaries to use when integration work is introduced. Use [STATUS.md](STATUS.md) for current runtime maturity, the repository's GitHub Issues for planned integration work, and [LLM_ASSISTED_INTAKE.md](LLM_ASSISTED_INTAKE.md) for LLM-assisted intake planning.

## Responsibilities

Future integration architecture should cover:

- API or service layers for additional clients.
- External source providers such as GitHub, calendars, documents, todo exports, or Canvas.
- Candidate extraction and normalization flows.
- Review-and-confirm workflows before persistence.
- Provider guardrails such as rate limits, retries, validation, and source traceability.

## Boundary Principles

- Integrations should call core services rather than duplicating task/list/event persistence logic.
- External provider code should sit behind source-specific abstractions.
- Imported content should be normalized into candidate models before becoming persisted tasks, lists, or events.
- User review should happen before generated or imported items are persisted.
- Source metadata and decision traceability should be preserved for imported candidates.

## Placement Guidance

- Put reusable intake, normalization, validation, and persistence orchestration in core or a future service layer, not in CLI-only code.
- Put provider-specific clients behind interfaces so new providers do not change core scheduling or persistence contracts.
- Keep UI review flows thin; they should present candidates and call core approval/persistence operations.
- Keep LLM/provider configuration and guardrails separate from scheduling algorithms.
- Each additional front end (CLI, API, Android, or future clients) should build core services the same way: construct the same concrete service graph (`TaskManagerService`, `IPersistenceService`, `ITimeService`, and related services) rather than each front end re-deriving its own wiring conventions. When a second front end is introduced, extract the shared construction steps into a common composition helper in the core or a thin shared bootstrap layer instead of duplicating `Program.cs`-style manual wiring per front end.
- `PriorityTaskManager.API` is multi-tenant: every persisted task/list/event/profile document is scoped by account (`Account` model, see [ARCHITECTURE_DATA.md](ARCHITECTURE_DATA.md)). Requests authenticate with a JWT bearer token (issued by `POST /api/auth/register` / `POST /api/auth/login`) whose claims resolve to an account id; the API builds the core service graph (`ServiceComposer.Compose`) per request scoped to that account rather than sharing one service graph across all accounts. Front ends that are inherently single-user/local (the CLI) have no account concept today and are unaffected; wiring the CLI (or a future Flutter client) to actually authenticate against this API is tracked separately by issue #44, targeted at V1, not required for the MVP account-model work in #35.
- Each front end's local store (CLI JSON files, a future Flutter client's local Hive database) is not replaced by the API once account/sync support is wired up. The local store remains the authoritative, offline-capable copy for that device; sync pushes local changes to the account-scoped API/Postgres store and pulls other devices' changes back down into the local store, rather than the local store becoming a mere cache of API responses. See [ARCHITECTURE_DATA.md](ARCHITECTURE_DATA.md) for the local-store-as-source-of-truth principle this follows from.
- `PriorityTaskManager.API` exposes minimal authenticated REST endpoints wrapping existing core operations (`/api/tasks`, `/api/lists`, `/api/events`), each mapped in its own `Map*Endpoints` extension (`Tasks/TaskEndpoints.cs`, `Lists/ListEndpoints.cs`, `Events/EventEndpoints.cs`) that call only `TaskManagerService` methods; request/response DTOs live alongside each endpoint file and never expose scheduler-computed fields (e.g. `UrgencyScore`, `ScheduledParts`) as writable input.
- 2FA and OAuth/social login are explicitly out of MVP scope for the account model and are tracked as a separate future-vision follow-up; do not block account-scoped persistence work on them.
- `PriorityTaskManager.API` also hosts a second, unauthenticated route group (`Local/LocalScheduleEndpoints.cs`, `POST /api/local/schedule`) that exists purely so the offline Flutter client can run the real scheduling strategies (`GoldPanningStrategy`/`ConstraintOptimizationStrategy`) without an account or login, per the MVP requirement that the Flutter client reach full scheduling parity with the CLI while staying offline (see [VISION.md](VISION.md)). This endpoint is stateless: it builds tasks/events/profile from the request body, computes the schedule and slack metrics in-memory, and returns them without touching Postgres or any persisted store. It is gated by an explicit `LocalOnly` startup flag (`Program.cs`) so the same API project can run in either "local sidecar" mode (no Postgres/JWT wiring, `/api/local/*` only) or normal multi-tenant "cloud" mode (`/api/tasks`, `/api/lists`, `/api/events`, auth) depending on how it's launched — never both purposes conflated in one request path.
- The Flutter desktop client (`PriorityTaskManager.Flutter/lib/data/local_sidecar.dart`) launches this API project as a local child process (`dotnet run`) on demand and polls it until reachable, then posts its Hive-derived tasks through `ApiScheduleRepository` to `/api/local/schedule`. Hive remains the sole source of truth for task/list data; the sidecar computes but never stores anything. This is a dev-time mechanism (launches from source) — bundling a published, self-contained sidecar executable for distributable builds is separate follow-up work. The web build target has no sidecar and is intentionally online-only for this feature.

## Relationships With Existing Areas

| Area | Relationship |
| --- | --- |
| Core business logic | Owns approved task/list/event mutations and shared validation |
| Data and persistence | Owns persisted state shape and imported source metadata once approved |
| CLI and user interaction | May provide a local review flow, but should not own provider logic |
| Scheduling | Consumes approved tasks/events after persistence; should not call providers |

## Invariants

- No integration should silently overwrite user-edited tasks or events.
- Imported or generated records should not bypass core validation.
- External fetch/extraction failures should produce actionable diagnostics and should not corrupt existing local data.
- Provider-specific assumptions should not leak into scheduler or persistence internals.