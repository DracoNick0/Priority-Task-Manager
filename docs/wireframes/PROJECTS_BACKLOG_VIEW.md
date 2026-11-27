# Projects Backlog & Dependency Graph

The **Projects** tab is the active list's structural task view. Unlike Today
and Horizon, it does not display event cards or assign tasks to calendar slots.
See the [product behavior specification](../PRODUCT_BEHAVIOR_SPECIFICATION.md#views)
for acceptance criteria.

## Wide layout

```text
+-----------------------------------------------------------------------------------+
| [ Today ] [ Horizon ] [ Projects ]        Projects & Dependency Backlog          |
| [ Search tasks by title...                                   ]                   |
| [ All ] [ Ready ] [ Blocked ] [ Someday ] [ Completed ]                          |
| Total: 8       Ready: 4       Blocked: 2       Completed: 2                      |
|                                                                                   |
| Dependency Step-Chains (1)                                                        |
| +-----------------------------------------------------------------------------+   |
| | [v] Launch initiative       1 of 4 completed (25%)                       |   |
| | [#####---------------]  Total: 3h 30m • Remaining: 2h 30m                |   |
| | [ ] Hide completed steps                                    4 steps       |   |
| | o-- Step 1  [ ] Launch initiative [Critical Path] [+ Add Follow-Up]       |   |
| | |                                                                     |   |
| | +----✕-- Step 2  [ ] Design [Critical Path] [Blocked by: Launch]           |   |
| | |                  Waiting on: Launch initiative  [+ Add Follow-Up]        |   |
| | `----✕-- Step 2  [x] Research      [Completed]  [+ Add Follow-Up]         |   |
| |          `----✕-- Step 3  [ ] QA [Critical Path] [Blocked by: Design]      |   |
| +-----------------------------------------------------------------------------+   |
|                                                                                   |
| Ready & Active (1)                                                                |
| [ ] Finalize budget       [Ready to Schedule]                 [+ Add Follow-Up]    |
| Someday / Flexible (1)                                                            |
| [ ] Explore ideas         [Ready to Schedule]                 [+ Add Follow-Up]    |
|                                                                                   |
| > Completed Tasks (2)                                  [expand to inspect/undo]   |
+-----------------------------------------------------------------------------------+
```

Step numbers indicate prerequisite depth. Siblings share a step number, and
multiple prerequisites can feed one dependent. Completed steps remain visible
inside their chains for context; the collapsed Completed Tasks section also
collects every completed task. A circular dependency displays a warning and
each task at most once rather than looping forever.

The vertical rails continue through intervening rows and turn smoothly into
horizontal branches at the dependent card. Each prerequisite has its own rail
and an `✕` near the dependent card, including when several prerequisites feed
one task. Hovering over a branch or its `✕` identifies the exact link and
highlights it. Removing a link updates the chain and offers **Undo** in a
notification; undo is refused if the tasks have changed such that restoring
the link would make a cycle.

Each chain header reports completed steps, a progress bar, total estimated
effort, and remaining effort excluding completed steps. The chevron collapses
the chain to its title and completion summary. **Hide completed steps** is
independent per chain: it removes completed cards from that chain's rail
without changing progress or completion counts, and keeps unfinished steps
visible at their original step numbers.

The longest root-to-leaf route of **remaining estimated effort** in each
acyclic chain is outlined with a primary accent, marked **Critical Path**, and
has accent-colored connector segments. Completed steps contribute zero effort;
an entirely completed chain or a dependency loop has no highlighted route.
Only links on the chosen route receive the accent, not shortcuts between its
tasks. This is a backlog estimate, not the scheduler's placement or urgency.

Search matches task titles without regard to case. **All**, **Ready**,
**Blocked**, **Someday**, and **Completed** filter the visible task cards while
the summary counters and chain progress remain list-wide. Ready excludes the
separate Someday / Flexible backlog. A matching chain shows only its matching
steps; hidden prerequisites do not leave empty rail branches. The Completed
filter opens the Completed Tasks section and does not repeat completed cards
in chains. Switching lists resets the search and selected filter.

## States and interactions

| State | Card treatment | Meaning |
| --- | --- | --- |
| Ready to Schedule | Green readiness badge | No incomplete prerequisite in the active list. This does not guarantee a calendar slot. |
| Blocked | Amber lock badge and `Waiting on: [titles]` | One or more visible prerequisites are incomplete. |
| Completed | Green checkmark and struck-through title | The task is complete; its dependents update their readiness. |

- Tap a card to open its task in the Right Inspector. Its checkbox can complete
  or reopen an eligible one-off task; blocked tasks are not marked complete
  directly from the backlog.
- **Add Follow-Up** offers **Create New Follow-Up**, which opens a new task in
  the inspector with this one-off task preselected as its prerequisite, and
  **Link Existing Task...**, which adds this task to an existing task's
  prerequisites. The picker excludes recurring and already-linked tasks and
  disables links that would form a cycle.
- The task inspector separates **Prerequisites** (this task waits on) from
  **Follow-ups** (tasks waiting on this task). Each list has a link picker and
  removable chips. Links on existing tasks save immediately; a new task's
  selected prerequisites are saved with the task. Revalidation before saving
  prevents stale selections from introducing a cycle or recurring-task link.
- Standalone tasks without a due date or with low importance (1-3) appear in
  **Someday / Flexible**. Other independent incomplete tasks appear in
  **Ready & Active**.
- Guests see the same chains and backlog from their local tasks. Their green
  badge says **Ready for manual execution**; it does not require or imply
  automated scheduling. Authenticated users see API-backed tasks.
- At narrow widths, the chain layout remains vertical and indented, and
  selecting a task or follow-up opens the inspector drawer.
