applyTo: '**'

# Copilot Instructions for Priority Task Manager

## Start Here
- Before coding, review `docs/STATUS.md` and the architecture guidance relevant to the task.
- Start architecture review with `docs/ARCHITECTURE.md`, then read the focused `docs/ARCHITECTURE_*.md` document for the area being changed.
- For backlog or active-work context, use the repository's GitHub Issues as the source of truth.
- For documentation ownership, use `docs/DOC_INDEX.md`.
- If a request conflicts with documented architecture or current status, call it out and propose an aligned path.

## Architecture Reading
- Use `docs/ARCHITECTURE.md` as the architecture map and shared-boundaries reference.
- Read `docs/ARCHITECTURE_CLI.md` only when the user explicitly requests restoring the archived CLI.
- Read `docs/ARCHITECTURE_CORE.md` before changing task, list, profile, event, dependency, or service coordination behavior.
- Read `docs/ARCHITECTURE_DATA.md` before changing models, JSON persistence, IDs, list-scoped settings, or persisted data shape.
- Read `docs/ARCHITECTURE_SCHEDULING.md` before changing prioritization, Gold Panning stages, scheduling invariants, strategy selection, or scheduler tests.
- Read `docs/ARCHITECTURE_INTEGRATIONS.md` before adding APIs, provider integrations, external intake, import flows, or new front-end boundaries.

## Documentation Boundaries & Standards
- All project documentation must follow `docs/DOCUMENTATION_STANDARDS.md` and respect the canonical owner document defined in `docs/DOC_INDEX.md`.
- **Status is user-observable only (`docs/STATUS.md`)**: Describe only what users can observe or do. Never put internal class/interface names, DTOs, method signatures, issue tracking numbers (`#123`), or refactoring narratives in `docs/STATUS.md`.
- **No backlog in documentation**: Never maintain to-do lists, task checklists, or overhaul roadmaps in documentation files. The repository's GitHub Issues is the single source of truth for active and planned work.
- **Strict anti-duplication**: Do not copy sections across documents. Update only the canonical document owner and link to it from other files.
- **Preserve document scopes**:
  - `docs/VISION.md`: Long-term intent, core differentiators, and milestone goals only; not current status or architecture contracts.
  - `docs/PRODUCT_BEHAVIOR_SPECIFICATION.md`: Intended user-facing behavior, user stories, and acceptance criteria; not internal code details.
  - `docs/ARCHITECTURE.md` & `docs/ARCHITECTURE_*.md`: System boundaries, service composition, data ownership, and invariants; not transient status or step-by-step shell commands.
  - `docs/WORKFLOW.md`: How to build, run, test, and deploy; keep step-by-step developer and deployment procedures here.
  - `docs/TESTING_STRATEGY.md`: Durable testing philosophy and active test surfaces; not temporary to-do lists.
- **Canonical terminology**: Strictly adhere to `docs/TERMINOLOGY.md`:
  - Distinguish *Guest mode* (local-only, unauthenticated, Hive) from *Free account* (authenticated, no subscription) from *Subscription tier* (authenticated entitlement for online scheduling).
  - Distinguish *Scheduling mode* (the setting) from *Strategy* (the algorithm implementation).
  - Distinguish *Global defaults* (profile-level) from *List-scoped settings* (per-list overrides).
- **Archived CLI exclusion**: `PriorityTaskManager.CLI/` is archived source. Never represent it as an active client integration in `docs/STATUS.md` or a routine test suite in `docs/TESTING_STRATEGY.md`.
- **Link integrity**: Always use relative Markdown links with valid targets. When linking within `docs/`, use relative links like `[STATUS.md](STATUS.md)`, not `docs/STATUS.md`.

## GitHub Issue Management
- Use the `gh` CLI (e.g. `gh issue view`, `gh issue edit`, `gh issue create`, `gh issue close`, `gh api graphql`) to read and edit issues; do not guess issue content or fabricate issue URLs.
- Every issue must belong to the "Priority Task Manager Roadmap" project and have a milestone assigned ((MVP) Minimum Viable Product, (V1) Online Daily Planner, (V2) Flexible Smart Planner, or Dream Product Future Issues). If an issue blocks or is a sub-issue of an issue in an earlier milestone, it generally belongs in that same milestone.
- Set parent/child and blocked-by/blocking relationships using GitHub's native issue-relationship fields (via `gh api graphql` mutations `addSubIssue`/`removeSubIssue` and `addBlockedBy`/`removeBlockedBy`, using each issue's GraphQL node `id`), not by only writing `#123` mentions in the body.
- Only restate a dependency or relationship in the issue body when the relationship itself needs explanation (why it blocks, what part is shared, scope boundaries); do not restate a plain "depends on #N" once the relationship is attached natively.
- When closing an issue as superseded/duplicated by another, add a closing comment explaining why and pointing to the superseding issue, and update any relationships/body text on related issues that referenced the closed issue.
- When splitting or resolving overlapping scope between issues, prefer asking before closing or substantially rescoping an issue if the intended resolution is ambiguous.

## Architecture Boundaries
- `PriorityTaskManager` contains core business logic, models, services, persistence, and scheduling. It must not depend on user-interface behavior.
- `PriorityTaskManager.CLI/` is preserved archived source, not an active product or maintenance target. Do not modify, build, test, or update CLI-specific documentation unless the user explicitly asks to restore it.
- `TaskManagerService` coordinates task/list/profile operations and delegates prioritization through `IUrgencyStrategy`.
- Scheduling and prioritization logic belongs under `PriorityTaskManager/Scheduling/**`.

## Deploying API Changes (Fly.io)
- There is no CI/CD for `PriorityTaskManager.API`. A merge to `main` does not redeploy the hosted instance (`tpm-api` on Fly.io); it keeps serving whichever build was last deployed with `flyctl deploy` until someone runs it again.
- After changing anything under `PriorityTaskManager.API/` or `PriorityTaskManager/` that the hosted API needs (new/changed endpoints, routes, contracts, persistence, auth), redeploy with `flyctl deploy -a tpm-api` from the repo root, or explicitly flag to the user that a redeploy is needed before the change is live.
- If the Flutter client reports unexpected 404s/errors against `https://tpm-api.fly.dev`, check `flyctl releases -a tpm-api` against the commit history of the affected endpoint before assuming a code defect — the hosted build may simply be stale.
- See `docs/WORKFLOW.md` ("Deploying the API (Fly.io)") for deploy commands and secrets handling.

## Archived CLI
- Keep `PriorityTaskManager.CLI/` and its test sources available, but outside routine development, solution builds, and test runs.
- Only restore the CLI after an explicit user request; then reconcile its project/test wiring and documentation with the current architecture before changing its code.

## Scheduling And Tests
- Treat documented scheduling invariants as correctness requirements, not as behavior to weaken when tests expose defects.
- Separate implementation defects from incorrect or outdated test expectations before changing tests.
- Use deterministic time through `ITimeService`/test seams for time-sensitive behavior.
- Follow `docs/TESTING_STRATEGY.md` for test scope and quality expectations.

## Code Quality
- Follow good coding practice; review the full file being changed and its existing conventions, and flag or apply revisions (e.g. deduplication, abstraction) where the code falls short.
- Prefer existing project patterns and small, focused changes.
- Use descriptive names.
- Add XML comments (`///`) to public members; use inline comments only for non-obvious reasoning.
- Validate changes with the narrowest relevant build or test command. If validation cannot be run or is already known to fail, state why and identify the blocking failure.
