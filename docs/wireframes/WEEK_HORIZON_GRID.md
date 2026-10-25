# Wireframe: Week Horizon Grid View

## Purpose
The **Week Horizon Grid View** provides strategic planning visibility, displaying whether weekly commitments fit within regular work windows and highlighting deadline risks.

It fulfills the calendar-style timeline layout proposed in [#56](https://github.com/DracoNick0/Priority-Task-Manager/issues/56), integrates daily workload and complexity warnings ([#27](https://github.com/DracoNick0/Priority-Task-Manager/issues/27)), and provides transparent visibility into unscheduled tasks and honest deadline slack ([#40](https://github.com/DracoNick0/Priority-Task-Manager/issues/40)).

## Visual Wireframe

```text
+-------------------------------------------------------------------------------------------------------------------------+
|  HORIZON - Week of Oct 21 - Oct 27, 2026       [ < Previous Week ]  [ Today ]  [ Next Week > ]   [ Quick Add: Ctrl+K ]  |
+-------------------------------------------------------------------------------------------------+-----------------------+
| TIME  | MON Oct 21      | TUE Oct 22      | WED Oct 23      | THU Oct 24      | FRI Oct 25      | UNSCHEDULED & RISK    |
|       | [======  ] 75%  | [========] 90%  | [==========]115%| [======= ] 80%  | [====    ] 50%  | (3 items need action) |
|       | 6.0h / 8.0h     | 7.2h / 8.0h     | ! OVERLOAD 9.2h | 6.4h / 8.0h     | 4.0h / 8.0h     |                       |
+-------+-----------------+-----------------+-----------------+-----------------+-----------------+-----------------------+
| 09:00 | [ Client Email] | [ Team Standup] | [ Architecture] | [ Client Email] | [ Weekly Retro] | ! AT RISK             |
|       | 45m             | 30m             | Part 1 (90m)    | 45m             | 60m             | * Tax Filing (2h)     |
+-------+-----------------+-----------------+                 +-----------------+-----------------+   Due: Fri 5:00 PM    |
| 10:00 | [ Spec Review ] | [ Core Refactor]|                 | [ Focus Block ] | [ Documentation]|   Reason: Exceeds     |
|       | 60m             | Part 1 (2h)     +-----------------+ Part 1 (90m)    | 2h              |   Friday work window  |
+-------+-----------------+                 | [*] Meeting     |                 |                 |   [ Extend Fri Hours ]|
| 11:00 | [ Buffer 30m  ] |                 | Busy (60m)      |                 |                 |   [ Defer to Next Wk ]|
+-------+-----------------+-----------------+-----------------+-----------------+-----------------+-----------------------+
| 12:00 | [ Lunch Break ] | [ Lunch Break ] | [ Lunch Break ] | [ Lunch Break ] | [ Lunch Break ] | [LOCKED] BLOCKED      |
|       |                 |                 |                 |                 |                 | * Deploy API (1h)     |
+-------+-----------------+-----------------+-----------------+-----------------+-----------------+   Blocked by:         |
| 01:00 | [ API Endpoints]| [ Core Refactor]| [ Architecture] | [*] Meeting     | [ Free Buffer ] |   Core Refactor       |
|       | 2h              | Part 2 (2h)     | Part 2 (90m)    | 60m             | 3h remaining    |   Scheduled for Wed   |
+-------+                 +-----------------+-----------------+-----------------+                 +-----------------------+
| 02:00 |                 |                 | [ Emergency Bug]| [ Focus Block ] |                 | ! ZERO SLACK          |
|       |                 |                 | 2h (High Imp)   | Part 2 (90m)    |                 | * Client Demo Prep    |
+-------+-----------------+-----------------+                 |                 +-----------------+   Realistic Slack: 0h |
| 03:00 | [ PR Review   ] | [ Buffer 45m  ] |                 |                 | =============== |   Deadline: Thu 3 PM  |
|       | 60m             |                 +-----------------+-----------------+ WORKDAY ENDS    |   [ Re-plan Horizon ] |
+-------+-----------------+-----------------+ [ Overload Rem] | [ Buffer 45m  ] |                 |                       |
| 04:00 | [ Buffer 60m  ] | [ Bug Triage  ] | 1.2h past hours |                 |                 | --------------------- |
|       |                 | 60m             | ! After hours   |                 |                 | [ Clear / Cleanup ]   |
+-------+-----------------+-----------------+-----------------+-----------------+-----------------+-----------------------+
| 05:00 | =============== | =============== | =============== | =============== |                 | COMPLETED THIS WEEK   |
|       | WORKDAY ENDS    | WORKDAY ENDS    | WORKDAY ENDS    | WORKDAY ENDS    |                 | [x] 14 tasks (22.5h)  |
+-------------------------------------------------------------------------------------------------------------------------+
```

## Key Components

### 1. Hourly Multi-Day Time Grid ([#56](https://github.com/DracoNick0/Priority-Task-Manager/issues/56))
- **Synchronized Time Axis**: Left-hand ruler shows regular working hours (e.g., `09:00 AM - 05:00 PM`) with hour lines stretching across all day columns.
- **Proportional Time Sizing**: Tasks render as blocks sized directly according to duration (e.g. 1 hour = 60 vertical density units).
- **Split Task Badging**: Divisible tasks spanning multiple days or windows display connected part badges (`Part 1`, `Part 2`) with accent borders to visually trace continuation.
- **Fixed Event Shading**: Events render in distinct dark backgrounds with diagonal hatch marks, immediately signalling non-movable calendar commitments.

### 2. Daily Workload & Capacity Gauges ([#27](https://github.com/DracoNick0/Priority-Task-Manager/issues/27), [#40](https://github.com/DracoNick0/Priority-Task-Manager/issues/40))
- **Sticky Day Header**:
  - Weekday and calendar date (`WED Oct 23`).
  - Total scheduled time vs. available work capacity (`6.0h / 8.0h`).
  - Compact visual progress bar:
    - **Green (< 85%)**: Balanced day with safe slack buffer.
    - **Amber (85% - 100%)**: High density, minimal buffer.
    - **Red / Warning (> 100%)**: Overload warning indicator (`! OVERLOAD 9.2h`). Flags that task chunks have spilled into after-hours fallback.
- **One-Click Rebalance**: When a day triggers an overload warning, clicking the header reveals options:
  - `Extend workday by 1h` (one-time list extension).
  - `Auto-rebalance schedule` (prompts Gold Panning to re-distribute divisible work into adjacent days with available capacity).

### 3. Dedicated "Unscheduled & Risk" Tray with Explainability
The right-hand tray displays why unplaced tasks cannot fit, identifying the exact constraint:
- **At-Risk Tasks**: Tasks requiring work outside regular hours before their deadline. Shows recommended actions (`Extend Hours`, `Defer`).
- **Blocked by Dependency**: Displays which prerequisite task is holding it up and when that prerequisite finishes.
- **Zero Slack / Tight Deadline Warnings**: Identifies tasks where realistic working-window buffer has reached zero, signaling that further delays will cause a missed deadline.
