# Product Behavior Specification

**Status:** Draft — product decisions are still open
**Scope:** Intended user-facing behavior for accounts, settings, views, lists, tasks, events, and the archive

## Purpose

This document describes what users should be able to do and what the product should do in response. It is the target-behavior reference for the features in scope, not a statement that every behavior is currently available.

Use user stories to explain the user's goal and feature descriptions to define intended behavior. Acceptance criteria capture distinct, observable outcomes for important interactions and edge cases; they are not a second checklist of every stated capability. Empty cells and unanswered questions are intentional; they identify behavior that has not been decided rather than inviting an implementation guess.

For what is currently available, see [STATUS.md](STATUS.md). For the long-term product direction, see [VISION.md](VISION.md). Scheduling invariants and algorithm details belong in [ARCHITECTURE_SCHEDULING.md](ARCHITECTURE_SCHEDULING.md) and its linked specifications.

## Main interaction pattern

- The **Left Rail** is where a user switches lists and opens app-wide areas such as Settings and Archive.
- The **Center Stage** shows the current schedule or a plain task list, depending on the user's access and selected view.
- Selecting a task, event, or list opens it in the **Right Inspector** for viewing and editing. Global defaults are edited from Settings; list settings are edited in the context of that list.
- Archive actions are available from the Archive view and clearly distinguish restore from permanent deletion.
- At narrow window widths, navigation and editing remain available through pane drawers rather than disappearing.

## Session and account access

- The application starts in Guest mode; users are not forced to register or log in.
- Guest users can manage their task, list, event, recurrence, and archive data locally. Free accounts can manage these data features as well. These are not subscription-only features.
- A Guest can choose to create an account later, which carries the Guest's data into the new account. Signing in to an existing account keeps Guest data separate until the user chooses whether to merge it.
- Computing a schedule is an authenticated Subscription capability. Guests can still use their plain, sortable task list but do not receive a computed schedule.
- Account/Profile provides account credential controls, profile details, subscription controls, and account deletion.

### Account deletion

A signed-in user can request account deletion from Account/Profile after reviewing the affected data and confirming. The account remains usable during a minimum 30-day recovery period, and the user can cancel deletion explicitly from Account/Profile. A deletion request immediately cancels Subscription renewal, but existing paid access continues through its paid-through date. If deletion is not canceled, the account profile and all account-owned product data, including tasks, lists, events, settings, and archived data, are permanently deleted no earlier than 30 days after the request or the Subscription paid-through date, whichever is later. Subscription access ends at the paid-through date; the account remains usable without Subscription access until deletion or cancellation. Canceling deletion does not reinstate the canceled Subscription, and the user must explicitly resubscribe after paid access ends. Separate Guest data on the device is not deleted.

### Account and session acceptance criteria

- Creating a new account transfers the Guest's tasks, lists, and events; signing in to an existing account keeps that Guest data separate and available until the user chooses whether to merge it.
- While deletion is pending, the account remains usable. The user can cancel deletion explicitly in Account/Profile; canceling does not reinstate the canceled Subscription.
- If deletion is not canceled, account data is permanently removed no earlier than 30 days after the request or the Subscription paid-through date, whichever is later; separate Guest data remains.

## Credentials and account security

The initial account sign-in method is a verified email address and password. The email address identifies the account; it is not a public username. Users can create an account, verify its email address, sign in with email and password, and manage credentials through Account/Profile. Passwords are private account secrets, separate from task, list, event, and work-availability data; they must not appear in schedule results or be visible to other users.

### Password recovery

A user who forgets a password can request a reset link for the account's verified email address. The response to the request must not reveal whether an account exists. A reset link is single-use and expires after a limited time. After following a valid link, the user chooses a new password; the application does not send or reveal the existing password or issue a reusable temporary password. A successful reset invalidates existing signed-in sessions and notifies the account email address. Recovering an account without access to its verified email is not supported by this initial flow.

Users who are already signed in can change their password after verifying control of the account. Credential recovery methods beyond the verified-email reset flow, including passkeys, social sign-in, SMS recovery, and support-assisted recovery, are not part of the initial credential model.

### Credential acceptance criteria

- A password-reset request gives the same user-facing response whether or not the address belongs to an account.
- A reset link succeeds only once and before it expires; after a successful reset, existing signed-in sessions are invalidated and the account is notified.
- Credential changes require verification that the user controls the account; passwords do not reappear in the UI or in task, list, event, work-availability, or schedule data.

## Core user outcome

> As a user managing work and commitments, I want to describe what I need to do, when I can work, and what constrains my time so the product can produce a useful schedule and make work that does not fit visible.

### Schedule behavior established by this draft

- Each day of the week can have its own regular work hours, with one or more local-time work windows. Work windows are configured separately in global defaults and each list's settings.
- Events are account-wide commitments, not list-owned items, and must be considered when scheduling any list. Busy and Tentative events block work time; Free events do not. Pinned tasks are fixed commitments that other tasks are scheduled around, and events and pinned tasks remain visible even outside regular work hours.
- A task must not be scheduled before its **Not before** time or before its prerequisites are complete. If a prerequisite cannot be scheduled, its dependent tasks—and all downstream tasks in that dependency chain—remain unscheduled, even if they otherwise fit in available time.
- A task's due date is its latest-finish target. A date-only deadline is the default and means the end of that calendar date; users may instead specify an exact time.
- Regular work windows are used first. If lower-priority work cannot be completed in those windows before its deadline, the scheduler may use any otherwise-free time outside the windows before the deadline, including time before the first work window. Such work is marked **At risk**. If the full task still cannot fit, the scheduler places as much work as its divisibility setting allows and shows the remainder as at risk; if no work can be placed before the deadline, the task is **Unscheduled**. No work is scheduled after the deadline.
- Work that cannot be placed remains visible to the user rather than silently disappearing.
- When a user saves a task, event, or scheduling-setting change, the schedule refreshes automatically; the user can also request a refresh manually.
- **Gold Panning** is the only scheduling algorithm in the initial offering and is used by default. A choice among algorithms only becomes useful if another one is offered.
- The user-facing setting label is **Scheduling algorithm**. Project architecture may continue to use **strategy** when describing the scheduling approach internally.
- A multi-list view has its own scheduling algorithm, which ranks eligible tasks from all included lists together. Each task remains subject to its list's work hours and other list-specific settings. All included lists share one non-overlapping user timeline; overlapping availability does not add capacity.
- Users can sort tasks by alphabet, due date, importance, complexity, estimated duration, or urgency, ascending or descending, only in a plain task-list view: for Guests or authenticated users viewing all tasks without a schedule. Sorting does not reorder a computed schedule. Ties use a fixed task-ID order; users cannot choose the tie-breaker.

## Views

**User story:** As a user, I want to see my schedule in a format that suits how and where I plan so I can move between a quick chronological view and a date-oriented view.

Users can switch among three schedule views:

| View | Intended user-facing behavior |
| --- | --- |
| Schedule Board | The current hybrid view: daily columns show scheduled task and event blocks, including pinned tasks and events outside regular work hours, with Unscheduled and Completed areas for work outside the timed schedule. |
| Agenda | A compact chronological presentation of upcoming scheduled items, especially suited to mobile screens. It visibly indicates when unscheduled tasks exist and provides a direct way to view them. |
| Calendar | A calendar-grid presentation, especially suited to desktop. The user can browse scheduled items by date; the view visibly indicates when unscheduled tasks exist and provides a direct way to view them. |

All three views provide access to Unscheduled and Completed tasks; they need not show those items in the main chronological, calendar, or board layout. Each view indicates when unscheduled tasks exist and provides a direct way to open the Unscheduled task view, plus a direct way to open Completed tasks. A plain, sortable task-list view is also available where no computed schedule is shown, including for Guests. Sorting a plain task list does not change the computed schedule.

### View acceptance criteria

- Switching among Schedule Board, Agenda, and Calendar preserves the current list scope and included-list selection without changing task, event, or schedule data.
- A schedule view makes its list scope visible and lets users review or change a multi-list view's membership.
- Schedule Board, Agenda, and Calendar display pinned tasks and events even when they fall outside regular work hours.

## Settings

### Defaults and profile settings

Users can configure defaults for the following settings. Lists snapshot the current defaults by default or can inherit global defaults; their settings can be changed independently.

| Setting | Intended user-facing behavior |
| --- | --- |
| Sort | A user can choose alphabetic, due-date, importance, complexity, estimated-duration, or urgency sort, each ascending or descending. Sorting is only for a plain task-list view (Guest or authenticated all-task view), not a computed schedule. Equal values use a fixed task-ID order. |
| Scheduling algorithm | Gold Panning is the only available algorithm initially and is used by default. The setting becomes a user choice if additional algorithms are introduced. |
| Work hours | Each day of the week can have one or more local-time work windows that define regular availability for scheduling. |
| Work days | The selected weekdays define regular work availability; each selected day can have different hours or multiple windows. A one-time extension to today's hours may continue past midnight into the next calendar day. |
| Location | A default location can prefill new tasks and events; individual items can have their own location. Users may optionally enter one-way travel duration and choose whether it is reserved as non-working time before the item, after it, or both before and after. When both are selected, the entered duration applies on each side. A future map action may open directions, and a future integration may calculate travel duration, particularly for events. |
| Urgency thresholds | Urgency is based on work-time slack: available work time before the due date compared with the task's remaining estimated effort. Thresholds determine both the user-facing urgency level and the scheduler's pre-deadline buffer. Exact numeric thresholds and buffer lengths are still open. |
| Account/profile | Users can access account credentials, profile details, and subscription controls. The specific profile fields and subscription actions are not defined here. |

**Default inheritance and updates:** Lists default to snapshotting the global defaults in effect when they are created. A user can instead choose for a list to inherit global defaults. Later global-default changes affect new lists and inheriting lists, but not snapshot lists. A user can change a list's settings independently.

**Overlapping work windows:** For one list, overlapping or directly adjacent work windows are treated as one continuous availability interval, not counted twice. In the multi list view, each list's windows still determine which tasks are eligible; when windows from different lists overlap, tasks compete for the same user-time slot on one timeline. The scheduler never adds overlapping list windows together as extra capacity.

**Quick work-hours extension:** While viewing a list, a user can enter a number of additional work hours for that day. This extends that list's current or previous work window for the selected date by the entered duration. The interval can cross midnight into the next calendar day even if that day is not normally a workday. Overlapping or directly adjacent windows for the same list are merged, never counted twice. Only the future portion is available for scheduling. It is a one-time, list-scoped override and does not change saved list or profile hours. In the multi list view, the extension makes tasks from that list eligible during the added interval; tasks from other lists remain governed by their own work windows and all tasks share the same non-overlapping timeline.

**Recurring work windows and exceptions:** A recurring work window is associated with the weekday on which it starts; an overnight window can end on the following day. Removing one occurrence of a recurring window creates an exception for that occurrence without removing the recurring rule. Time off is represented by removing the applicable work-window occurrence(s) through exceptions; it does not create a separate blocking event. Blocking events remain independent and continue to block work time.

**Time zones:** Work windows, task deadlines, and event times use the user's current local time zone. Account- and list-specific time-zone overrides are not offered. How saved recurring rules behave if the user's time zone changes, and how daylight-saving gaps or repeated local times are handled, remain undecided.

**Access to scheduling:** Guests can use a plain task list but do not receive a computed schedule. Scheduling is an authenticated Subscription capability. Multi-list scheduled views follow the same access rule.

### Settings acceptance criteria

- When a global default changes, new and inheriting lists use it while snapshot lists retain their existing value; editing one list's setting does not change global defaults or other lists.
- A recurring work window that crosses midnight belongs to its start weekday, and removing one occurrence leaves the recurring rule in place.
- Overlapping or directly adjacent work windows for the same list form one continuous interval rather than extra capacity.
- A quick work-hours extension changes only the selected list's future availability, merges with overlapping time, and can cross midnight without making elapsed time available.

### Simulated time

Simulated time is a developer-only setting, saved per list. It previews how the schedule and urgency would look at a selected date and time without changing actual task or event dates. It is not an end-user planning control.

**Simulated-time acceptance criterion:** Changing a list's simulated time changes only the developer preview; it does not change persisted task/event dates, completion state, or real current time.

## Lists

| Feature | Intended user-facing behavior |
| --- | --- |
| Name and description | A user can identify a list with a name and optional description. |
| List settings | A list has its own sort, scheduling algorithm, work hours, work days, location, and urgency thresholds. It snapshots current defaults at creation by default, or can inherit global defaults; settings can be independently edited afterward. Sort applies only to plain task-list views. |
| Simulated time | Developers can save a simulated-time override for a list. It does not alter real dates or appear as an end-user planning control. |
| Archive and restore | A user can archive a list, then restore the list from the Archive. Its tasks remain associated with it. |
| Permanently delete | From the Archive, a user can permanently delete an archived list and all tasks belonging to it, after explicit confirmation. Account-wide events are not deleted with the list. |
| Multi-list views | A user can create and manage named views combining multiple lists/schedules on one non-overlapping timeline. The user explicitly chooses which lists each view includes and which scheduling algorithm the view uses to rank eligible tasks together. New views do not automatically include all lists, and creating a new list does not automatically add it to existing views. Each task uses its own list's availability and other list-specific settings. |

**User story:** As a user with different kinds of work, I want separate lists with their own settings so each list can be planned appropriately.

### List acceptance criteria

- A newly created or edited multi-list view includes only its explicitly selected lists; creating another list does not add it to an existing view.

## View-scope acceptance criteria

- Running cleanup in a single-list or multi-list view archives every completed task in that scope, including tasks outside the visible date range, as one cleanup group.

## Tasks

**User story:** As a user, I want to capture a task's effort, deadline, importance, and constraints so that it can be considered in my schedule.

### Task details

| Attribute | Intended user-facing behavior |
| --- | --- |
| Completion | A user can mark a task complete or return it to incomplete. Completed tasks are no longer treated as pending work. |
| Title and description | A task has a title and may have a description. The title cannot be empty. |
| Due date | The due date is the latest-finish target. A date-only deadline is the default and means the end of that calendar date; a user may instead specify an exact time. The user's current local time zone defines that date or time. If work cannot be completed within regular work windows before the deadline, otherwise-free time outside those windows may be used and the task is marked at risk. The selected algorithm determines how work is prioritized toward the deadline. |
| Estimated duration | The estimate is the expected amount of work, in minutes. Scheduled parts of a divided task should add up to the estimate when the full task is schedulable. |
| Importance | A 1–10 rating, where 10 is most important. How importance affects ranking and placement is specific to the selected scheduling algorithm. |
| Complexity | A 1–10 rating, where 10 requires the most focus, describes cognitive effort rather than duration. How complexity affects ranking and placement is specific to the selected algorithm; an algorithm may prefer higher-complexity work earlier when appropriate. See the [Complexity Guide](COMPLEXITY_GUIDE.md). |
| Urgency | Urgency is based on work-time slack: available work time before the due date compared with the task's remaining estimated effort. Its effect on ranking and placement is specific to the selected algorithm; numeric thresholds and pre-deadline buffers remain open. |
| Recurrence | A task can repeat on a daily, weekly, monthly, yearly, or explicitly selected-date pattern, with an end condition of never, after a number of occurrences, or by a date. A monthly occurrence whose day number does not exist in a month falls on that month's last day. Each recurring task has a progression behavior chosen by the user: wait for each occurrence to be completed before progressing, roll forward while retaining missed occurrences as backlog, or roll forward while disregarding missed occurrences. Recurrence is available for Guest, Free, and Subscription users. The user can edit or delete one occurrence, that occurrence and following occurrences, or the whole series. |
| Location | A task can have its own location or use the default location. A user may optionally provide one-way travel duration and choose whether to reserve it immediately before the task, after it, or both before and after. When both are selected, the entered duration applies on each side. Future ideas include opening map directions and calculating travel duration automatically. |
| Link | A task can have a website or meeting link. Selecting the link opens it using the device's normal link handler. |

### Scheduling constraints and advanced settings

| Attribute | Intended user-facing behavior |
| --- | --- |
| Divisible | When enabled, a task may be split into scheduled parts that are at least 15 minutes long by default; the minimum is adjustable per task. When disabled, it must remain one uninterrupted block. |
| Not before | A task cannot be scheduled before its specified date and time. If Not before is later than the due date, the product rejects the conflicting input and tells the user to correct it. |
| Before/after padding | The user can set and change before-padding and after-padding independently. Each reserves unavailable time immediately before or after the task and does not count toward estimated work duration. The same rule applies to events. A future travel-time feature may adjust these buffers based on location. |
| Pinned | A user can pin a task to a chosen time slot. It behaves as a fixed commitment: the scheduler does not move it, other tasks are scheduled around it, and it may fall outside regular work hours. Pinned tasks remain visible there and can be completed. If it overlaps a Busy or Tentative event or another pinned task, both remain at their chosen times and the conflict is flagged; no other work is placed in the overlap. Being outside regular work hours alone does not make a pinned task at risk. |
| Dependencies | A task can depend on other tasks. A dependent task can be scheduled only after all prerequisites are complete. If a prerequisite cannot be scheduled, the dependent and every downstream task that depends on it, directly or indirectly, remain unscheduled. |
| List | A task belongs to a list, and the user can change its associated list. It is planned using its current list's effective settings. |

### Cleanup and deletion

| Action | Intended user-facing behavior |
| --- | --- |
| Cleanup | Cleanup archives every completed task in the current view's list scope, not only the items visible in the current date range: the current list in a single-list view, or all included lists in the multi list view. Tasks archived by one cleanup action are grouped together in the Archive. |
| Archive task | A user can archive an active task individually. An active task cannot be permanently deleted directly. |
| Recurring-task edits and archive | The user can choose one occurrence, that occurrence and following occurrences, or the whole series. Archived occurrences from the same recurring series are grouped together in the Archive. |

### Task acceptance criteria

- Moving a task to another list preserves its other attributes, and the destination list's effective settings govern its subsequent schedule.
- If a prerequisite remains unscheduled, every downstream dependent remains unscheduled even if it would otherwise fit in an available slot.
- When work cannot fit within regular work windows before a deadline, the scheduler uses otherwise-free time outside those windows for lower-priority work and marks it at risk. It places only the work allowed by the task's divisibility setting, flags any remaining work, and marks the task unscheduled if no work can be placed before the deadline.
- A pinned task remains at its chosen time even outside regular work hours, and other tasks are scheduled around it. If it overlaps a blocking event or another pinned task, both fixed items remain visible with a conflict flag and no other work is placed in the overlap.

## Events

**User story:** As a user, I want to record commitments so that my schedule accounts for time I cannot use for tasks.

| Attribute or action | Intended user-facing behavior |
| --- | --- |
| Title and description | An event has a title and may have a description. The title cannot be empty. |
| Start and end | An event reserves its specified start and end time, interpreted in the user's current local time zone. |
| Recurrence | An event can repeat on a daily, weekly, monthly, yearly, or explicitly selected-date pattern, with an end condition of never, after a number of occurrences, or by a date. A monthly occurrence whose day number does not exist in a month falls on that month's last day. The user can edit or delete one occurrence, that occurrence and following occurrences, or the whole series. |
| Location | An event can have its own location or use the default location. A user may optionally provide one-way travel duration and choose whether to reserve it immediately before the event, after it, or both before and after. When both are selected, the entered duration applies on each side. Future ideas include opening map directions and calculating travel duration automatically, particularly for events. |
| Link | An event can have a website or meeting link. Selecting the link opens it using the device's normal link handler. |
| Status | The user can set or change an event's status to **Free**, **Tentative**, or **Busy**. Busy and Tentative events block task scheduling; Free events do not. |
| Before/after padding | The user can set and change before-padding and after-padding independently. Each reserves unavailable time immediately before or after the event, in addition to the event's start/end interval. |
| Reminder | A user can choose a reminder lead time for an event. Reminders are off unless set, and a set reminder is delivered as a device notification. |
| Archive | A user can archive an event, one occurrence, or a selected recurring-series scope. Archived events can be restored or permanently deleted from the Archive. |

### Event acceptance criteria

- Changing an event's status updates task availability: Busy and Tentative events block placement, while Free events do not.
- A configured reminder produces a device notification at the selected lead time; with reminders off, no reminder is sent.
- Editing or archiving one scope of a recurring event affects only that scope, and archived occurrences remain grouped by recurrence origin.

## Archive

| Feature | Intended user-facing behavior |
| --- | --- |
| Restore | A user can restore an archived task, event, list, or group. Tasks return to their original list when it exists; if the original list has been permanently deleted, the user chooses a destination list. Restored tasks retain their completion state. Restoring an archived list restores the list and its associated tasks. |
| Permanent delete | A user can permanently delete an archived task, event, or group. Permanent deletion requires confirmation and is not available from active task or event views. |
| Groups | Archived tasks and events from the same recurrence origin are grouped together. Tasks archived together by one cleanup action are grouped as one cleanup batch. If a cleanup batch includes recurring occurrences, the batch remains a single group and its occurrences retain their recurrence-origin grouping within it. |
| Archived lists | Archiving a list retains the list and its tasks together. A user can restore the list and its tasks, or permanently delete the archived list and all its tasks after confirmation. Events are account-wide and are not deleted with a list. |

### Archive acceptance criteria

- A cleanup batch containing recurring occurrences remains one group while preserving each occurrence's recurrence-origin grouping; restoring or deleting the batch applies to its contents.
- If a task's original list has been permanently deleted, restoration cannot complete until the user selects an existing destination list.
- Restoring a completed task returns it from the Archive without changing its completed state.

## Cross-feature acceptance criteria

- Across included lists, task placements do not overlap or double-count work-window capacity; after-hours fallback uses only otherwise-free time.
- Before confirming a destructive action, the user is told which data it will affect.

## Open product decisions

The following behavior remains undecided and should not be inferred during implementation.

| ID | Question | Answer |
| --- | --- | --- |
| P-01 | What numeric work-time-slack thresholds and pre-deadline buffer lengths should correspond to each urgency level? | Deferred for a later product decision. |
| P-02 | When the user's current local time zone changes, how should saved recurring work windows, task deadlines, and event times be interpreted? | Deferred for a later product decision. |
| P-03 | How should recurring work windows and event occurrences handle local times that do not exist or occur twice during daylight-saving transitions? | Deferred for a later product decision. |
| P-04 | When a Guest chooses to merge data into an existing account, how should duplicate records and other conflicts be resolved? | Deferred until the merge flow is designed. |
| P-05 | Should users be able to export account data, and if so, what data and formats should be included? | Deferred for a later product decision. |
