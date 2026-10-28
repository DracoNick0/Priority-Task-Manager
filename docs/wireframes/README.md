# UX Wireframes and UI Architecture Guide

## Purpose
This directory contains reference text wireframes, visual ASCII mockups, and interaction models for the **Priority Task Manager UI/UX Overhaul**. These mockups serve as the visual companion to the canonical [PRODUCT_BEHAVIOR_SPECIFICATION.md](../PRODUCT_BEHAVIOR_SPECIFICATION.md).

## Core Design Principles
1. **Temporal Clarity (Today vs. Horizon vs. Backlog)**:
   - Planning and execution have distinct cognitive needs. Users have a focused view for execution today, an honest capacity grid for the week's horizon, and an organized backlog for projects.
2. **Proportional Time-Blocks (True Scheduling Visibility)**:
   - Scheduled task chunks and fixed events render with heights proportional to their duration on an hourly timeline ([#56](https://github.com/DracoNick0/Priority-Task-Manager/issues/56)). Visual space equals time.
3. **Honest Workload & Capacity Gauges**:
   - Day headers display total scheduled hours vs. available work windows with color-coded risk states ([#27](https://github.com/DracoNick0/Priority-Task-Manager/issues/27), [#40](https://github.com/DracoNick0/Priority-Task-Manager/issues/40)), maintaining realistic daily workloads.
4. **Hands-Off Flow & Frictionless Intake**:
   - The app acts as an encouraging compass. The Today view sequences tasks fluidly across offline intervals, offering 1-tap capture with smart defaults, ready to integrate with MVP LLM-assisted intake ([#18](https://github.com/DracoNick0/Priority-Task-Manager/issues/18)).
5. **Trust Through Diagnostic Explainability**:
   - When tasks cannot be scheduled or fall at risk, the UI explicitly explains the governing constraint (unmet dependencies, full work windows, deadline passed).

## Wireframe Index

| Document | Primary Focus | Key Features |
| --- | --- | --- |
| [TODAY_EXECUTION_VIEW.md](./TODAY_EXECUTION_VIEW.md) | Today's Planning Compass | Recommended priority sequence, remaining work capacity gauge, flexible offline tolerance, 1-tap push to tomorrow, evening summary |
| [WEEK_HORIZON_GRID.md](./WEEK_HORIZON_GRID.md) | Multi-Day Calendar-Style Grid | Hourly time grid ([#56](https://github.com/DracoNick0/Priority-Task-Manager/issues/56)), daily capacity meters ([#27](https://github.com/DracoNick0/Priority-Task-Manager/issues/27), [#40](https://github.com/DracoNick0/Priority-Task-Manager/issues/40)), event hashes, bottom-docked unscheduled & risk tray |
| [FAST_CAPTURE_AND_PALETTE.md](./FAST_CAPTURE_AND_PALETTE.md) | Low-Friction Intake & Command Palette | Universal `Ctrl + K` palette, clean title quick-add with smart defaults, clickable micro-chips, future LLM candidate review container |
| [EMPTY_STATES_AND_RITUALS.md](./EMPTY_STATES_AND_RITUALS.md) | Onboarding, Empty States & Daily Rituals | Actionable empty schedule hero ([#77](https://github.com/DracoNick0/Priority-Task-Manager/issues/77)), 60-second interactive tour, Morning Kickoff, Evening Wind-Down |

## Component Hierarchy & Shell Layout

```text
+--------------------------------------------------------------------------------------------------------------------+
|                                           TOP APP BAR & VIEW SWITCHER                                              |
|  [Logo] Priority Task Manager · Work  |  [ Today ]  [ Horizon ]  [ Projects ]  |  [ Cleanup ]  [ Inspector ]  [ Account ]  |
+-------------------+-----------------------------------------------------------------------+------------------------+
| COLLAPSIBLE RAIL  | ACTIVE WORKSPACE (SWITCHABLE VIEW)                                    | CONTEXTUAL RIGHT PANE  |
|                   |                                                                       |                        |
| LISTS:            | * In "Today" view:   Recommended priority sequence & capacity gauge   | (Contextual per view): |
| * Work (active)   | * In "Horizon" view: Multi-day calendar grid & capacity meters        | * Quick Intake Field   |
| * Personal        | * In "Projects":     Kanban / list backlog & dependency graph         | * Daily Capacity Gauge |
| * + New List      |                                                                       | * Unscheduled / Risk   |
|                   |                                                                       | * Detail Inspector     |
| NAVIGATION:       |                                                                       |   (When item selected) |
| * Archive         |                                                                       |                        |
| * Settings        |                                                                       |                        |
| * Engine Status   |                                                                       |                        |
+-------------------+-----------------------------------------------------------------------+------------------------+
```

Fast Capture remains available through the global `Ctrl+K` / `Cmd+K` shortcut and the inline quick-add bar; it has no separate Top App Bar button.
