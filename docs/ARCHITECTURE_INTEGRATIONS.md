# Integrations Architecture

This document describes the API and Flutter integration boundaries and the constraints for future provider integrations and intake. See [STATUS.md](STATUS.md) for current feature maturity, [WORKFLOW.md](WORKFLOW.md) for operational and deployment procedures, and the repository's GitHub Issues for planned work.

## Responsibilities

- Core owns domain operations, validation, and persistence coordination. Hosts should use Core services rather than duplicate those rules.
- The API provides authenticated network access and account-scoped persistence. Flutter owns presentation and selects the appropriate local or API-backed repository.
- External provider adapters, when introduced, should remain separate from client presentation, persistence internals, and scheduling algorithms.

## API And Client Boundaries

With Postgres configured, `PriorityTaskManager.API` exposes account, task, list, event, profile, archive, and schedule endpoints. Account operations use JWT authentication. For authenticated data requests, the account identity scopes Postgres persistence, and the API composes the Core service graph with that account's persistence service for the request. Task, list, event, profile, and archive operations delegate domain behavior to Core services; authentication and account management use account services, and schedule computation follows a separate path.

`POST /api/schedule` requires an authenticated account with the `Subscription` tier. It accepts the caller's tasks, events, profile settings, and optional simulated time, then computes and returns a schedule without reading or writing account persistence. The Flutter client calls this endpoint to obtain schedules; it does not implement scheduling itself. The active strategy is Gold Panning; Constraint Optimization remains unimplemented (see [ARCHITECTURE_SCHEDULING.md](ARCHITECTURE_SCHEDULING.md)). Guests do not use online scheduling.

The Flutter client selects a Hive-backed repository in Guest mode and an API-backed repository for authenticated sessions. Guest data is local and is not automatically migrated to an account or synchronized with account data. The API and Guest stores also differ in event scoping; see [ARCHITECTURE_DATA.md](ARCHITECTURE_DATA.md) before changing persistence or sync behavior.

For a .NET host that invokes Core, use `ServiceComposer` with the host's `IPersistenceService` rather than independently wiring the shared Core service graph. Keep host-specific transport, authentication, and presentation concerns outside Core; see [ARCHITECTURE_CORE.md](ARCHITECTURE_CORE.md).

## Future Provider And Intake Boundaries

External source providers and intake flows should follow these boundaries:

- Put provider-specific access behind source-specific abstractions; keep provider credentials and transport concerns out of scheduling and persistence code.
- Normalize fetched or generated content into candidates before persistence. Require user review before creating or changing persisted tasks, lists, or events.
- Persist approved candidates through Core services so shared validation applies. Preserve source attribution and do not silently overwrite user-edited data.
- Keep review UI focused on presenting candidates and collecting decisions. Surface provider failures with actionable diagnostics and avoid corrupting existing data.
- Keep provider resilience controls, such as request limits and retries, separate from tier-based usage quotas. The API's current rate limiter applies to login and registration, not provider calls.

Candidate models, tier-specific candidate storage, and intake quotas are planned rather than current API/client behavior; see [LLM_ASSISTED_INTAKE.md](LLM_ASSISTED_INTAKE.md) for that design. Any future sync design must define reconciliation between local and server data rather than treating Guest data as a disposable cache; see [ARCHITECTURE_DATA.md](ARCHITECTURE_DATA.md).
