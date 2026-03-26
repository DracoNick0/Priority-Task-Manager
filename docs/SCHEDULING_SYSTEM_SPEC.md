# Scheduling System Specification

## 1. Purpose
This document defines the complete scheduling design for the **Constraint Solver** strategy.

It consolidates requirements, algorithm behavior, workflow stages, agent responsibilities, data contracts, preference effects, fallback behavior, and expected outputs.

This is the implementation source of truth for the `ConstraintOptimizationStrategy` class and its dependencies.

*Note: This specification does not apply to the `GoldPanning` (Legacy) strategy.*

## 2. Design Goals
1. Schedule as many tasks as possible before due dates.
2. Protect must-schedule tasks from being dropped.
3. Drop only non-must tasks when capacity is insufficient.
4. Respect dependency ordering (FS).
5. Front-load higher-cost tasks earlier in each day when feasible.
6. Balance cognitive load across days to reduce burnout spikes.
7. Preserve continuity by minimizing harmful context switching.
8. Produce explainable results for each scheduling decision.

## 3. Scope and Assumptions
1. Single-user local scheduler.
2. Single-threaded execution: one task chunk at a time.
3. Duration estimates are user-provided.
4. Importance and cost are both 1-10 scales.
5. Splittable tasks may span multiple days.
6. Non-splittable tasks require one continuous block.

## 4. Core Entities

### 4.1 Task
Required scheduling fields:
- Id
- Title
- Importance (1-10)
- Cost (1-10)
- EstimatedDuration
- DueDate (nullable)
- Dependencies (task IDs)
- IsMustSchedule
- IsDivisible
- IsCompleted

Optional policy fields:
- MinChunkSize
- MaxChunkCount
- MaxSplitDays
- BeforePadding
- AfterPadding

### 4.2 User Preferences
Required behavior-related fields:
- PlanningHorizonMode (Fixed or Adaptive)
- FixedHorizonDays (default 21)
- AdaptiveMinDays
- AdaptiveMaxDays
- AllowMustScheduleLateness
- AllowMustScheduleOvertime
- OvertimeScope (MustOnly or AllTasks)
- AllowNonMustLateness
- MinChunkSizeDefault
- EnableRecoveryBuffers
- RebalanceAggressiveness
- ContinuityPenaltyWeight

### 4.3 Time Model
- Work windows: generated from user workdays and work hours.
- Event blocks: remove availability.
- Overtime blocks: extra windows only if policy allows.

### 4.4 Scheduling Output
- Scheduled chunks (start and end times)
- Overtime chunk marker
- Late chunk marker
- Unscheduled non-must tasks (dropped)
- Explanation entries (reason codes + plain language)

## 5. Hard Constraints (Must Never Be Violated)
1. No overlapping chunks for the same user timeline.
2. One active task chunk at any instant.
3. FS dependencies must be satisfied:
- Task B start >= Task A completion.
4. Must-schedule tasks cannot be dropped.
5. Non-divisible tasks require one contiguous placement.
6. Divisible tasks must satisfy chunk rules:
- Total chunk duration equals task duration.
- Each chunk respects minimum chunk size.

## 6. Soft Constraints (Optimization Targets)
1. Minimize deadline risk (low slack), weighted by importance and must-schedule status.
2. Minimize lateness, weighted by importance and must-schedule status.
3. Keep higher-cost tasks earlier in each day where feasible.
4. Balance total daily cost across the horizon.
5. Minimize context switching (task fragmentation and frequent switching).
6. Prefer shorter tasks when all higher-level priorities tie.

## 7. Objective Model (Conceptual)
The planner minimizes a weighted objective:

J = a*SlackRiskPenalty + b*LatePenalty + c*DropPenalty + d*InDayOrderPenalty + e*DailyBalancePenalty + f*SwitchingPenalty

Term definitions:
- SlackRiskPenalty: weighted penalty for tasks scheduled too close to due date even if not late.
- LatePenalty: weighted lateness duration.
- DropPenalty: high for must-schedule (effectively prohibitive), lower for non-must by importance.
- InDayOrderPenalty: penalty when lower-cost work is placed before higher-cost work without urgency justification.
- DailyBalancePenalty: variance of daily total cost from target.
- SwitchingPenalty: context-switch and fragmentation penalty.

Slack concept:
- Slack = DueDate - PlannedFinishTime.
- Low positive slack is penalized to reduce last-minute stress, especially for high-importance tasks.
- Negative slack is lateness and is penalized by LatePenalty.

Normalization guidance (V1):
- Penalty terms should be normalized to comparable scales before weighting.
- V1 keeps normalization as an implementation concern, but the planner must document effective ranges used in diagnostics.

Null due-date handling (V1):
- No aging urgency is applied in V1.
- Null due-date tasks are treated as neutral backlog tasks.
- They are considered only after must-schedule and due-dated urgency work has been placed.
- Deterministic tie-break for null due-date tasks: higher importance, then shorter duration, then task ID.

Tie-break order:
1. Lower total penalty.
2. More must-schedule coverage.
3. Larger minimum slack on high-importance tasks.
4. Less lateness.
5. Shorter task duration first.
6. Stable deterministic order by task ID.

## 8. Scheduling Workflow

### 8.0 V1 Execution Pipeline (Locked)
V1 uses a reduced 6-stage execution pipeline to minimize overlap while keeping quality high:
1. PolicyCoordinator + Feasibility
2. WindowBuilder
3. Dependency + Decomposition
4. Scoring
5. OptimizationPlanner
6. Explanation

Note:
- In-day sequencing and cross-day balancing run as internal sub-passes of OptimizationPlanner in V1.
- They can be promoted back to standalone agents later if needed.

### Stage 1: PolicyCoordinator + Feasibility
1. Remove completed tasks from candidate pool.
2. Validate ranges and defaults (importance, cost, duration).
3. Validate dependencies and detect cycles.
4. Expand per-task preference overrides from user defaults.
5. Resolve policy flags for lateness and overtime scope.

Output:
- Validated task set.
- Invalid task diagnostics.
- Effective run policy.

### Stage 2: WindowBuilder
1. Determine horizon:
- Fixed mode: exactly N days (default 21).
- Adaptive mode: estimate required horizon from total remaining duration and effective capacity, then apply guardrails.
2. Build work windows from workdays/hours.
3. Subtract event blocks.
4. Add overtime windows only when policy allows.
5. Attach horizon advisories:
- If estimated horizon exceeds 30 days, request user confirmation before proceeding.
- If estimated horizon exceeds 90 days, emit high-horizon alert.

Output:
- Canonical schedule window set.
- Estimated completion timeline summary.
- Optional horizon advisories.

### Stage 3: Dependency + Decomposition
1. Build dependency DAG.
2. Produce topological layers for FS ordering.
3. Identify blocked tasks and impossible chains.
4. For each divisible task, generate candidate chunk structure.
5. Enforce min chunk size and any chunk count limits.
6. For non-divisible tasks, preserve single-block requirement.

Output:
- Dependency-feasible candidate ordering constraints.
- Scheduling units (chunks or full tasks).

### Stage 4: Scoring
1. Compute urgency from due date proximity and slack risk for due-dated tasks.
2. Combine urgency with importance and must-schedule status.
3. Apply continuity and balance preference weights.
4. Keep null due-date tasks in a neutral backlog bucket for capacity-fill placement.

Output:
- Weighted candidate queue and solver objective coefficients.

### Stage 5: OptimizationPlanner
1. Place units into normal work windows while enforcing hard constraints.
2. If normal windows are exhausted and overtime is enabled, evaluate overtime windows according to OvertimeScope:
- MustOnly: overtime is allowed only for must-schedule tasks.
- AllTasks: overtime is available to all tasks; overdue must-schedule tasks are ordered ahead of overdue non-must tasks.
3. Mark overtime placements explicitly.
4. Mark late placements explicitly.
5. Late policy:
- Must-schedule tasks may be scheduled late only when AllowMustScheduleLateness is true.
- Non-must tasks may be scheduled late only when AllowNonMustLateness is true.
6. If a must-schedule task is impossible under current policy and windows:
- Keep the task incomplete.
- Mark as impossible or overdue as applicable.
- Emit user-visible infeasibility diagnostics.
7. If infeasible for non-must tasks:
- Mark as unscheduled due to capacity.
- Re-evaluate on future runs unless completed, deleted, or archived.
8. Run in-day sequencing and cross-day balancing as internal, constraint-preserving sub-passes.

Output:
- Draft schedule + unscheduled set.
- Placement diagnostics including overtime and infeasibility markers.

### Stage 6: Explanation
1. Emit reason codes for each notable outcome:
- ScheduledOnTime
- ScheduledLate
- ScheduledOvertime
- UnscheduledNonMustCapacity
- BlockedByDependency
- DeferredByPolicy
- InfeasibleMustSchedule
- TimeoutFallbackApplied (only when timeout fallback is enabled and used)
2. Generate user-facing explanation summary.

Output:
- Prioritization result and explainability metadata.

## 9. Agent and Component Responsibilities

### 9.0 Coordination Rule (No Responsibility Overlap)
Each stage has exactly one owning component.

Ownership rules:
1. Validation ownership:
- Only FeasibilityAgent performs validation and cycle checks.
- Later stages consume validated inputs and must not re-validate business rules.
2. Window ownership:
- Only WindowBuilderAgent creates and edits availability windows.
- Later stages read windows as immutable inputs.
3. Decomposition ownership:
- Only DecompositionAgent creates chunk structures.
- OptimizationPlanner may place chunks but must not redefine chunk rules.
4. Scoring ownership:
- Only ScoringAgent computes objective weights and risk features.
- OptimizationPlanner uses weights but must not mutate scoring definitions.
5. Placement ownership:
- Only OptimizationPlanner decides final time placement, drop decisions, and overtime usage.
6. Explanation ownership:
- Only ExplanationAgent maps planner outcomes to user-facing reasoned summaries.

Clarification:
- Later stages may run constraint-preserving checks needed to avoid invalid placements.
- Such checks do not transfer ownership of upstream business-rule validation.

### 9.1 PolicyCoordinator
- Reads user preferences.
- Decides fixed/adaptive horizon strategy.
- Selects planner mode and fallback behavior.

### 9.2 FeasibilityAgent
- Input validation.
- Dependency cycle detection.
- Infeasibility diagnostics.

### 9.3 WindowBuilderAgent
- Builds availability windows from profile and events.
- Adds overtime windows when allowed.

### 9.4 DependencyPlannerAgent
- Produces FS-respecting constraint graph and order guidance.

### 9.5 DecompositionAgent
- Splits divisible tasks according to rules.

### 9.6 ScoringAgent
- Computes urgency and objective weights.

### 9.7 OptimizationPlanner
- Core placement engine (heuristic, solver, or hybrid).

### 9.8 InDaySequencingAgent
- Applies front-loading by cost with feasibility checks.

### 9.9 LoadBalancingAgent
- Reduces cross-day load variance.

### 9.10 ExplanationAgent
- Converts internal decisions to reasoned, user-readable output.

### 9.11 V1 Stage Mapping (Reduced Pipeline)
1. Stage 1: PolicyCoordinator + FeasibilityAgent
2. Stage 2: WindowBuilderAgent
3. Stage 3: DependencyPlannerAgent + DecompositionAgent
4. Stage 4: ScoringAgent
5. Stage 5: OptimizationPlanner
- Includes internal in-day sequencing sub-pass.
- Includes internal cross-day balancing sub-pass.
6. Stage 6: ExplanationAgent

Rationale:
- This preserves separation of concerns and deterministic behavior.
- It reduces orchestration complexity for V1 without sacrificing optimization quality.

## 10. Preferred Architecture Path

### 10.1 Near-Term (Current Codebase)
Hybrid approach:
- Keep MCP-style orchestration and explainability.
- Improve weighting and constraints within staged agents.

### 10.2 Scalable Direction
Solver-centered core with policy layer:
- Use solver for hard constraints and objective optimization.
- Keep agents for normalization, decomposition, and explanations.
- On this branch, legacy fallback is handled by branch rollback strategy instead of in-branch feature flags.

## 11. Overload and Infeasibility Policy
1. Must-schedule tasks:
- Cannot be dropped.
- May be scheduled late only when AllowMustScheduleLateness is true.
- May use overtime windows only when AllowMustScheduleOvertime is true.
- If impossible under current policy and horizon, remain incomplete and emit InfeasibleMustSchedule.
2. Non-must tasks:
- Can be marked unscheduled when required by capacity pressure.
- Are excluded from the current run and re-evaluated on future runs unless completed, deleted, or archived.
- If overdue and AllowNonMustLateness is false, remain unscheduled by policy.

## 12. Adaptive Horizon Policy
1. Default start horizon: 21 days.
2. Adaptive mode estimates horizon to cover remaining work:
- Estimate required days from total estimated remaining task duration and effective daily capacity.
- Expand horizon in deterministic steps within min/max guardrails.
3. User-facing guidance thresholds:
- If estimated horizon exceeds 30 days, ask user to confirm proceeding.
- If estimated horizon exceeds 90 days, emit high-horizon alert and show estimated timeline.
4. Expansion triggers can include:
- High must-schedule lateness risk.
- Excess unscheduled non-must ratio.
5. Must obey runtime/performance cap.
6. Timeout fallback is optional in V1:
- If enabled and timeout occurs, return partial deterministic result with TimeoutFallbackApplied.
- If not enabled, omit timeout fallback reason codes.

## 13. Recovery and Buffer Policy
1. Recovery buffers are opt-in.
2. If enabled, insert buffer blocks after high-cost chunks.
3. Buffers reduce effective capacity and may increase dropping/lateness.
4. Buffer behavior must be visible in explanations.

## 14. Determinism and Reproducibility
1. Same inputs and preferences should produce same output.
2. Randomness is disallowed in V1 default mode.
3. If future stochastic optimization is introduced, it must support fixed seed mode.
4. Stage-level sorting and tie-break behavior must be stable, with final tie-break by task ID.

## 15. Output Contract (Conceptual)
Result should include:
- ScheduledTasks (with chunks)
- UnscheduledTasks
- OvertimeChunks
- LateTasks
- History or DecisionLog
- ExplanationEntries

Each explanation entry includes:
- TaskId
- Status
- ReasonCode
- ShortMessage
- Optional diagnostics metadata

### 15.1 Canonical Reason Code Table (V1)
| ReasonCode | Meaning | Typical Trigger |
| :--- | :--- | :--- |
| `ScheduledOnTime` | Task or chunk was scheduled without lateness. | Placement within allowed window and before due date. |
| `ScheduledLate` | Task was scheduled but finishes after due date. | Due-date miss allowed by policy and no on-time feasible placement. |
| `ScheduledOvertime` | Task was placed in overtime window. | Normal windows exhausted and overtime policy permits placement. |
| `UnscheduledNonMustCapacity` | Non-must task excluded from current run due to capacity or policy. | Lower-priority non-must work cannot be placed without violating constraints. |
| `BlockedByDependency` | Task could not be placed because dependency prerequisites were not met. | FS dependency ordering prevents legal placement. |
| `DeferredByPolicy` | Task was not placed due to explicit policy settings. | Lateness disabled or overtime scope excludes the task type. |
| `InfeasibleMustSchedule` | Must-schedule task cannot be placed under current policy/horizon. | No legal placement remains even after allowed overtime/lateness rules. |
| `TimeoutFallbackApplied` | Planner timed out and returned deterministic fallback result. | Optional timeout fallback mode is enabled and timeout threshold reached. |

## 16. Testing Expectations
Minimum scheduling test groups:
1. Dependency correctness (FS ordering always preserved).
2. Must-schedule protection under overload.
3. Non-must dropping behavior and exclusion policy.
4. Divisible and non-divisible placement correctness.
5. Slack protection behavior for high-importance tasks (avoid last-minute placement when feasible).
6. In-day cost ordering with urgency and slack overrides.
7. Cross-day balancing behavior.
8. Adaptive horizon expansion within guardrails.
9. Deterministic replay for identical inputs.

TDD execution rule:
1. Implement each scheduler feature via Red-Green-Refactor.
2. Write failing behavior tests first from this specification.
3. Keep feature slices small so each policy change maps to specific tests.

TDD merge gate:
1. No scheduler behavior change is complete without test-first evidence in the same PR.
2. If a policy rule changes, corresponding tests must be added or updated in the same PR.

## 17. Operational Metrics
Track per run:
- Percentage scheduled on time.
- Must-schedule lateness count and total lateness.
- Non-must drop count.
- Daily cost variance.
- Context-switch count.
- Runtime and timeout/fallback rate.

## 18. Risk Register
1. Policy drift between docs and implementation.
2. Hidden behavior differences between old and new planners.
3. Explainability regression when introducing solver internals.
4. Performance degradation at larger horizons.

Mitigation:
- Maintain contract-first docs.
- Use migration test matrix and branch-level comparisons.
- Enforce acceptance criteria before default switch.

## 19. Implementation Sequence (Recommended)
1. Finalize contracts and reason codes.
2. Implement explicit preference fields and defaults.
3. Build canonical window/dependency/decomposition pipeline.
4. Introduce optimization planner abstraction.
5. Implement solver-centered planner on this branch after legacy path removal.
6. Validate behavior against migration test matrix and documentation contracts.
7. Merge to shared branch only after acceptance criteria are met.

## 20. Related Documents
- [docs/SCHEDULING_DISCUSSION_NOTES.md](docs/SCHEDULING_DISCUSSION_NOTES.md)
- [docs/RFC_SOLVER_MIGRATION.md](docs/RFC_SOLVER_MIGRATION.md)
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/STATUS.md](docs/STATUS.md)
- [docs/TODO.md](docs/TODO.md)
