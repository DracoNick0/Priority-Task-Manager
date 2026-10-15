# Terminology

## Scope and Usage Rules
This document defines canonical project terminology and accepted aliases. Use these terms consistently across code comments and documentation.

Rules:
- Describe active behavior unless a term is explicitly marked Archived or Planned.
- Prefer canonical terms in all docs.
- Use aliases only when helpful for discoverability.
- Avoid forbidden synonyms that cause ambiguity.

## Canonical Terms Table
| Canonical Term | Definition | Accepted Aliases | Forbidden or Ambiguous Synonyms |
| --- | --- | --- | --- |
| Core | The active `PriorityTaskManager/` library containing domain models, business logic, persistence, and scheduling | Core library | Backend, engine layer |
| API | The `PriorityTaskManager.API/` REST host exposing networked account and core operations | REST API | Core library, client UI |
| Flutter client | The active web and Windows desktop user interface; it uses local storage for Guests and API-backed repositories for authenticated sessions | Flutter app | API host, CLI |
| CLI | The archived `PriorityTaskManager.CLI/` source project, retained for reference but not an active product surface or routine build/test target | Archived CLI | Active client, supported CLI |
| Strategy | A scheduling approach selected by the effective scheduling mode | Scheduling strategy | Algorithm mode |
| Scheduling mode | A profile or list setting that selects a scheduling strategy; it is not the strategy itself | `SchedulingMode` | Strategy |
| Gold Panning | The active, implemented scheduling strategy, executed as a fixed pipeline of stages | Gold Panning strategy | Gold strategy, agent pipeline |
| Constraint Solver | The planned optimization-based scheduling strategy; its mode is routed to a placeholder and the solver is not implemented | Constraint-optimization strategy | Implemented solver |
| Stage | One processing step in a scheduling pipeline; the active Gold Panning implementation names its steps as stages | Pipeline stage | Agent (for Gold Panning steps) |
| Pipeline | An ordered execution flow of stages or components within a named strategy | Stage chain | Strategy (as a synonym) |
| Event | A time commitment treated as unavailable work time by scheduling | Calendar event | Task |
| Guest | An unauthenticated, local-data session with no active signed-in account; Guest mode is distinct from a Free account | Guest mode | Free account |
| Authenticated session | A session associated with a signed-in account; authentication alone does not grant Subscription-tier capabilities | Signed-in session | Subscription session |
| Subscription tier | An account entitlement, currently `Free` or `Subscription`; online scheduling requires `Subscription` independently of authentication, and the tier alone does not prove a payment transaction | Account tier | Authentication state |
| Global defaults | Profile-level settings used when a list setting is unset and copied into new lists; stored list settings can diverge from later global changes | Profile defaults | User defaults |
| List-scoped settings | Settings stored for one list; set values take precedence over global defaults, while unset values use the profile default | List settings, per-list overrides | Global defaults (for list-specific values) |
| Recurring series | A persisted task or event definition with a recurrence rule; dates are expanded on demand, while per-occurrence state, exceptions, and overrides are stored separately | Series (when context is clear) | Occurrence |
| Recurrence rule | A pattern and end condition defining the dates in a recurring series | Recurrence pattern | Occurrence list |
| Occurrence | One dated instance expanded from a recurring series; task state is keyed by the original recurrence date, and projected task occurrences are not separate persisted tasks | Task occurrence, event occurrence | Separate task record (for a projected task occurrence) |
| Cancellation exception | A per-series record that excludes one original recurrence date | Occurrence cancellation | Occurrence override |
| Occurrence override | Edited values for one occurrence, keyed by its original recurrence date; it may change the displayed fields without cancelling the occurrence or changing the series rule | Per-occurrence edit | Cancellation exception |
| Invariant | A rule that must always hold true | Non-negotiable rule | Suggestion |
| Assumption | A condition currently treated as true and periodically re-verified | Working assumption | Invariant |
| Current State | Verified present behavior of the application | Current behavior | Future plan |
| Planned Work | Work intended but not yet delivered | Roadmap item | Current capability |

## Ambiguous Terms and Disambiguation Notes
- Agent:
  - Use only for explicit AI or agent-oriented architecture components, such as the planned Constraint Solver roles.
  - Do not use as a synonym for stages in the active Gold Panning pipeline.
- Mode:
  - Use `scheduling mode` for the profile/list setting that selects a strategy such as Gold Panning or Constraint Solver.
  - Do not use `mode` as a synonym for the strategy itself.
- Pipeline:
  - Use for ordered execution within a strategy.
  - Name the strategy's pipeline when context is not clear.
- Defaults:
  - Use `global defaults` for profile-level values and `list-scoped settings` for per-list values.
  - Do not use `user defaults` when referring to profile-level defaults.
- Recurrence:
  - Use `cancellation exception` for a date excluded from a series and `occurrence override` for edits to one date.
  - Keep each operation keyed to the original recurrence date, even when an occurrence is moved.

## Naming Conventions for New Terms
- Use singular nouns for core concepts.
- Prefer domain words over implementation-specific labels.
- Include one-line definition on first introduction in a document.
- Add new recurring terms here before using them broadly.

## Deprecated Terms
| Deprecated Term | Replacement | Notes |
| --- | --- | --- |
| user defaults | global defaults | Distinguish profile-level defaults from list-scoped settings |
