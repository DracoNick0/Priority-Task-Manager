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
- Keep UI review flows thin; they should present candidates and call core approval/persistence operations, not implement candidate validation or persistence rules themselves (the same core/CLI boundary already enforced for scheduling and task/list/event operations).
- Keep LLM/provider configuration and guardrails separate from scheduling algorithms.
- Provider guardrails cover two distinct concerns, both owned by the provider-abstraction layer: request-level resilience (rate limits, retries, validation) and tier-based usage quota enforcement (Free vs. Subscription, per [LLM_ASSISTED_INTAKE.md](LLM_ASSISTED_INTAKE.md)). Build both together; they gate the same provider call path.
- Candidate persistence follows the same account-scoping mechanism as tasks/lists/events: Free-tier candidates persist client-local only (never sent to the API/Postgres store); Subscription-tier candidates persist server-side, scoped by account id. Extraction/quota checks always go through the API regardless of tier; only candidate persistence location differs by tier.
- Each additional front end (CLI, API, or future clients) should build core services the same way: construct the same concrete service graph (`TaskManagerService`, `IPersistenceService`, `ITimeService`, and related services) rather than re-deriving its own wiring conventions. When a new front end is introduced, extract shared construction steps into a common composition helper instead of duplicating `Program.cs`-style manual wiring per front end.
- 2FA and OAuth/social login are out of scope for the account model until a later milestone; do not block account-scoped persistence work on them.

## API Account And Service Model

- `PriorityTaskManager.API` is multi-tenant: every persisted task/list/event/profile document is scoped by account (`Account` model, see [ARCHITECTURE_DATA.md](ARCHITECTURE_DATA.md)). Requests authenticate with a JWT bearer token (issued by `POST /api/auth/register` / `POST /api/auth/login`) whose claims resolve to an account id; the API builds the core service graph (`ServiceComposer.Compose`) per request, scoped to that account, rather than sharing one service graph across all accounts.
- The CLI is inherently single-user/local, has no account concept, and is unaffected by the account model.
- `PriorityTaskManager.API` exposes minimal authenticated REST endpoints wrapping existing core operations (`/api/tasks`, `/api/lists`, `/api/events`, `/api/profile`, `/api/archive`), each mapped in its own `Map*Endpoints` extension (`Tasks/TaskEndpoints.cs`, `Lists/ListEndpoints.cs`, `Events/EventEndpoints.cs`, `Profile/ProfileEndpoints.cs`, `Archive/ArchiveEndpoints.cs`) that call only `TaskManagerService` methods. Request/response DTOs live alongside each endpoint file and never expose scheduler-computed fields (e.g. `UrgencyScore`, `ScheduledParts`) as writable input.
- The core `Event` model (`PriorityTaskManager/Models/Event.cs`) has no list association; `/api/events` returns and accepts events for the whole account, unlike the Flutter client's local `FixedEvent`, which is list-scoped. This is a known mismatch, not yet resolved.
- Each front end's local store (CLI JSON files, the Flutter client's local Hive database) is not replaced by the API once account/sync support is wired up. The local store remains the authoritative, offline-capable copy for that device's task/list/event CRUD data; sync pushes local changes to the account-scoped API/Postgres store and pulls other devices' changes back down, rather than the local store becoming a mere cache of API responses. See [ARCHITECTURE_DATA.md](ARCHITECTURE_DATA.md) for the local-store-as-source-of-truth principle this follows from. Scheduling is the one capability excluded from this local-first pattern (see "Online-Only Scheduling And Subscription Gating" below): the local store holds task/list/event data offline, but computing a schedule always requires a live call to the networked API.
- The Flutter client authenticates against the API through a Guest-vs-Authenticated session state machine (`lib/providers/session_provider.dart`, `SessionController extends AsyncNotifier<SessionState>`) and a guest-first entry flow: a one-time `EntryChoiceScreen` offers "Continue as Guest" or "Log in / Create account", defaulting to Guest and never forcing a login; an expired token silently downgrades to Guest rather than re-prompting. A Guest can opt into logging in later via an account control in the Left Rail. The JWT is persisted via `flutter_secure_storage` (`lib/data/secure_token_store.dart`); login/register screens live under `lib/ui/auth/`. `taskRepositoryProvider` (`lib/providers/task_providers.dart`) selects the active `TaskRepository` implementation based on session status: `LocalTaskRepository` (Hive) for Guests, `ApiTaskRepository` (`lib/data/api_task_repository.dart`, calls `/api/tasks`/`/api/lists`/`/api/events`/`/api/profile`) once Authenticated. Migrating a Guest's local Hive data into a newly-created/logged-into account is out of scope for this work. CLI authentication is deferred to a later milestone.
- Dev-only observability (issue #59, debug builds only): outgoing HTTP calls made by `ApiTaskRepository`/`ApiScheduleRepository`/`AuthRepository` and local (Guest) `LocalTaskRepository` domain mutations are recorded by wrapping the shared `http.Client`/`TaskRepository` seam (`lib/dev/logging_http_client.dart`, `lib/dev/logging_task_repository.dart`) rather than by adding logging calls inside each repository method. Both wrappers write to a shared in-memory ring buffer (`lib/dev/dev_log_sink.dart`), viewable via a `kDebugMode`-gated, resizable bottom-docked "Dev Log" panel (`lib/ui/dev/dev_log_panel.dart`, toggled from the Left Rail, styled after an IDE's docked terminal/debug console) rather than a separate full-screen route. Neither wrapper is constructed at all outside `kDebugMode`, so there is no logging overhead or data exposure in release builds. New data-layer classes that make outbound calls should be constructed with the shared `LoggingHttpClient` (when `kDebugMode`) the same way, rather than re-adding ad hoc logging.

## Online-Only Scheduling And Subscription Gating

Scheduling (`GoldPanningStrategy`/`ConstraintOptimizationStrategy`) is an online-exclusive, subscription-gated capability served only through the authenticated, account-scoped `/api/schedule` route. There is no unauthenticated schedule route, no client-spawned sidecar process, and no client-side scheduling implementation. Every client — CLI, web, desktop, and future mobile — always calls this networked, authenticated route to compute a schedule. The Flutter client (`ApiScheduleRepository`, `authToken` required) only renders the computed Daily Column pipeline while the session is authenticated; Guests instead see `GuestTaskList`, a plain, user-sortable (Importance/Due Date/Alphabetical) task list, since they have no access to online-only features.

- Cross-device sync is likewise subscription-gated and served through the authenticated API.
- Task/list/event CRUD and LLM-assisted intake remain free and do not require a subscription. There are exactly two account tiers, Free and Subscription (see [VISION.md](VISION.md)), sitting above a default, no-account Guest mode that has no access to any online-only feature (scheduling, sync, LLM-assisted intake) and is never prompted to log in until the user explicitly chooses to create or log into an account. Free-tier intake is protected by a lower usage quota (not a hard paywall) plus the same abuse-prevention request/traffic rate limiter applied to both tiers; Subscription gets a materially higher or unlimited quota.
- Subscription entitlement is enforced server-side, at the API, via the authenticated account's JWT claims: the scheduling and sync endpoints check the caller's entitlement before executing, the same way authentication is already checked. No client-side license/entitlement check is used, since both gated features already require a server round-trip.
- Beta grace period (current MVP/beta behavior): new accounts default to Subscription-tier entitlement instead of Free, since there is no real payment processor yet (see [VISION.md](VISION.md)). This is a default-value change at registration time, not a change to the entitlement-check mechanism itself; existing beta accounts will be downgraded to Free with an in-app notice once real payment integration ships. `AccountService.Register(email, password)` (the self-service registration overload) grants a config-driven default tier read from `BetaGracePeriod:DefaultNewAccountsToSubscription`; `POST /api/auth/register`/`POST /api/auth/login` responses carry a `BetaGracePeriodNotice` string (non-null only while the flag is on), which the Flutter `RegisterScreen` surfaces via an `AlertDialog`. `DevAccountSeeder` and `AccountService`'s explicit-tier overload (`Register(email, password, tier)`) are unaffected by the flag.
- Sync protocol (not yet implemented): each client's local store (Hive for Flutter) is expected to assign a client-generated `Guid` `Id` to items created offline, which the API would accept as the authoritative `Id` when that item is later synced (rather than generating a new server-side `Id`), so an item keeps one identity across offline creation and sync. Conflicts would be resolved last-write-wins per item; sync would push local changes to the API when connectivity returns and pull other devices' changes back down.
- Staleness UX (not yet implemented): the client would show a visible "not synced" indicator only while the local store has changes that have not yet been reconciled with the API; hidden once the client is in sync.

## Hosting And Deployment

`PriorityTaskManager.API` is containerized and hosted as a real, always-on, shared instance rather than a per-developer local process, so scheduling works for any downloaded client, not just next to a developer's own machine. It is deployed on [Fly.io](https://fly.io) (app `tpm-api`, region `sjc`, reachable at `https://tpm-api.fly.dev` with automatic HTTPS) backed by a managed Fly Postgres cluster. Production secrets (JWT signing key, connection string) are managed through Fly's secrets store and are never committed.

Deployment is manual, not CI/CD-triggered: the hosted instance only reflects a change under `PriorityTaskManager.API/` or `PriorityTaskManager/` once someone runs `flyctl deploy` after merging it. A merged API change (new endpoint, contract change, etc.) can silently 404 or behave stale against `https://tpm-api.fly.dev` until redeployed.

The Flutter client's API base URL is compile-time configurable (`ApiScheduleRepository.defaultBaseUri`), defaulting to a local API for development and switchable to the hosted production URL for testing against the real instance.

For container build steps, local Docker Compose usage, and deployment/secret-configuration commands, see [WORKFLOW.md](WORKFLOW.md).

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