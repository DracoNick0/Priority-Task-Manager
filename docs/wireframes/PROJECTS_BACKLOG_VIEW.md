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
| | o-- [ ] Step 1  Critical Path                        [···]             |   |
| |     :: Launch initiative [Ready to Schedule]        [+ Follow-Up]     |   |
| | |                                                                     |   |
| | +------- [ ] Step 2  Critical Path                    [···]             |   |
| | |         :: Design [Blocked by: Launch]              [+ Follow-Up]     |   |
| | `------- [x] Step 2                                   [···]             |   |
| |           :: Research [Completed]                     [+ Follow-Up]     |   |
| |          `------- [ ] Step 3  Critical Path            [···]             |   |
| |                    :: QA [Blocked by: Design]          [+ Follow-Up]     |   |
| +-----------------------------------------------------------------------------+   |
| [ Drop to detach from chain (make standalone) ]  (while dragging a linked task)   |
|                                                                                   |
| Ready & Active (1)                                                                |
| [ ] Finalize budget       [Ready to Schedule]                 [+ Follow-Up]        |
| Someday / Flexible (1)                                                            |
| [ ] Explore ideas         [Ready to Schedule]                 [+ Follow-Up]        |
|                                                                                   |
| > Completed Tasks (2)                                  [expand to inspect/undo]   |
+-----------------------------------------------------------------------------------+
```

Step numbers indicate prerequisite depth. Siblings share a step number, and
multiple prerequisites can feed one dependent. Completed steps remain visible
inside their chains for context; the collapsed Completed Tasks section also
collects every completed task. A circular dependency displays a warning and
each task at most once rather than looping forever.
On one-off cards the `::` drag handle sits below the checkbox, `Critical Path`
follows `Step N` on the same line, and the `+ Follow-Up` button sits at the
bottom right. The `···` actions menu remains available at narrow widths.

The vertical rails continue through intervening rows and turn smoothly into
horizontal branches at the dependent card. Each visible prerequisite gets a
distinct incoming point, with equal vertical spacing. A task with visible
dependents shares one outgoing rail and circle one spacing below its last
incoming point; with no incoming points, the outgoing point is centered.
Leaves show no outgoing stub or circle. Search and hide-completed remove hidden
edges without leaving phantom connectors. Hovering a branch identifies and
highlights that link; there are no `✕` buttons on the rails. Remove individual
links using prerequisite or follow-up chips in the inspector.
The step-chain layout remains intact when branches cross: the horizontal branch
continues, while an unrelated vertical rail has a small gap at the crossing.
For example, with A -> B, A -> C, and both B and C -> D, the A -> C branch
crosses B's rail without joining it; B -> D similarly passes C's rail.
Actual prerequisite-to-dependent turns and shared outgoing rails remain joined.

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
| Blocked | Amber `Blocked by: [titles]` lock badge | One or more prerequisites are incomplete. |
| Completed | Green checkmark and struck-through title | The task is complete; its dependents update their readiness. |

- Tap a card to open its task in the Right Inspector. Its checkbox can complete
  or reopen an eligible one-off task; blocked tasks are not marked complete
  directly from the backlog.
- One-off task cards have a drag handle that starts dragging as soon as it
  moves; pressing and holding elsewhere on the card still starts a drag.
  Drop it onto another card to make it a follow-up of the target task. The target
  highlights the proposed action; self-links, duplicate links, cycles, and
  recurring-task targets show a forbidden indicator and reject the drop.
  Recurring series and occurrences cannot be dragged. Dropping a task with
  incoming links into the detach zone below the chains removes all its
  prerequisites so it can return to the standalone backlog.
- Both drag-to-link and drag-to-detach notify with **Undo**. Undo removes just
  the newly added link or restores the previous prerequisites without
  overwriting unrelated edits; if intervening changes make restoration unsafe,
  it refuses the restore and explains why.
- Every card's **···** menu offers **Link Prerequisite...** and
  **Link Follow-Up...** (the same cycle-aware picker), **Reopen** for completed
  one-off tasks, and **Archive Task**. Completion uses the card checkbox,
  not the menu. A task with incoming links can also
  **Detach from Chain**; this removes its prerequisites with Undo while
  retaining any tasks that depend on it. Chain roots show why detach is
  unavailable. Blocked and recurring tasks cannot be completed by the
  checkbox. Archiving a one-off task requires an authenticated account;
  Guests are told to sign in, and recurring tasks open the inspector so the
  user can choose the occurrence or series scope safely.
- **+ Follow-Up** directly opens **Link Existing Follow-Up**, which adds this
  task to an existing task's prerequisites. The picker excludes recurring and
  already-linked tasks and disables links that would form a cycle. The usual
  new-task controls remain available elsewhere in the workspace.
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
