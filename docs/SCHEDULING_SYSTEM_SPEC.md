# Scheduling System Specification

## 1. Purpose
This document defines the complete scheduling design for Priority Task Manager.

It consolidates requirements, algorithm behavior, workflow stages, agent responsibilities, data contracts, preference effects, fallback behavior, and expected outputs.

This is the implementation source of truth for scheduling logic.

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

### Stage 0: Input Validation and Normalization
1. Remove completed tasks from candidate pool.
2. Validate ranges and defaults (importance, cost, duration).
3. Validate dependencies and detect cycles.
4. Expand per-task preference overrides from user defaults.

Output:
- Validated task set.
- Invalid task diagnostics.

### Stage 1: Horizon and Availability Construction
1. Determine horizon:
- Fixed mode: exactly N days (default 21).
- Adaptive mode: start from baseline and expand within guardrails.
2. Build work windows from workdays/hours.
3. Subtract event blocks.
4. Add overtime windows only when policy allows.

Output:
- Canonical schedule window set.

### Stage 2: Dependency Layering
1. Build dependency DAG.
2. Produce topological layers for FS ordering.
3. Identify blocked tasks and impossible chains.

Output:
- Dependency-feasible candidate ordering constraints.

### Stage 3: Task Decomposition
1. For each divisible task, generate candidate chunk structure.
2. Enforce min chunk size and any chunk count limits.
3. For non-divisible tasks, preserve single-block requirement.

Output:
- Scheduling units (chunks or full tasks).

### Stage 4: Prioritization and Weighting
1. Compute urgency from due date proximity and slack risk.
2. Combine urgency with importance and must-schedule status.
3. Apply continuity and balance preference weights.

Output:
- Weighted candidate queue and solver objective coefficients.

### Stage 5: Placement Optimization
1. Place units into windows while enforcing hard constraints.
2. Mark overtime placements explicitly.
3. Mark late placements explicitly.
4. If infeasible:
- Keep must-schedule candidates in pool.
- Drop only non-must tasks by lowest value first.

Output:
- Draft schedule + dropped set.

### Stage 6: In-Day Ordering Pass
1. Reorder chunks within each day for descending cost where feasible.
2. Do not violate dependency, lateness, or must-schedule feasibility.

Output:
- Energy-aware day sequences.

### Stage 7: Cross-Day Balancing Pass
1. Reduce large day-to-day cost spikes.
2. Respect due-date risk thresholds while balancing.

Output:
- Smoothed weekly/horizon load profile.

### Stage 8: Explanation and Result Packaging
1. Emit reason codes for each notable outcome:
- ScheduledOnTime
- ScheduledLate
- ScheduledOvertime
- DroppedNonMustCapacity
- BlockedByDependency
- DeferredByPolicy
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
- Keep current planner as fallback under feature flag during migration.

## 11. Overload and Infeasibility Policy
1. Must-schedule tasks:
- Cannot be dropped.
- May be scheduled late if needed.
- May use overtime windows if allowed.
2. Non-must tasks:
- Can be dropped when required by capacity pressure.
- Stay incomplete and excluded from future cycles unless explicitly reactivated.

## 12. Adaptive Horizon Policy
1. Default start horizon: 21 days.
2. Adaptive expansion allowed only within min/max guardrails.
3. Expansion triggers can include:
- High must-schedule lateness risk.
- Excess dropped non-must ratio.
4. Must obey runtime/performance cap and fallback if exceeded.

## 13. Recovery and Buffer Policy
1. Recovery buffers are opt-in.
2. If enabled, insert buffer blocks after high-cost chunks.
3. Buffers reduce effective capacity and may increase dropping/lateness.
4. Buffer behavior must be visible in explanations.

## 14. Determinism and Reproducibility
1. Same inputs and preferences should produce same output.
2. Randomness is disallowed in V1 default mode.
3. If future stochastic optimization is introduced, it must support fixed seed mode.

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
- Use shadow-mode comparisons.
- Enforce acceptance criteria before default switch.

## 19. Implementation Sequence (Recommended)
1. Finalize contracts and reason codes.
2. Implement explicit preference fields and defaults.
3. Build canonical window/dependency/decomposition pipeline.
4. Introduce optimization planner abstraction.
5. Add solver-backed implementation behind feature flag.
6. Run shadow-mode parity and performance checks.
7. Promote solver planner to default once acceptance criteria are met.

## 20. Related Documents
- [docs/SCHEDULING_DISCUSSION_NOTES.md](docs/SCHEDULING_DISCUSSION_NOTES.md)
- [docs/RFC_ELASTIC_SCHEDULING.md](docs/RFC_ELASTIC_SCHEDULING.md)
- [docs/RFC_SOLVER_MIGRATION.md](docs/RFC_SOLVER_MIGRATION.md)
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/STATUS.md](docs/STATUS.md)
- [docs/TODO.md](docs/TODO.md)
