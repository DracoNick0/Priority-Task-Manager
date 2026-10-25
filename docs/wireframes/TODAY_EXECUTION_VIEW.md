# Wireframe: Today Execution View (The Hands-Off Compass)

## Purpose
The **Today Execution View** serves as a fluid planning compass. It answers: **"What is the best order to do my work today, and what fits in my remaining hours?"**

It supports students, freelancers, and independent workers with variable schedules, multi-hour gaps between tasks, and minimal administrative interaction.

---

## Visual Wireframe: Scenario A - Morning Opening (The Daily Game Plan)

When opened in the morning (e.g. 09:00 AM), the view provides a clear, priority-ranked sequence and an honest workload gauge:

```text
+----------------------------------------------------------------------------------------------------+
|  TODAY - Thursday, Oct 24, 2026                             [ + Quick Task ]  [ Settings: Hours ]  |
+----------------------------------------------------------------------------------------------------+
|                                                                                                    |
|  TODAY'S WORKLOAD: 5.0h Planned Work | 1.5h Meetings | 1.5h Free Buffer  [ CAPACITY: [======= ] 77%]|
|                                                                                                    |
|  RECOMMENDED SEQUENCE FOR TODAY (Order calculated by Gold Panning based on deadlines and effort):  |
|                                                                                                    |
|  +-- 1. HIGHEST URGENCY (Least Slack) ---------------------------------------------------------+   |
|  |  [ ] Math Homework Problem Set #4                                                           |   |
|  |      Effort: ~1.5h | Due: Today at 5:00 PM | Least Slack (0.5h buffer remaining)            |   |
|  |      Suggested window: Morning (9:30 AM - 11:00 AM)                                         |   |
|  +---------------------------------------------------------------------------------------------+   |
|                                                                                                    |
|  +-- FIXED CALENDAR COMMITMENT ----------------------------------------------------------------+   |
|  |  [*] 11:30 AM - 12:30 PM: Biology Lab Lecture (In-person)                                   |   |
|  +---------------------------------------------------------------------------------------------+   |
|                                                                                                    |
|  +-- 2. NEXT UP -------------------------------------------------------------------------------+   |
|  |  [ ] History Essay Research & Outline (Part 1 of 2)                                         |   |
|  |      Effort: ~2.0h | Due: Friday | Realistic Slack: 3.5h                                    |   |
|  |      Suggested window: Afternoon (1:30 PM - 3:30 PM)                                        |   |
|  +---------------------------------------------------------------------------------------------+   |
|                                                                                                    |
|  +-- 3. LOW EFFORT / FLEXIBLE -----------------------------------------------------------------+   |
|  |  [ ] Reply to Internship Inquiries & Send Transcript                                        |   |
|  |      Effort: ~45m | Due: Friday | High Slack                                                |   |
|  |      Suggested window: Late Afternoon                                                       |   |
|  +---------------------------------------------------------------------------------------------+   |
|                                                                                                    |
|  ======================================= WORKDAY ENDS AT 5:00 PM =================================  |
+----------------------------------------------------------------------------------------------------+
```

---

## Visual Wireframe: Scenario B - Returning After a 4-Hour Gap

When opening the app after working offline or stepping away, the schedule displays updated capacity against the remaining workday:

```text
+----------------------------------------------------------------------------------------------------+
|  TODAY - Thursday, Oct 24, 2026 (Current Time: 2:30 PM)                       [ Settings: Hours ]  |
+----------------------------------------------------------------------------------------------------+
|                                                                                                    |
|  STATUS UPDATE:                                                                                    |
|  * Remaining work hours today: 2.5 hours left (until 5:00 PM)                                      |
|  * Incomplete tasks for today: 4.2 hours planned                                                   |
|  * Load Notice: 1.7h of planned work extends past 5:00 PM unless postponed.                        |
|                                                                                                    |
|  REMAINING TASKS FOR TODAY:                                                                        |
|                                                                                                    |
|  +-- URGENT DEADLINE (Needs immediate attention) ----------------------------------------------+   |
|  |  [ ] Math Homework Problem Set #4                                             [ [x] Done ]  |   |
|  |      Effort: ~1.5h | Due Today at 5:00 PM                                                   |   |
|  |      Status: Fits within your remaining 2.5h if started now.                                |   |
|  +---------------------------------------------------------------------------------------------+   |
|                                                                                                    |
|  +-- CAN BE POSTPONED (Not due today) ---------------------------------------------------------+   |
|  |  [ ] History Essay Outline (Part 1 of 2)                                      [ [x] Done ]  |   |
|  |      Effort: ~2.0h | Due: Friday                                                            |   |
|  |      [ >> Push to Tomorrow Morning ] (Balances today's workload in 1 tap)                   |   |
|  +---------------------------------------------------------------------------------------------+   |
|                                                                                                    |
|  +-- QUICK WINS -------------------------------------------------------------------------------+   |
|  |  [ ] Reply to Internship Inquiries (~45m)                                     [ [x] Done ]  |   |
|  +---------------------------------------------------------------------------------------------+   |
|                                                                                                    |
|  [ One-Click Action: "Push non-urgent tasks to tomorrow" ]   [ "Extend today's hours by 1h" ]      |
+----------------------------------------------------------------------------------------------------+
```

---

## Core Hands-Off Principles

1. **Autonomous Flow**:
   - The app presents a recommended sequence. Tasks remain stable across interruptions.
2. **Asynchronous Batch Completion**:
   - Users check off completed work at any time (`[ [x] Done ]`), batching updates on demand.
3. **Sequence Continuity**:
   - Earlier unscheduled tasks remain in the active flow as recommended next actions.
4. **Direct Capacity Adjustment**:
   - When remaining work exceeds available workday hours, 1-tap actions adjust the plan instantly (`Push non-urgent tasks to tomorrow` or `Extend today's hours by 1h`).
5. **Evening Accomplishment Summary**:
   - At the end of configured work hours, the view summarizes completed tasks and provides tomorrow's preview.
