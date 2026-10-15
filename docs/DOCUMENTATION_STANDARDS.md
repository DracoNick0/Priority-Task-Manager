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

## Canonical Ownership
The canonical owner for every document and topic is defined in [DOC_INDEX.md](DOC_INDEX.md). If a topic is discussed in multiple documents, [DOC_INDEX.md](DOC_INDEX.md) governs which document is authoritative.

Every document belongs to one of three categories defined in [DOC_INDEX.md](DOC_INDEX.md):
- **Core Documents**: High-level orientation, vision, user behavior, architecture map, and current status.
- **Supporting Documents**: Deep-dives into subsystems (focused architecture, algorithms, integration, testing, workflow).
- **Long-Lived Knowledge Documents**: Stable rules, standards, and terminology.

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

### Vision
Required sections:
- Purpose
- Scope
- Problem Statement
- Target Users
- Core Differentiator
- Desired End State
- Milestones
- Success Definition
- Non-Goals
- How to Use This Document

Must not include:
- Current implementation status (owned by docs/STATUS.md)
- Architecture contracts (owned by docs/ARCHITECTURE.md and docs/ARCHITECTURE_*.md)
- Scheduled or in-progress work (owned by the repository's GitHub Issues)

### Product Behavior Specification
Required sections:
- Purpose and scope
- User outcomes or user stories
- Intended user-facing behavior
- Observable acceptance criteria
- Open product decisions, with unanswered behavior left unresolved rather than guessed

Must not include:
- Current implementation status owned by docs/STATUS.md
- Architecture or implementation contracts owned by docs/ARCHITECTURE.md and docs/ARCHITECTURE_*.md
- Backlog or active-work tracking owned by the repository's GitHub Issues

### Architecture
Required sections:
- Architecture Overview
- Focused Architecture Documents
- System Boundaries
- Runtime Data Flow
- Architectural Invariants
- Related Specifications
- Terminology links

Must not include:
- Roadmap items
- Feature matrix
- Full command reference

### Focused Architecture Documents
Required sections:
- Responsibilities
- Key entry points, services, models, or boundaries for the area
- Important dependencies and relationships with other areas
- Implementation guidance for where responsibilities should be added
- Invariants or constraints

Must not include:
- Current status reporting owned by docs/STATUS.md
- Backlog or active-work handoff owned by the repository's GitHub Issues
- Exhaustive method-level reference details

### Status
Required sections:
- Status Snapshot
- Feature Matrix (subsections: Core (C# backend), active client integrations)
- Confirmed Capabilities
- Known Limitations
- Known Issues and Technical Debt
- Validation Notes

The Feature Matrix separates what Core implements from what active clients expose; archived code must not be presented as a supported integration.

docs/STATUS.md is a user-facing document: describe what a user can observe or do (features, commands, behavior, limitations), not how it is implemented internally.

Must not include:
- Future plans
- Design debate notes
- Changelog narrative
- Internal implementation details (class names, contracts, interfaces, method names, migration/refactor state) — these belong in the matching docs/ARCHITECTURE_*.md document instead

## Writing Style and Formatting Rules
- Use clear, concrete language and short paragraphs.
- Keep section heading order stable once established.
- Prefer tables for lists of commands, statuses, mappings, and ownership.
- Use explicit labels: Current State, Planned Work, Assumption, Invariant.
- Do not maintain active work lists, task checklists, or sprint to-dos in documentation files; track active and planned work exclusively in the repository's GitHub Issues.
- Avoid unexplained acronyms and overloaded terms.

## Verification Requirements
- High-risk claims must be verifiable from code or canonical documents.
- Confirm archived CLI code is not represented as an active product surface or routine build/test target.
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
- Keep backlog and active-work handoff content only in the repository's GitHub Issues.
- Keep current-state truth only in docs/STATUS.md.
- Keep shared architecture routing in docs/ARCHITECTURE.md and focused architecture contracts in the matching docs/ARCHITECTURE_*.md file.

## Common Mistakes and How to Avoid Them
- Mixing roadmap plans or to-do lists into status or testing strategy:
Move planned items and task lists to the repository's GitHub Issues; keep documentation durable and current.
- Leaking internal implementation details into user status:
Keep docs/STATUS.md strictly user-facing (observable capabilities, behavior, and limitations). Class names, method signatures, DTOs, and issue numbers belong in docs/ARCHITECTURE_*.md or code.
- Treating archived components as active:
The CLI is archived source. Do not represent it as an active client integration in docs/STATUS.md or as a routine test target in docs/TESTING_STRATEGY.md.
- Placing operational deployment and run commands in architecture:
Keep step-by-step build, run, and deployment commands in docs/WORKFLOW.md; keep architectural boundaries and tenancy models in docs/ARCHITECTURE_*.md.
- Conflating session modes and subscription tiers:
Strictly use canonical terms from docs/TERMINOLOGY.md to distinguish Guest mode (local-only, unauthenticated, Hive), Free accounts (authenticated, no subscription), and Subscription tier (authenticated entitlement for online scheduling).
- Duplicating ownership matrices:
docs/DOC_INDEX.md is the sole source of truth for topic and document ownership. Link to it rather than recreating ownership tables.
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
