# RFC: Dual-Scheduler Strategy & Migration

## 1. Purpose
Define a documentation-first path to introduce a new solver-centered scheduling system alongside the existing Multi-Agent (MCP) system.

This RFC focuses on the "Strategy Pattern" implementation, allowing users to switch between the Legacy (current) and V1 (new) schedulers at runtime.

## 2. Target Architecture (Dual-Mode)
The system will support two distinct scheduling strategies implementing a common `IUrgencyStrategy` interface:

1.  **Legacy Strategy (`MultiAgentUrgencyStrategy`)**:
    -   The current production implementation.
    -   Preserved as-is (naming may be updated to `LegacyMcpStrategy`).
    -   Default for existing users until V1 is stable.

2.  **V1 Strategy (`OptimizationSchedulingStrategy`)**:
    -   The new solver-based implementation.
    -   Follows the `SCHEDULING_SYSTEM_SPEC.md`.

## 3. Migration Principles
1.  **Co-existence**: The new scheduler is additive. No legacy code is deleted until V2+ (deprecation phase).
2.  **User Choice**: Users select their preferred scheduler via `UserProfile.SchedulingMode`.
3.  **Safe Fallback**: If V1 fails or is incomplete, the user can instantly switch back to Legacy.
4.  **Deterministic**: Both strategies must be deterministic given the same inputs.

## 4. Work Breakdown

### Phase 1: Strategy Abstraction & Routing
1.  Extract/Confirm `IUrgencyStrategy` interface.
2.  Add `SchedulingMode` enum to `UserProfile` (Values: `Legacy`, `V1Optimization`).
3.  Update `TaskManagerService` to instantiate the correct strategy based on the user's profile.
4.  Preserve all existing unit tests 100%.

### Phase 2: V1 Implementation (Optimization Planner)
1.  Implement the V1 pipeline as defined in `SCHEDULING_SYSTEM_SPEC.md`.
2.  Isolate V1 logic in a new namespace/directory to avoid polluting legacy code.
3.  Add new unit tests specifically for V1 behavior.

### Phase 3: Gradual Rollout
1.  Expose the `SchedulingMode` setting in the CLI (`settings` command).
2.  Mark V1 as "Experimental" or "Preview" in the UI.
3.  Collect feedback while keeping Legacy as the safe default.

## 5. Documentation Roadmap

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
1. Branch-strategy rollout plan (no in-branch legacy fallback requirement).
2. Regression test matrix for policy correctness.
3. Performance budget and timeout/fallback policy.
4. Merge promotion gates from migration branch to shared branches.
5. Terminology migration checklist for renaming legacy MCP labels after V1 behavior stabilizes.

Definition of Done:
- Clear go/no-go checklist for making solver planner the default.

### Phase 5: Terminology and Naming Cleanup (Post-V1 Stabilization)
Deliverables:
1. Rename scheduling-facing MCP names to pipeline-oriented names in core and CLI layers.
2. Update documentation and command/help text to match finalized terminology.
3. Keep temporary compatibility aliases only where needed to avoid unnecessary breakage.

Definition of Done:
- Naming reflects the implemented architecture without relying on historical context.
- Renaming introduces no behavior changes.

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

## 6.1 Data Compatibility Table (V1)
| Field | Location | Default (if missing) | Backward-Compatibility Rule |
| :--- | :--- | :--- | :--- |
| `AllowMustScheduleLateness` | User preferences/profile | `true` | Missing field is treated as enabled for V1 migration behavior. |
| `AllowMustScheduleOvertime` | User preferences/profile | `true` | Missing field is treated as enabled to preserve must-task completion attempts. |
| `OvertimeScope` | User preferences/profile | `MustOnly` | Missing field defaults to safer scope to avoid broad overtime expansion. |
| `AllowNonMustLateness` | User preferences/profile | `false` | Missing field defaults to stricter policy for non-must tasks. |
| `PlanningHorizonMode` | User preferences/profile | `Fixed` | Missing field uses fixed horizon behavior. |
| `FixedHorizonDays` | User preferences/profile | `21` | Missing field uses 21-day baseline. |
| `AdaptiveMinDays` | User preferences/profile | `14` | Missing field uses lower guardrail default. |
| `AdaptiveMaxDays` | User preferences/profile | `90` | Missing field uses upper guardrail default. |
| `MinChunkSizeDefault` | User preferences/profile | `00:30:00` | Missing field uses 30-minute chunk minimum. |
| `EnableRecoveryBuffers` | User preferences/profile | `false` | Missing field keeps recovery buffers opt-in. |

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
- Branch-strategy rollout behavior is documented.
- Merge promotion metrics and gates are documented.
- Go/no-go promotion gates are documented.

## 10. TDD Policy (Required)
Migration implementation follows test-driven development.

Required workflow per feature slice:
1. Red: add or update failing tests from the scheduling contract first.
2. Green: implement the smallest change needed to pass.
3. Refactor: improve structure without changing behavior.

Minimum required first-test set:
1. FS dependency correctness.
2. Must-schedule overload behavior (late and overtime policy).
3. Non-must unscheduled behavior (excluded in current run, re-evaluated in future runs).
4. Slack protection for high-importance tasks.
5. Deterministic replay for identical inputs.

Merge gate:
1. No migration implementation PR is considered complete without tests authored first and passing.
2. Any policy change requires corresponding test updates in the same PR.
