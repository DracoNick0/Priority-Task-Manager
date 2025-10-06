# **Risk Assessment Matrix: Priority Task Manager (Reduced Scope)**

## **1. Introduction**

This document outlines the potential risks identified for the 7-week project to implement a **three-agent** prioritization system in the Priority Task Manager at 6 hours per week. The purpose of this matrix is to proactively identify, analyze, and prepare mitigation and contingency strategies to ensure the project's successful and timely completion. The reduced scope significantly lowers overall project risk compared to the original four-agent proposal.

## **2. Risk Matrix**

| ID | Risk Description | Likelihood | Impact | Risk Level | Mitigation Strategy | Contingency Plan |
| :- | :--- | :--- | :--- | :--- | :--- | :--- |
| **T1** | **Algorithm Complexity**<br>The prioritization formula in the `PrioritizationAgent` produces unintuitive task ordering. | Medium | Medium | **Medium** | **Simple Formula:** Use a straightforward weighted sum: `FinalScore = (TaskAnalyzerScore * 0.7) + (UserContextScore * 0.3)`.<br>**Real-World Testing:** Test with actual task lists weekly to verify intuitive results.<br>**TDD with Key Scenarios:** Develop specific unit tests for common use cases. | Adjust the weights (e.g., 0.8/0.2 or 0.6/0.4) if results aren't intuitive. In worst case, use equal weighting (0.5/0.5). The "single-agent" mode serves as a built-in fallback. |
| **T2** | **Refactoring Failure**<br>The foundational refactoring in Week 1 to introduce the `IUrgencyStrategy` interface introduces critical bugs. | Low | High | **Medium** | **Comprehensive Test Coverage:** Rely on the existing xUnit test suite as a safety net. All existing tests must pass.<br>**Version Control:** Perform all refactoring on a dedicated Git branch.<br>**Incremental Changes:** Make small, testable changes rather than one large refactoring. | Revert the branch using Git. Re-approach the refactoring with a less invasive adapter pattern, or accept tighter coupling temporarily to maintain schedule. |
| **T3** | **Agent Coordination Failures**<br>The MCP coordination system produces inconsistent state or corrupted data. | Low | High | **Medium** | **Context Validation:** Implement strict validation at each agent execution step.<br>**Audit Trail:** Use MCP's History feature to track state transitions.<br>**Unit Tests:** Create specific tests that simulate context corruption scenarios. | Implement automatic fallback to single-agent mode when context validation fails. Log detailed diagnostic information. The single-agent mode ensures users always get a prioritized list. |
| **T4** | **Unforeseen Bugs**<br>A significant bug appears in Week 6 that threatens core functionality. | Low | Medium | **Low** | **Incremental Integration:** Weekly testing reduces "big bang" integration failures.<br>**Clean Code Practices:** Modular agent design makes debugging easier.<br>**Weekly Testing:** Run integration tests at the end of each sprint. | Disable the buggy feature if a fix cannot be found within 1-2 days. The reduced scope means fewer interdependencies—bugs are more likely to be isolated. |
| **D1** | **Data File Corruption**<br>JSON files become corrupted, causing application crashes. | Low | Medium | **Low** | **Schema Validation:** Validate all loaded JSON against expected schemas.<br>**Defensive Parsing:** Use try-catch blocks with detailed error messages.<br>**Graceful Degradation:** Initialize with empty data if files are corrupt. | Preserve corrupt files with `.corrupt` extension. Notify user with clear instructions to restore from a previous working state or start fresh. The simplified scope means fewer data files to manage. |
| **PM1**| **Scope Creep**<br>Attempting to add features beyond the defined core scope. | Low | Medium | **Low** | **Strict Adherence to Plan:** Rigorously follow the defined core feature list.<br>**Backlog Management:** Document all new ideas in a "Post-Submission Enhancements" list.<br>**Weekly Review:** Review goals at the start of each week to ensure alignment. | Firmly remind yourself that GoalAlignment, user preferences, and backups are **post-submission features**. Focus only on the three-agent core. |
| **PM2**| **Time Overrun**<br>A sprint takes longer than 6 hours, causing schedule pressure. | Medium | Medium | **Medium** | **Granular Task Breakdown:** Break each weekly goal into daily 1-2 hour tasks.<br>**AI Tool Usage:** Leverage Copilot heavily for boilerplate, test scaffolding, and repetitive code.<br>**Daily Tracking:** Track actual hours vs. planned. If >2 hours over by Friday, adjust next week's scope.<br>**Simplified Features:** Time-of-day logic is straightforward (4 time periods), formula is simple weighted sum. | If Week 3 or 4 runs 2+ hours over, reduce test count from 5-6 scenarios to 3-4 core scenarios. If Week 5 runs over, simplify the formula further or reduce integration tests from 3 to 2. |
| **PM3**| **Tool Dependency**<br>AI tools are unavailable or produce low-quality code. | Low | Low | **Low** | **Manual Proficiency:** Be prepared to code manually if needed.<br>**Critical Review:** All AI-generated code must be reviewed and understood.<br>**Simple Code:** The reduced scope means less complex code that's easier to write manually. | Revert to fully manual coding. The 6-hour/week pace is sustainable even without AI acceleration, though you may need to simplify test coverage to 40% if coding manually. |
| **PM4**| **Personal Disruption**<br>Illness, family emergency, or other coursework conflicts disrupt development. | Low | Medium | **Low** | **Modular Design:** Each agent is independent—partial completion still provides value.<br>**Flexible Weeks:** Some weeks (2, 4, 7) have slightly lighter loads and can absorb minor disruptions.<br>**Regular Commits:** Git ensures no work is lost. | If significant disruption occurs: 1) Pause development, 2) Use lighter weeks (2, 7) to catch up, 3) If multiple weeks lost, deliver with 2 agents instead of 3 (TaskAnalyzer + Prioritization only, skip UserContext), 4) Communicate with instructor if extension needed. |

## **3. Risk Mitigation Timeline**

| Week | Risk Mitigation Activities | Hours Allocated |
| :--- | :--- | :--- |
| **Week 1** | - Complete refactoring on feature branch with full test validation<br>- All existing tests must pass before merging<br>- Set up daily progress tracking | 6 hours |
| **Week 2** | - Verify all agent classes scaffold correctly<br>- Test JSON validation works<br>- **CHECKPOINT: If Week 1 went over by 2+ hours, simplify Week 3 test count** | 6 hours |
| **Week 3** | - Test TaskAnalyzer with real task lists<br>- Verify scores make logical sense<br>- **CHECKPOINT: If incomplete, reduce test scenarios and continue** | 7 hours |
| **Week 4** | - Test time-of-day logic in all 4 periods<br>- Verify edge cases (midnight transitions)<br>- **MID-PROJECT: Assess if on track for completion** | 6 hours |
| **Week 5** | - Run first full end-to-end test<br>- Verify multi-agent produces better results than single-agent<br>- **CHECKPOINT: If formula doesn't work, simplify immediately** | 7 hours |
| **Week 6** | - Test mode switching thoroughly<br>- Fix any critical bugs<br>- **FEATURE FREEZE by Thursday** | 5 hours |
| **Week 7** | - Final testing Monday-Tuesday<br>- Documentation Wednesday-Friday<br>- Video recording over weekend if needed | 5 hours |

## **4. Risk Response Decision Matrix**

| Situation | Response |
| :--- | :--- |
| Week runs 1-2 hours over | Accept it—allocate from next week's buffer or work slightly longer |
| Week runs 2+ hours over | Reduce test scenarios in current or next week by 20-30% |
| TaskAnalyzer too complex | Simplify deadline scoring to: urgent (<3 days), normal (3-7 days), low (>7 days) |
| UserContext too complex | Use fixed time periods (morning/afternoon/evening/night) with static score multipliers |
| PrioritizationAgent formula doesn't work | Use simple weighted average: 70% task score + 30% context score |
| Mode switching has bugs | Fix if <2 hours; otherwise document as known issue and deliver functional multi-agent mode |
| Week 6 incomplete | Extend into first 2 days of Week 7, reduce documentation quality slightly |
| Major personal disruption | Communicate immediately, use lighter weeks to catch up, worst case deliver 2-agent system |

## **5. Success Criteria & Quality Gates**

| Week | Must Complete | Can Defer if Needed |
| :--- | :--- | :--- |
| **Week 1** | Interface created, DI working, existing tests pass | Perfect code comments |
| **Week 2** | All 3 agents scaffolded, basic validation working | Extensive edge case tests |
| **Week 3** | TaskAnalyzer produces logical scores in 3+ scenarios | 5-6 test scenarios (can do 3-4) |
| **Week 4** | UserContext handles all 4 time periods correctly | Complex edge case handling |
| **Week 5** | Multi-agent mode works and improves on single-agent | 3 integration tests (can do 2) |
| **Week 6** | Mode switching functional, system stable | Performance optimization |
| **Week 7** | README complete, video recorded, submission ready | Comprehensive API docs |

## **6. Why This Scope is Lower Risk**

Compared to the original proposal, this reduced scope significantly lowers risk:

1. **50% Less Agent Complexity:** 3 agents instead of 4 (no GoalAlignment)
2. **No External Configuration:** No user_profile.json, config commands, or goal management
3. **Simpler Data Management:** No backup system needed—reduces failure points
4. **Reduced Test Burden:** 50% coverage target vs 80%, fewer edge cases
5. **Time Buffer Built In:** 42 hours of work spread across 7 weeks allows for minor overruns
6. **AI Assistance Effective:** Simpler code means AI tools more reliable for generation
7. **Clear Success Path:** Three working agents = successful project, no ambiguity

**Overall Risk Assessment:** Low to Medium

This project has a high probability of success (~85%) given the focused scope, realistic time allocation, and strong foundation. The key to success is disciplined adherence to the simplified scope and immediate adjustment if any week overruns by more than 2 hours.

## **2. Risk Matrix**

| ID | Risk Description | Likelihood | Impact | Risk Level | Mitigation Strategy | Contingency Plan |
| :- | :--- | :--- | :--- | :--- | :--- | :--- |
| **T1** | **Algorithm Complexity**<br>The prioritization formula in the `PrioritizationAgent` is difficult to balance, resulting in unintuitive or illogical task ordering that doesn't meet the project's core goal. | High | High | **Critical** | **TDD with Key Scenarios:** Develop specific unit tests for common use cases (e.g., "urgent but low-complexity" vs. "non-urgent but high-complexity").<br>**Start Simple:** Begin with a transparent, additive formula and iteratively add complexity.<br>**Configurable Weights:** Keep the weights for each agent's score in an easily accessible configuration spot for rapid tuning.<br>**Real-World Testing:** Test with actual task lists weekly to verify intuitive results. | Revert to a simpler, more predictable formula for the final deliverable. The "single-agent" mode serves as a built-in fallback to guarantee a functional, if less intelligent, prioritization engine. |
| **T2** | **Refactoring Failure**<br>The foundational refactoring in Week 1 to introduce the `IUrgencyStrategy` interface introduces critical bugs that break existing, stable functionality. | Medium | High | **High** | **Comprehensive Test Coverage:** Rely on the existing xUnit test suite to serve as a safety net. All existing tests must pass before the refactoring is considered complete.<br>**Version Control:** Perform all refactoring on a dedicated Git branch to allow for easy review and the ability to revert completely if the changes are unstable.<br>**Incremental Changes:** Make small, testable changes rather than one large refactoring. | Revert the branch using Git. Re-approach the refactoring with a less invasive method, potentially using an adapter pattern to wrap existing code, or accept tighter coupling as a temporary trade-off to maintain the schedule. |
| **T3** | **Unforeseen Critical Bugs**<br>A significant bug appears late in the development cycle (e.g., Week 6-7) that is difficult to diagnose and threatens a core feature, such as the mode-switching logic or agent coordination. | Medium | High | **High** | **Incremental Integration:** The weekly sprint plan is designed to integrate features as they are built, reducing the likelihood of a "big bang" integration failure at the end.<br>**Clean Code Practices:** Adherence to SOLID principles and the modular agent design makes the codebase easier to debug.<br>**Weekly Integration Tests:** Run full end-to-end tests at the end of each sprint to catch integration issues early. | Isolate the buggy feature and disable it if a fix cannot be found within 2-3 days. Deliver a functional project with the feature explicitly noted as "in-progress" rather than delivering a broken application. The buffer week (Week 8) provides time to address late-discovered bugs. |
| **T4** | **Agent Coordination Failures**<br>The MCP coordination system produces inconsistent state, with agents receiving corrupted data or the `MCPContext` becoming invalid mid-execution. | Medium | High | **High** | **Context Validation:** Implement strict validation at each agent execution step to verify required data exists and is well-formed.<br>**Immutable Context Sections:** Design the context so completed agent outputs cannot be overwritten by subsequent agents.<br>**Audit Trail:** Use MCP's History feature extensively to track state transitions and identify where corruption occurs.<br>**Unit Tests:** Create specific tests that simulate context corruption scenarios. | Implement automatic fallback to single-agent mode when context validation fails. Log detailed diagnostic information to help identify the root cause. The single-agent mode serves as a safety net ensuring users always get a prioritized list. |
| **D1** | **Data Integrity & File Corruption**<br>One or more JSON files (`tasks.json`, `lists.json`, `user_profile.json`) become corrupted or invalid, causing application crashes or data loss. | Medium | High | **High** | **Automatic Backups:** Implement automatic timestamped backups before every write operation. Retain the 5 most recent backups.<br>**Schema Validation:** Validate all loaded JSON against expected schemas before use.<br>**Atomic Writes:** Use temp files and atomic rename operations to prevent partial writes during crashes.<br>**Defensive Parsing:** Use try-catch blocks with detailed error messages when parsing JSON. | If corruption is detected: 1) Attempt to load the most recent backup, 2) If all backups are corrupt, initialize with empty/default data and notify the user with clear recovery instructions, 3) Preserve corrupt files in a `corrupted/` directory for potential manual recovery. |
| **T5** | **Test Coverage Inadequacy**<br>Unit tests don't cover critical edge cases or real-world scenarios, leading to bugs that only appear during actual use, discovered too late to fix easily. | Medium | Medium | **Medium** | **Coverage Metrics:** Use code coverage tools to ensure minimum 80% coverage for all new agent code.<br>**Edge Case Documentation:** Maintain a checklist of edge cases (no deadline, past due, empty lists, no goals set, missing profiles) and ensure each has a test.<br>**Real-World Scenario Tests:** Create integration tests based on actual use cases described in the proposal.<br>**Weekly Manual Testing:** Manually test the application with realistic task lists each week. | If gaps are discovered late: prioritize fixing the most critical bugs and document known issues in the README. Use the buffer week (Week 8) to address any gaps. Accept that some edge cases may need to be handled in a future version post-submission. |
| **PM1**| **Scope Creep**<br>The desire to add features beyond the defined core scope (e.g., trying to implement all stretch goals) jeopardizes the 8-week timeline. | Medium | High | **High** | **Strict Adherence to Plan:** Rigorously follow the defined weekly sprints and core feature list.<br>**Backlog Management:** Document all new ideas or feature requests in a separate "Post-Launch" list to avoid distraction.<br>**Weekly Review:** At the start of each week, review the goals to ensure they align with the core scope.<br>**Definition of Done:** Clearly define what "done" means for each sprint and resist adding "just one more thing." | If a new feature is deemed absolutely essential, formally de-scope a core feature of equivalent effort. The primary goal is a finished, polished product within the timeframe. Stretch goals remain strictly optional and only attempted if time permits after all core features are complete. |
| **PM2**| **Schedule Slippage**<br>A sprint's deliverables take longer than the allotted week, causing a domino effect on the rest of the schedule. | **Very High** | **Critical** | **CRITICAL** | **Granular Task Breakdown:** Break each weekly goal into a checklist of small, daily tasks with hour estimates.<br>**Front-Load Complexity:** The project plan intentionally places the most complex refactoring and agent logic in the earlier weeks (Weeks 1-3).<br>**Daily Progress Tracking:** Review progress daily. If >4 hours behind by Friday, trigger immediate scope reduction.<br>**No Buffer Week:** Unlike 8-week plans, there is zero margin for error. Every delay must be addressed immediately.<br>**Efficient Tooling:** Leverage AI development tools to accelerate boilerplate and common code patterns.<br>**Pre-Defined Cut Points:** Week 4 checkpoint determines if GoalAlignment stays or goes. | **Immediate Scope Reduction Protocol:**<br>- Day 2 behind: Work 4-6 extra hours over weekend to catch up<br>- Day 3+ behind: Cut test coverage targets from 70% to 60%<br>- Week 3 misses deadline: Move GoalAlignmentAgent to stretch goal permanently<br>- Week 5 misses deadline: Simplify PrioritizationAgent formula to basic weighted sum<br>- Week 6 misses deadline: Reduce documentation requirements to bare minimum<br>**Accept that missing deadlines = reduced scope, not working longer hours indefinitely.** |
| **PM3**| **Tool Dependency**<br>The AI code generation tools (Copilot, Gemini) are unavailable or produce low-quality code, slowing down the development pace. | Low | Medium | **Low** | **Manual Proficiency:** Maintain proficiency in writing all code manually. The AI tools are accelerators, not crutches.<br>**Critical Review:** All AI-generated code must be thoroughly reviewed and understood before being committed to the codebase.<br>**Fallback Ready:** Be prepared to switch to fully manual development at any time without panic. | Revert to a fully manual coding workflow. The timeline has buffer built in, but non-essential tasks (like extensive documentation beyond core requirements) might be simplified to compensate for the reduced velocity. Focus on core functionality over polish if time becomes tight. |
| **PM4**| **Personal Factors**<br>Unexpected personal circumstances (illness, family emergency, other coursework conflicts) disrupt the development schedule. | Medium | High | **High** | **Front-Loading:** Schedule the most critical and complex work in the first 6 weeks when disruption is less costly.<br>**Buffer Week:** Week 8 provides cushion for unexpected delays.<br>**Modular Design:** Because agents are independent, partial completion of a sprint still provides value.<br>**Regular Backups:** Git commits ensure no work is ever lost if development must pause. | If significant disruption occurs: 1) Immediately reassess scope and prioritize absolutely essential features, 2) Communicate with instructor about potential extension if needed, 3) Lean heavily on the single-agent fallback—even if multi-agent isn't fully polished, a working application can be delivered, 4) Use the buffer week to catch up after disruption resolves. |

## **3. Risk Mitigation Timeline**

To proactively manage risks throughout the project lifecycle, specific risk mitigation activities are scheduled:

| Week | Risk Mitigation Activities |
| :--- | :--- |
| **Week 1** | - Complete all refactoring on a feature branch with full test validation before merging<br>- Establish backup system for all JSON files<br>- Set up daily progress tracking system<br>- **CRITICAL: Must complete on time - no week 8 buffer exists** |
| **Week 2** | - Implement and test JSON validation and error recovery<br>- Verify backup/restore functionality works correctly<br>- **CHECKPOINT: If Week 1 slipped, reduce test count targets for all agents** |
| **Week 3** | - Conduct first real-world testing session with actual task lists<br>- Evaluate test coverage metrics (target 70% minimum)<br>- **CHECKPOINT: If TaskAnalyzer incomplete, move GoalAlignment to stretch goal** |
| **Week 4** | - **CRITICAL MID-PROJECT CHECKPOINT:** Assess overall progress<br>- If >2 days behind schedule, immediately cut GoalAlignmentAgent<br>- Test graceful degradation scenarios<br>- Make formal go/no-go decision on goal features |
| **Week 5** | - Integration testing of completed agents<br>- If PrioritizationAgent formula proves complex, simplify immediately<br>- **CHECKPOINT: If behind, reduce integration test count from 5 to 3** |
| **Week 6** | - Mode switching must be completed by Wednesday<br>- Thursday-Friday reserved for critical bug fixes only<br>- **CHECKPOINT: Feature freeze - no new features, bugs only** |
| **Week 7** | - Monday-Wednesday: Final bug fixes<br>- Thursday-Sunday: Documentation and video<br>- **NO FEATURE WORK - Submission prep only** |

## **4. Risk Response Decision Matrix**

This matrix provides quick decision guidance when risks materialize:

| Situation | Response |
| :--- | :--- |
| A sprint is 1-2 days behind | Work extra hours for 2-3 days, simplify non-critical tasks in current sprint |
| A sprint is 3+ days behind | De-scope a lower-priority feature from next sprint, move to stretch goals |
| Critical bug found in Week 6-7 | Allocate up to 3 days to fix; if unfixable, disable feature and document |
| Test coverage below 70% | Stop new feature development, write tests until >80% before proceeding |
| Data corruption detected | Must be fixed immediately before any other work; triggers use of buffer time |
| Multi-agent coordination unstable | Simplify formula, reduce agent complexity, or deliver with single-agent as primary mode |
| Behind schedule in Week 7 | Immediately move all stretch goals to post-submission; focus only on core features |
| Personal emergency | Pause development, communicate with instructor, use buffer week for recovery |

## **5. Success Criteria & Quality Gates**

Each week has defined quality gates that must be met before proceeding to the next sprint. **In a 7-week timeline, missing any gate by >2 days requires immediate scope reduction.**

- **Week 1:** All existing tests pass, no regressions introduced, backup system functional
- **Week 2:** JSON validation works, backup/restore tested successfully, all agents scaffolded  
- **Week 3:** TaskAnalyzerAgent has >70% test coverage and produces logical scores, UserContext time logic working
- **Week 4:** UserContextAgent complete with preference handling, goal commands functional (or GoalAlignment cut from scope)
- **Week 5:** PrioritizationAgent produces better results than single-agent in >3 test scenarios, all agents integrated
- **Week 6:** Mode switching works correctly, all critical bugs fixed, system stable
- **Week 7:** Documentation complete to minimum standards, demo video recorded, submission ready

## **6. Lessons Learned & Continuous Improvement**

Throughout the project, maintain a `LESSONS.md` file documenting:
- What risks materialized and how they were handled
- Which mitigation strategies were most effective
- What unexpected challenges arose
- What would be done differently in future projects

This continuous learning approach ensures that even if risks materialize, they provide valuable experience for future work.