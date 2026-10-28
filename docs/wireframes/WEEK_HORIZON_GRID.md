# Wireframe: Week Horizon Grid View

## Purpose
The **Week Horizon Grid View** provides strategic planning visibility, displaying whether weekly commitments fit within regular work windows and highlighting deadline risks.

It fulfills the calendar-style timeline layout proposed in [#56](https://github.com/DracoNick0/Priority-Task-Manager/issues/56), integrates daily workload and complexity warnings ([#27](https://github.com/DracoNick0/Priority-Task-Manager/issues/27)), and provides transparent visibility into unscheduled tasks and honest deadline slack ([#40](https://github.com/DracoNick0/Priority-Task-Manager/issues/40)).

The date/time header stays pinned while its horizontal scrolling remains synchronized with the day columns. The grid always shows Monday through Sunday, even when some dates are not configured workdays. Day columns expand to use wide viewports while remaining at least 160 px wide; on narrower viewports they retain that minimum and scroll horizontally. A subtle theme tint marks the configured work-hour range on working days only; non-working days show events without a tint, capacity meter, or summary. The timeline always spans midnight to midnight, regardless of work-hour settings; the initial vertical scroll aligns the active list's work-start time with the top of the timeline viewport. Events remain visible outside work hours, while scheduled tasks remain within configured work hours and workdays.

## Visual Wireframe

The week strip shows all seven day columns and their workday-only capacity indicators. Day columns expand to use wide viewports and keep a 160 px minimum when horizontal scrolling is needed. The detailed task-block sketch below illustrates the working-day portion of the expanded tray state; weekend columns remain visible and show their events without capacity indicators. The bottom-docked tray is used at every viewport width and starts collapsed.

```text
| TIME  | MON 19 | TUE 20 | WED 21 | THU 22 | FRI 23 | SAT 24 | SUN 25 |
|       | [meter]| [meter]| [meter]| [meter]| [meter]|        |        |
|       | 6h/8h  | 7h/8h  | 6h/8h  | 6h/8h  | 4h/8h  |        |        |
```

Detailed task-block example:

```text
+-------------------------------------------------------------------------------------------------------------------------+
| HORIZON - Week of Oct 21 - Oct 27, 2026 [ < Previous Week ] [ Today ] [ Next Week > ] [ Quick Add ] [ Unscheduled & Risk (3) ] |
+-------------------------------------------------------------------------------------------------------------------------+
| TIME  | MON Oct 21      | TUE Oct 22      | WED Oct 23      | THU Oct 24      | FRI Oct 25      |
|       | [======  ] 75%  | [========] 90%  | [==========]115%| [======= ] 80%  | [====    ] 50%  |
|       | 6.0h / 8.0h     | 7.2h / 8.0h     | ! OVERLOAD 9.2h | 6.4h / 8.0h     | 4.0h / 8.0h     |
+-------+-----------------+-----------------+-----------------+-----------------+-----------------+
| 09:00 | [ Client Email] | [ Team Standup] | [ Architecture] | [ Client Email] | [ Weekly Retro] |
|       | 45m             | 30m             | Part 1 (90m)    | 45m             | 60m             |
+-------+-----------------+-----------------+                 +-----------------+-----------------+
| 10:00 | [ Spec Review ] | [ Core Refactor]|                 | [ Focus Block ] | [ Documentation]|
|       | 60m             | Part 1 (2h)     +-----------------+ Part 1 (90m)    | 2h              |
+-------+-----------------+                 | [*] Meeting     |                 |                 |
| 11:00 | [ Buffer 30m  ] |                 | Busy (60m)      |                 |                 |
+-------+-----------------+-----------------+-----------------+-----------------+-----------------+
| 12:00 | [ Lunch Break ] | [ Lunch Break ] | [ Lunch Break ] | [ Lunch Break ] | [ Lunch Break ] |
+-------+-----------------+-----------------+-----------------+-----------------+-----------------+
| 01:00 | [ API Endpoints]| [ Core Refactor]| [ Architecture] | [*] Meeting 60m | [ Free Buffer ] |
|       | 2h              | Part 2 (2h)     | Part 2 (90m)    |                 | 3h remaining    |
+-------+                 +-----------------+-----------------+-----------------+                 +
| 02:00 |                 |                 | [ Emergency Bug]| [ Focus Block ] |                 |
|       |                 |                 | 2h (High Imp)   | Part 2 (90m)    |                 |
+-------+-----------------+-----------------+                 |                 +-----------------+
| 03:00 | [ PR Review   ] | [ Buffer 45m  ] |                 |                 | =============== |
|       | 60m             |                 +-----------------+-----------------+ WORKDAY ENDS    |
+-------+-----------------+-----------------+-----------------+-----------------+-----------------+
| 04:00 | [ Buffer 60m  ] | [ Bug Triage  ] | 1.2h past hours | [ Buffer 45m  ] |                 |
|       |                 | 60m             | ! After hours   |                 |                 |
+-------+-----------------+-----------------+-----------------+-----------------+-----------------+
| 05:00 | =============== | =============== | =============== | =============== |                 |
|       | WORKDAY ENDS    | WORKDAY ENDS    | WORKDAY ENDS    | WORKDAY ENDS    |                 |
+-------+-----------------+-----------------+-----------------+-----------------+-----------------+
| UNSCHEDULED & RISK (3)                                                         [ Close ]         |
+-------------------------------------------------------------------------------------------------+
| ! AT RISK — Tax Filing (2h), due Fri 5 PM: exceeds Friday work window. [Extend Hours] [Defer]    |
| [LOCKED] BLOCKED — Deploy API (1h), waiting for Core Refactor scheduled for Wed.                |
| ! ZERO SLACK — Client Demo Prep, due Thu 3 PM; realistic slack is 0h. [Re-plan Horizon]         |
+-------------------------------------------------------------------------------------------------+
```

## Key Components

### 1. Hourly Multi-Day Time Grid ([#56](https://github.com/DracoNick0/Priority-Task-Manager/issues/56))
- **Current-Time Indicator**: A distinct horizontal line marks the current clock position in today's column anywhere within the full 24-hour timeline.
- **Synchronized Time Axis**: The pinned left-hand ruler shows all hours from midnight through 11 PM, with hour lines stretching across all day columns. Its horizontal scroll stays aligned with the day headers and timeline.
- **Full-Day Timeline**: Each day spans 00:00–24:00 regardless of working hours. Events outside the work window remain visible, while task placement continues to follow the configured work window.
- **Work-Hour Band**: A subtle background tint marks the effective work-start-to-work-end range on configured workdays; non-working days and hours outside the work window remain unshaded.
- **Proportional Time Sizing**: Tasks render as blocks sized directly according to duration (e.g. 1 hour = 60 vertical density units).
- **Split Task Badging**: Divisible tasks spanning multiple days or windows display connected part badges (`Part 1`, `Part 2`) with accent borders to visually trace continuation.
- **Fixed Event Shading**: Events render in distinct dark backgrounds with diagonal hatch marks, immediately signalling non-movable calendar commitments.
- **Event Labels**: Each event card shows its title followed by its duration, without a fixed-commitment text label.

### 2. Daily Workload & Capacity Gauges ([#27](https://github.com/DracoNick0/Priority-Task-Manager/issues/27), [#40](https://github.com/DracoNick0/Priority-Task-Manager/issues/40))
- **Sticky Day Header**:
  - Weekday and calendar date (`WED Oct 23`).
  - In-work event and scheduled task time vs. available work capacity (`6.0h / 8.0h`); event time outside the work window is excluded, and overlapping events count once.
  - Compact load bar: event time uses the same dark fill and diagonal hatch as fixed-event blocks, scheduled task time retains the green/amber/red load color based on total utilization, and free capacity uses the neutral background. Only work-hour capacity and in-work event/task time contribute; the meter fills to capacity, while the percentage still exposes genuine in-window overflow.
- **One-Click Rebalance**: When a day triggers an overload warning, clicking the header reveals options:
  - `Extend workday by 1h` (one-time list extension).
  - `Auto-rebalance schedule` (prompts Gold Panning to re-distribute divisible work into adjacent days with available capacity).

### 3. Dedicated "Unscheduled & Risk" Tray with Explainability
The bottom-docked tray displays why unplaced tasks cannot fit, identifying the exact constraint:
- **At-Risk Tasks**: Tasks requiring work outside regular hours before their deadline. Shows recommended actions (`Extend Hours`, `Defer`).
- **Blocked by Dependency**: Displays which prerequisite task is holding it up and when that prerequisite finishes.
- **Zero Slack / Tight Deadline Warnings**: Identifies tasks where realistic working-window buffer has reached zero, signaling that further delays will cause a missed deadline.

The Horizon navigation bar provides the same **Unscheduled & Risk** toggle at every viewport width. The tray is initially collapsed; when it contains tasks, the button uses alert styling and shows the distinct task count in parentheses. Pressing the button opens a height-adjustable tray below the calendar grid, sized to the Center Stage width.
