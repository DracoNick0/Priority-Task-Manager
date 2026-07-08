# Terminology

## Scope and Usage Rules
This document defines canonical project terminology and accepted aliases. Use these terms consistently across code comments and documentation.

Rules:
- Prefer canonical terms in all docs.
- Use aliases only when helpful for discoverability.
- Avoid forbidden synonyms that cause ambiguity.

## Canonical Terms Table
| Canonical Term | Definition | Accepted Aliases | Forbidden or Ambiguous Synonyms |
| --- | --- | --- | --- |
| Core | The PriorityTaskManager project containing business logic, models, services, and scheduling | Core library | Backend, engine layer |
| CLI | The PriorityTaskManager.CLI project for parsing commands and user interaction | Command-line interface | Frontend app |
| Strategy | A scheduling approach selected by scheduling mode | Scheduling strategy | Algorithm mode |
| Gold Panning | Current implemented staged scheduling strategy | Gold strategy | Agent pipeline |
| Constraint Solver | Planned optimization-based scheduling strategy | Solver | Implemented solver (unless actually implemented) |
| Stage | One processing step in the Gold Panning chain | Pipeline stage | Agent (for Gold Panning stage names) |
| Command Surface | Set of user-facing CLI commands | Command catalog | API surface |
| Invariant | A rule that must always hold true | Non-negotiable rule | Suggestion |
| Assumption | A condition currently treated as true and periodically re-verified | Working assumption | Invariant |
| Current State | Verified present behavior of the application | Current behavior | Future plan |
| Planned Work | Work intended but not yet delivered | Roadmap item | Current capability |

## Ambiguous Terms and Disambiguation Notes
- Agent:
  - Use only for explicit AI or agent-oriented architecture components.
  - Do not use as a synonym for Gold Panning stages.
- Mode:
  - Use for selecting strategy behavior such as Gold Panning or Constraint Solver.
  - Do not use for command groups.
- Pipeline:
  - Use for ordered stage execution flow.
  - Specify which strategy pipeline to avoid confusion.

## Naming Conventions for New Terms
- Use singular nouns for core concepts.
- Prefer domain words over implementation-specific labels.
- Include one-line definition on first introduction in a document.
- Add new recurring terms here before using them broadly.

## Deprecated Terms
| Deprecated Term | Replacement | Notes |
| --- | --- | --- |
| user defaults | defaults | Align with current command surface |
