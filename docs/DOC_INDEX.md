# Documentation Index

## How to Use This Index
Use this file to find the canonical document for each topic. If a topic appears in more than one place, this index defines the source of truth.

## Core Documents
| Document | Purpose | Audience |
| --- | --- | --- |
| README.md | Entry point and quick orientation | New users, maintainers, AI agents |
| docs/ARCHITECTURE.md | System design, boundaries, and interactions | Developers and refactors |
| docs/STATUS.md | Current state of working, partial, broken, in progress | Maintainer and contributors |

## Supporting Documents
| Document | Purpose |
| --- | --- |
| docs/WORKFLOW.md | Contribution and delivery process |
| docs/TESTING_STRATEGY.md | Testing approach and quality expectations |
| docs/TODO.md | Backlog and planned work |
| docs/GOLD_PANNING.md | Gold Panning algorithm details |
| docs/CONSTRAINT_SOLVER.md | Constraint Solver specification and contract |
| docs/COMPLEXITY_GUIDE.md | Complexity model reference |

## Long-Lived Knowledge Documents
| Document | Purpose |
| --- | --- |
| docs/DOCUMENTATION_STANDARDS.md | Documentation writing and maintenance rules |
| docs/TERMINOLOGY.md | Canonical terminology, aliases, disambiguation |

## Source-of-Truth Map
| Topic | Source of Truth |
| --- | --- |
| Onboarding and project overview | README.md |
| Architecture boundaries and component responsibilities | docs/ARCHITECTURE.md |
| Current feature reality | docs/STATUS.md |
| Command reference | docs/STATUS.md |
| Planned work and roadmap | docs/TODO.md |
| Scheduling algorithm details | docs/GOLD_PANNING.md, docs/CONSTRAINT_SOLVER.md |
| Testing philosophy | docs/TESTING_STRATEGY.md |
| Shared vocabulary | docs/TERMINOLOGY.md |
| Documentation process and rules | docs/DOCUMENTATION_STANDARDS.md |

## Reading Paths by Persona
### New user
1. README.md
2. docs/STATUS.md
3. docs/WORKFLOW.md

### Maintainer
1. docs/STATUS.md
2. docs/ARCHITECTURE.md
3. docs/TERMINOLOGY.md

### AI coding agent
1. docs/DOCUMENTATION_STANDARDS.md
2. docs/TERMINOLOGY.md
3. docs/STATUS.md
4. docs/ARCHITECTURE.md

## Maintenance Notes
- Review this index whenever a document is added, removed, renamed, or repurposed.
- Confirm every recurring topic has exactly one canonical owner.
- Keep links and ownership map synchronized with repository changes.
