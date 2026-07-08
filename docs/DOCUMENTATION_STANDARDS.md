# Documentation Standards

## Purpose
Define durable rules for writing, updating, and verifying project documentation for dual audiences: maintainer and AI coding agents.

## Audience
- Primary: project maintainer & AI coding agents working in this repository
- Secondary: future human contributors

## Scope
This document defines documentation structure, ownership, style, verification, and maintenance process. It does not define project architecture or implementation details.

## Documentation Philosophy
- Optimize for both human readability and AI reliability.
- Keep each concept in exactly one canonical document.
- Prefer concise, factual writing over exhaustive narrative.
- Separate current behavior from future plans.
- Treat documentation as part of the product surface.

## Canonical Ownership Matrix
| Topic | Canonical Document | Notes |
| --- | --- | --- |
| Project entry and navigation | README.md | High-level orientation only |
| System design and boundaries | docs/ARCHITECTURE.md | Stable architecture concepts |
| Current behavior and maturity | docs/STATUS.md | Current state only |
| Contributor process | docs/WORKFLOW.md | Process and workflow only |
| Testing strategy | docs/TESTING_STRATEGY.md | Test philosophy and approach |
| Backlog and roadmap | docs/TODO.md | Planned work only |
| Gold Panning strategy details | docs/GOLD_PANNING.md | Algorithm-specific details |
| Constraint Solver spec | docs/CONSTRAINT_SOLVER.md | Specification and intended contract |
| Complexity scale semantics | docs/COMPLEXITY_GUIDE.md | Stable scoring reference |
| Documentation ownership map | docs/DOC_INDEX.md | Documentation navigation and scope map |
| Controlled vocabulary | docs/TERMINOLOGY.md | Canonical terms and aliases |

## Required Structure by Document Type
### README
Required sections:
- Project Summary
- Why This Project Exists
- Current Capabilities at a Glance
- Quick Start
- Repository Map
- Documentation Map
- Current Maturity Snapshot

Must not include:
- Full command catalog
- Deep architecture internals
- Long issue lists

### Architecture
Required sections:
- Architecture Overview
- System Boundaries
- Core Components
- Data Model Overview
- Data Flow
- Scheduling Strategy Architecture
- Extension Points
- Architectural Invariants Summary with link
- Terminology links

Must not include:
- Roadmap items
- Feature matrix
- Full command reference

### Status
Required sections:
- Status Snapshot
- Feature Matrix
- Confirmed Capabilities
- Known Limitations
- Known Issues and Technical Debt
- Command Surface Summary
- Validation Notes

Must not include:
- Future plans
- Design debate notes
- Changelog narrative

## Writing Style and Formatting Rules
- Use clear, concrete language and short paragraphs.
- Keep section heading order stable once established.
- Prefer tables for lists of commands, statuses, mappings, and ownership.
- Use explicit labels: Current State, Planned Work, Assumption, Invariant.
- Avoid unexplained acronyms and overloaded terms.

## Verification Requirements
- High-risk claims must be verifiable from code or canonical documents.
- Validate command surface against PriorityTaskManager.CLI/Program.cs.
- Validate strategy behavior claims against PriorityTaskManager/Services/TaskManagerService.cs and PriorityTaskManager/Scheduling/GoldPanning/GoldPanningStrategy.cs.
- Validate links and cross-references in every documentation update.

## Update Triggers
Update docs whenever any of the following changes occur:
- Command surface changes
- Architecture boundaries or component responsibilities change
- Scheduling strategy behavior changes
- Status of a feature changes from broken, partial, in progress, or working
- Testing approach or quality gates change
- Terminology changes or a new key concept is introduced

## Anti-Duplication Rules
- Do not copy full sections between documents.
- Link to canonical owner documents instead of restating details.
- Keep backlog content only in docs/TODO.md.
- Keep current-state truth only in docs/STATUS.md.
- Keep architecture contracts only in docs/ARCHITECTURE.md.

## Common Mistakes and How to Avoid Them
- Mixing roadmap plans into status:
Move planned items to docs/TODO.md and keep status strictly current.
- Mixing implementation notes into architecture:
Keep architecture at subsystem and contract level.
- Using ambiguous terms inconsistently:
Use docs/TERMINOLOGY.md as the source of truth.
- Leaving stale references after refactors:
Run link and ownership sweeps during doc updates.

## Documentation Review Checklist
- Does each section match this file's scope?
- Are claims traceable to code or canonical docs?
- Is any content duplicated from another canonical doc?
- Are all links valid?
- Are Current State and Planned Work clearly separated?
- If commands changed, was docs/STATUS.md updated?
- If architecture changed, was docs/ARCHITECTURE.md reviewed?

## Maintenance and Cadence
- Status updates happen continuously on meaningful behavior changes.
- Run a monthly documentation drift check:
  - Link validity
  - Ownership compliance
  - Terminology consistency
  - Status accuracy against code

## Lightweight Long-Lived Notes
If a concept is important but not substantial enough for its own document, capture it in docs/ARCHITECTURE.md or docs/WORKFLOW.md instead of creating a separate file.
