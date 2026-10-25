# Wireframe: Empty States, Onboarding Tour & Daily Rituals

## Purpose
This document specifies user onboarding and transitional moments:
1. **The Actionable Empty Schedule ([#77](https://github.com/DracoNick0/Priority-Task-Manager/issues/77))**: Provides clear next steps and immediate actions when lists have no tasks.
2. **The 60-Second Interactive Tour ([#77](https://github.com/DracoNick0/Priority-Task-Manager/issues/77))**: Demonstrates automated scheduling concisely to new users.
3. **The Daily Rituals (Morning Kickoff & Evening Wind-Down)**: Establishes focused daily planning habits.

## 1. Actionable Empty Schedule Hero ([#77](https://github.com/DracoNick0/Priority-Task-Manager/issues/77))

When a list contains no tasks or events, Center Stage renders an **Onboarding Hero Container**:

```text
+----------------------------------------------------------------------------------------------------+
|                                                                                                    |
|                               Welcome to Priority Task Manager                                     |
|             Make a plan that fits the day you actually have. Choose an action to begin:            |
|                                                                                                    |
|    +--------------------------+  +--------------------------+  +--------------------------+        |
|    |  [*] Take a 60-Sec Tour  |  |  [+] Add Your First Task |  |  [*] Add a Fixed Event   |        |
|    |                          |  |                          |  |                          |        |
|    |  See how smart planning  |  |  Describe what needs     |  |  Record meetings, lunch, |        |
|    |  splits work, protects   |  |  doing and when it's due.|  |  or fixed appointments.  |        |
|    |  slack, and schedules.   |  |  We'll schedule the rest.|  |  Work wraps around them. |        |
|    |                          |  |                          |  |                          |        |
|    |  [ Start Tour ]          |  |  [ + New Task ]          |  |  [ + New Event ]         |        |
|    +--------------------------+  +--------------------------+  +--------------------------+        |
|                                                                                                    |
|                                   [ [*] Or load a 1-day sample schedule ]                          |
|                                                                                                    |
+----------------------------------------------------------------------------------------------------+
```

### Interactions:
- `Start Tour`: Launches the interactive scheduling demo overlay.
- `+ New Task`: Focuses the Fast-Capture field on the title input.
- `+ New Event`: Opens the event inspector form to record a commitment.
- `Load sample schedule`: Populates the current list with 3 representative sample tasks and 1 fixed event, computing an immediate schedule.

---

## 2. The 60-Second Interactive Tour Overlay ([#77](https://github.com/DracoNick0/Priority-Task-Manager/issues/77))

A lightweight 3-step carousel modal explaining the core scheduling model:

```text
+----------------------------------------------------------------------------------------------------+
|                                                                                                    |
|    +------------------------------------------------------------------------------------------+    |
|    |  HOW PRIORITY TASK MANAGER WORKS (Step 2 of 3)                             [ Skip / x ]  |    |
|    +------------------------------------------------------------------------------------------+    |
|    |                                                                                          |    |
|    |      [*] 10:00 AM [ Fixed Meeting (1h) ]                                                 |    |
|    |                  -----------------------                                                 |    |
|    |      [o] 11:00 AM [ Feature Architecture: Part 1 (1.5h) ] ---+                           |    |
|    |                  -----------------------                     | Automatically split       |    |
|    |      [*] 12:30 PM [ Lunch Break (1.0h) ]                     | around meetings & lunch!  |    |
|    |                  -----------------------                     |                           |    |
|    |      [o] 01:30 PM [ Feature Architecture: Part 2 (1.5h) ] ---+                           |    |
|    |                                                                                          |    |
|    |  The engine finds realistic time slots between meetings and rest,                        |    |
|    |  splitting demanding tasks to fit available availability.                                |    |
|    |                                                                                          |    |
|    |  (o) ( ) ( )                                     [ < Back ]    [ Next: Honest Slack > ]  |    |
|    +------------------------------------------------------------------------------------------+    |
|                                                                                                    |
+----------------------------------------------------------------------------------------------------+
```

### Tour Steps:
1. **Step 1: Effort Over Backlogs**: Focuses on effort duration and priority weights, calculating schedule placement automatically.
2. **Step 2: Intelligent Work Splitting**: Demonstrates how Gold Panning fits divisible tasks around fixed calendar commitments.
3. **Step 3: Honest Slack & Risk Warnings**: Highlights why knowing what fits and what cannot fit protects deadlines.

---

## 3. Daily Ritual: Morning Kickoff

Presents plan details when the app opens during morning work hours:

```text
+----------------------------------------------------------------------------------------------------+
|                                                                                                    |
|    +------------------------------------------------------------------------------------------+    |
|    |  Good Morning, Nicholas! Here is your plan for today                       [ Dismiss x ] |    |
|    +------------------------------------------------------------------------------------------+    |
|    |                                                                                          |    |
|    |  TODAY'S WORKLOAD SNAPSHOT:                                                              |    |
|    |  * Scheduled Work: 5.5 hours across 4 tasks                                              |    |
|    |  * Fixed Events:   1.5 hours (2 meetings)                                                |    |
|    |  * Free Buffer:    1.0 hour (Healthy margin for interruptions)                           |    |
|    |                                                                                          |    |
|    |  ! LEAST SLACK ATTENTION:                                                                |    |
|    |  "Draft Architecture Proposal" is scheduled first at 09:15 AM (Due Tomorrow 5 PM).       |    |
|    |                                                                                          |    |
|    |  [ Start My Day (Lock In) ]        [ Settings: Adjust Hours ]     [ Defer Low Priority ] |    |
|    +------------------------------------------------------------------------------------------+    |
|                                                                                                    |
+----------------------------------------------------------------------------------------------------+
```

---

## 4. Daily Ritual: Evening Wind-Down

Presents wrap-up options at or after configured workday end (`05:00 PM`). Categorizes unfinished work into deadline-critical items due before midnight and flexible items due later:

```text
+----------------------------------------------------------------------------------------------------+
|                                                                                                    |
|    +------------------------------------------------------------------------------------------+    |
|    |  Workday Complete - Evening Wrap-Up                                        [ Dismiss x ] |    |
|    +------------------------------------------------------------------------------------------+    |
|    |                                                                                          |    |
|    |  TODAY'S ACCOMPLISHMENTS:                                                                |    |
|    |  [x] Completed 3 tasks (4.5 hours of focus work)                                         |    |
|    |                                                                                          |    |
|    |  ! DEADLINE ATTENTION (Due Tonight Before Midnight):                                     |    |
|    |  * Math Homework Problem Set #4 (Effort: ~1.5h | Due: 11:59 PM)                          |    |
|    |    [ Complete Tonight (Open 2h Window) ]  [ [x] Mark Done ]  [ Postpone (Late) ]         |    |
|    |                                                                                          |    |
|    |  FLEXIBLE WORK (Due Tomorrow or Later):                                                  |    |
|    |  * History Essay Outline (Due Friday)                                                    |    |
|    |  * Client Follow-up (Due Next Week)                                                      |    |
|    |                                                                                          |    |
|    |  [ >> Push to Tomorrow Morning ]                         [ Return to Backlog ]           |    |
|    |                                                                                          |    |
|    |  [ Save & Wrap Up ]                                      [ Preview Tomorrow's Plan ]     |    |
|    +------------------------------------------------------------------------------------------+    |
|                                                                                                    |
+----------------------------------------------------------------------------------------------------+
```
