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
- `PriorityTaskManager.API` is multi-tenant: every persisted task/list/event/profile document is scoped by account (`Account` model, see [ARCHITECTURE_DATA.md](ARCHITECTURE_DATA.md)). Requests authenticate with a JWT bearer token (issued by `POST /api/auth/register` / `POST /api/auth/login`) whose claims resolve to an account id; the API builds the core service graph (`ServiceComposer.Compose`) per request scoped to that account rather than sharing one service graph across all accounts. Front ends that are inherently single-user/local (the CLI) have no account concept today and are unaffected. **Implemented** (2026-08-30, issue #44 core): the Flutter client now has real login/register screens (`lib/ui/auth/login_screen.dart`, `register_screen.dart`), a JWT persisted via `flutter_secure_storage` (`lib/data/secure_token_store.dart`), and a Guest-vs-Authenticated session state machine (`lib/providers/session_provider.dart`, `SessionController extends AsyncNotifier<SessionState>`) driving a guest-first entry flow: a one-time `EntryChoiceScreen` offers "Continue as Guest" or "Log in / Create account", defaulting to Guest and never forcing a login; an expired token silently downgrades to Guest rather than re-prompting. A Guest can still deliberately opt into logging in later via an account control in the Left Rail. This wires the client to authenticate against the API, but an API-backed repository for task/list/event CRUD (replacing the local Hive store) does not exist yet, and migrating a Guest's local Hive data into a newly-created/logged-into account is deliberately out of scope here, tracked separately by issue #46 (targeted at V1). CLI authentication remains deferred and out of scope for MVP; see #49 for the open question of the CLI's networked future.
- Each front end's local store (CLI JSON files, a future Flutter client's local Hive database) is not replaced by the API once account/sync support is wired up. The local store remains the authoritative, offline-capable copy for that device's task/list/event CRUD data; sync pushes local changes to the account-scoped API/Postgres store and pulls other devices' changes back down into the local store, rather than the local store becoming a mere cache of API responses. See [ARCHITECTURE_DATA.md](ARCHITECTURE_DATA.md) for the local-store-as-source-of-truth principle this follows from. Scheduling itself is the one capability excluded from this local-first pattern (see "Online-Only Scheduling And Subscription Gating" below): the local store holds task/list/event data offline, but computing a schedule always requires a live call to the networked API.
- `PriorityTaskManager.API` exposes minimal authenticated REST endpoints wrapping existing core operations (`/api/tasks`, `/api/lists`, `/api/events`), each mapped in its own `Map*Endpoints` extension (`Tasks/TaskEndpoints.cs`, `Lists/ListEndpoints.cs`, `Events/EventEndpoints.cs`) that call only `TaskManagerService` methods; request/response DTOs live alongside each endpoint file and never expose scheduler-computed fields (e.g. `UrgencyScore`, `ScheduledParts`) as writable input.
- 2FA and OAuth/social login are explicitly out of MVP scope for the account model and are tracked as a separate future-vision follow-up; do not block account-scoped persistence work on them.
- The Flutter client's UI is a single "Three-Pane Command Center" (`lib/ui/command_center/`): `CommandCenterScreen` composes a persistent Left Rail, a scrollable Center Stage, and a Right Inspector, and owns the responsive layout policy (docked panes on wide layouts, `Drawer`/`endDrawer` overlays on narrow ones, plus manual drag-to-collapse/dock independent of window size). Selection state that drives what the Right Inspector shows (`InspectorKind`/`InspectorTarget`) lives in a dedicated Riverpod provider (`selection_provider.dart`) rather than being owned by any one pane, so the Left Rail, Center Stage cards, and Right Inspector can all set or react to the current selection without coupling to each other directly. New Right Inspector content (a new entity type to inspect/edit) should be added as another `InspectorKind` plus a corresponding `inspector_forms/*_inspector_form.dart` file rather than branching pane layout logic in `CommandCenterScreen`.

## Online-Only Scheduling And Subscription Gating

Scheduling (`GoldPanningStrategy`/`ConstraintOptimizationStrategy`) is an online-exclusive, subscription-gated capability served only through the authenticated, account-scoped API (issue #41, implemented 2026-08-30): the unauthenticated `/api/local/schedule` route and the `LocalOnly` startup flag have been removed; there is no client-spawned sidecar process and no client-side scheduling implementation. Every client — CLI, web, desktop, and future mobile — always calls the networked, authenticated `/api/schedule` route to compute a schedule. The Flutter client now routes exclusively through this authenticated route (`ApiScheduleRepository`, `authToken` is required, not optional) and only renders the computed Daily Column pipeline while the session is authenticated (`SessionStatus.authenticated`); Guests instead see `GuestTaskList`, a plain, user-sortable (Importance/Due Date/Alphabetical) task list, since they have no access to online-only features.

- Cross-device sync is likewise subscription-gated and served through the authenticated API.
- Task/list/event CRUD and LLM-assisted intake remain free and do not require a subscription; there are exactly two account tiers, Free and Subscription (see [VISION.md](VISION.md)), sitting above a default, no-account Guest mode that has no access to any online-only feature (scheduling, sync, LLM-assisted intake) and is never prompted to log in until the user explicitly chooses to create or log into an account (see #46 for guest-to-account data migration). Free-tier intake is protected by a lower usage quota (not a hard paywall) plus the same abuse-prevention request/traffic rate limiter applied to both tiers; Subscription gets a materially higher or unlimited quota.
- Subscription entitlement is enforced server-side, at the API, via the authenticated account's JWT claims: the scheduling and sync endpoints check the caller's entitlement before executing, the same way authentication is already checked today. No client-side license/entitlement check is used, since both gated features already require a server round-trip.
- Beta grace period (MVP only): new accounts default to Subscription-tier entitlement instead of Free, since there is no real payment processor yet (see [VISION.md](VISION.md), tracked by #50). This is a default-value change at registration time, not a change to the entitlement-check mechanism itself; existing beta accounts get downgraded to Free with an in-app notice once V1 ships with real payment integration. `AccountService.Register(email, password)` (the overload used by real self-service registration) grants the config-driven default tier instead of a hardcoded one; `PriorityTaskManager.API/Program.cs` reads `BetaGracePeriod:DefaultNewAccountsToSubscription` (`true` in `appsettings.json` for the MVP/beta period) and passes it into `AccountService`'s constructor. `POST /api/auth/register`/`POST /api/auth/login` responses carry a `BetaGracePeriodNotice` string (non-null only while the flag is on) for a future client to surface as an honest "this is a free beta preview" notice; the Flutter client's `RegisterScreen` now surfaces this via an `AlertDialog` on successful registration (issue #44 core). `DevAccountSeeder` and `AccountService`'s explicit-tier overload (`Register(email, password, tier)`) are unaffected by the flag, so dev-seeded Free/Subscription test accounts still work as before.
- Sync protocol (not yet implemented): each client's local store (Hive for Flutter) is expected to assign a client-generated `Guid` `Id` to items created offline, which the API would accept as the authoritative `Id` when that item is later synced (rather than generating a new server-side `Id`), so an item keeps one identity across offline creation and sync. Conflicts would be resolved last-write-wins per item; sync would push local changes to the API when connectivity returns and pull other devices' changes back down.
- Staleness UX (not yet implemented): the client would show a visible "not synced" indicator only while the local store has changes that have not yet been reconciled with the API; hidden once the client is in sync.

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