# RFC: Solver-Centered Scheduling Migration

## 1. Purpose
Define a documentation-first path to evolve the scheduling system toward a solver-centered architecture while keeping the current system operational.

This RFC focuses on planning artifacts, contracts, and migration governance before implementation.

## 2. Target Architecture (Summary)
The long-term architecture combines:
- Policy and orchestration layer (preferences, validation, fallback rules).
- Solver core (constraints + objective optimization).
- Explainability layer (why each scheduling decision happened).

The existing MCP pipeline can remain as a fallback planner during migration.

V1 execution baseline:
- Use the reduced 6-stage pipeline defined in SCHEDULING_SYSTEM_SPEC.md.
- Keep in-day sequencing and cross-day balancing as internal OptimizationPlanner sub-passes.
- Enforce single-owner stage boundaries to prevent responsibility overlap.

## 3. Migration Principles
1. No big-bang rewrite.
2. Maintain feature parity checkpoints.
3. Keep outputs explainable.
4. Preserve deterministic behavior for equal inputs and settings.
5. Prefer additive schema changes first, destructive changes last.

## 4. Documentation Roadmap

### Phase 0: Decision and Scope Lock
Deliverables:
1. Confirm V1 policy baseline in [docs/SCHEDULING_DISCUSSION_NOTES.md](docs/SCHEDULING_DISCUSSION_NOTES.md).
2. Record non-goals and postponed concerns (for example local-minima global search).
3. Define migration success metrics and constraints.

Definition of Done:
- Baseline decisions are explicit and approved.
- No unresolved contradictions across docs.

### Phase 1: Contracts and Domain Vocabulary
Deliverables:
1. Add planner interface contract in architecture docs.
2. Define canonical scheduling request/response objects.
3. Define standard outcome categories:
- Scheduled.
- Scheduled overtime.
- Late scheduled.
- Dropped (non-must).
- Deferred/blocked.
4. Define explanation schema fields used by CLI output.
5. Define V1 stage ownership contract:
- Validation owner
- Window owner
- Decomposition owner
- Scoring owner
- Placement owner
- Explanation owner

Definition of Done:
- Any planner implementation can be validated against one shared contract spec.

### Phase 2: Constraint and Objective Specification
Deliverables:
1. Create formal constraint inventory:
- Hard constraints.
- Soft constraints.
2. Define objective terms and tie-break order.
3. Define SlackRiskPenalty behavior (deadline closeness before lateness).
4. Define SwitchingPenalty sources from schedule shape:
- transition count
- task fragmentation count
- tiny-chunk count
5. Define preference-to-constraint mapping table.
6. Define infeasibility and fallback semantics.

Definition of Done:
- Team can translate the spec into solver model code without policy ambiguity.

### Phase 3: Data Model and Persistence Migration Plan
Deliverables:
1. Identify new fields in user profile and task metadata.
2. Write backward-compatibility and defaulting rules.
3. Define data versioning and migration strategy for JSON files.
4. Define explicit overtime block representation format.

Definition of Done:
- Existing user data can load without manual edits.
- New fields have safe defaults.

### Phase 4: Rollout and Safety Plan
Deliverables:
1. Shadow-mode plan: run old and new planners side by side for comparison.
2. Feature-flag rollout strategy.
3. Regression test matrix for parity and policy correctness.
4. Performance budget and timeout/fallback policy.

Definition of Done:
- Clear go/no-go checklist for making solver planner the default.

## 5. Required New or Updated Documents
1. New: Solver migration RFC (this document).
2. Update: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) with planner abstraction and component boundaries.
3. Update: [docs/STATUS.md](docs/STATUS.md) with migration status section.
4. Update: [docs/TODO.md](docs/TODO.md) with phased doc tasks and implementation gates.
5. Optional: Add a dedicated testing RFC appendix for parity and performance.

## 6. Suggested Work Breakdown (Docs First)
1. Write planner contract section in architecture doc.
2. Write constraint catalog and scoring spec (including slack and switching terms).
3. Write stage ownership contract and mutation boundaries.
4. Write data schema migration notes and compatibility table.
5. Write rollout policy and acceptance checklist.

## 7. Risks to Address in Documentation
1. Policy drift between docs and implementation.
2. Hidden assumptions in heuristic behavior not carried to solver spec.
3. User trust regression if explanations are weaker than current logs.
4. Performance regressions at larger planning horizons.

## 8. Acceptance Criteria for Documentation Readiness
1. Every user preference has a documented runtime effect.
2. Every scheduling output state has a documented reason code.
3. Every hard constraint has at least one test scenario.
4. Fallback behavior is deterministic and documented.
5. Migration can proceed incrementally without downtime for existing users.
6. V1 reduced pipeline and stage ownership boundaries are explicitly documented.
7. Slack-risk and switching-penalty terms are defined with concrete computation sources.

## 9. Pre-Implementation Checklist
Use this checklist before coding migration tasks:
1. Architecture contract approved:
- Planner request/response shapes are locked.
- Stage ownership boundaries are locked.
2. Policy contract approved:
- Hard constraints and soft constraints are locked.
- Slack and switching penalty definitions are locked.
3. Data contract approved:
- New fields, defaults, and versioning rules are documented.
- Overtime representation is documented.
4. Rollout contract approved:
- Feature flag behavior is documented.
- Shadow-mode comparison metrics are documented.
- Go/no-go promotion gates are documented.
