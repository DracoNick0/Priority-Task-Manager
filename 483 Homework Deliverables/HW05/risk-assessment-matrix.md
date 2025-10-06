# **Risk Assessment Matrix: Priority Task Manager**

## **1. Introduction**

This document outlines the potential risks identified for the 7-week project to implement a **three-agent** prioritization system in the Priority Task Manager at 6 hours per week. The purpose of this matrix is to proactively identify, analyze, and prepare mitigation and contingency strategies to ensure the project's successful and timely completion.

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