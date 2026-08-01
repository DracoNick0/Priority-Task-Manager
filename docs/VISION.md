# Vision

## Purpose
Define the long-term desired outcome for Priority Task Manager: who it serves, the problem it solves, what "success" looks like, and the milestone path from MVP to the dream product. This document answers "why are we building this and where is it going," one level above architecture and current status.

## Scope
This document describes intent and direction, independent of any specific algorithm or implementation in use today. It does not cover current implementation status (see [STATUS.md](STATUS.md)), architecture contracts (see [ARCHITECTURE.md](ARCHITECTURE.md)), or the authoritative backlog (see the repository's GitHub Issues). Where this document and current status disagree, current status wins for "is it built"; this document wins for "should we build it." Milestone contents here are a durable summary of intent, not a live tracker — GitHub Issues and their milestone fields are the source of truth for what's actually planned and in progress.

## Problem Statement
Manually planning work causes decision fatigue and stress: people must repeatedly re-decide what to work on, re-triage priorities as due dates and interruptions shift, and guess at when they'll realistically have the time and energy to do the work. Most task tools track work; they don't decide *when* the user should do it. This is especially acute for people without a fixed structure to their day — freelancers, students, and self-employed workers — who need an honest picture of how close they are to missing a deadline once real-life needs like rest are accounted for, not just a theoretically wide-open calendar.

## Target Users
General productivity users who want a planner rather than a list keeper — anyone juggling tasks with deadlines, varying importance, and limited time who wants to be told what to work on and when, not just what's outstanding. This spans people with fixed work hours and people with tentative or non-traditional schedules, who need the app to double as an honest workload and risk gauge so they can stay on top of deadlines while living a balanced life on their own terms.

## Core Differentiator
Priority Task Manager schedules and prioritizes tasks automatically, based on factors such as importance, complexity, due dates, dependencies, fixed events, and available working time, instead of leaving the user to manually triage an unordered backlog. As the product matures, scheduling should also weigh human factors like energy and peak cognitive hours, placing demanding work when the user is best equipped for it.

This is reinforced by three supporting differentiators:
- **Human-friendly intake**: turning messy real-world input into scheduled tasks with minimal manual entry, so the tool fits how users already track work instead of demanding a new system of record.
- **Trust through transparency**: the app explains why a task landed where it did, and adapts gracefully when a user overrides a suggestion or misses a scheduled block, instead of requiring the user to fight the scheduler to keep it useful.
- **Honest workload visibility**: flexibility about *when* work happens should never come at the cost of an accurate picture of *how much room is left*. For users without fixed hours, realistic default reserved time (rest, personal life) keeps slack and deadline-risk signals honest instead of assuming a naively wide-open calendar — this is what lets the app double as a life-balance tool, not just a scheduler.

## Desired End State
- **Product form**: A single scheduling core reachable from multiple clients — command-line, web, and desktop today, extending to native mobile — backed by a shared account and sync layer so a user's schedule is consistent everywhere they work.
- **Local-first data ownership**: Even as cloud sync and mobile apps mature, the user's data should remain exportable and usable offline by default — a deliberate contrast to cloud-only competitors, not just a technical fallback.
- **Protected scheduling algorithms**: the scheduling algorithms are the app's core value and should be protected accordingly. Not every algorithm needs to run on the client — some may remain offline-available (bundled with the client for baseline use), while more advanced or proprietary algorithms can be online-exclusive (executed server-side only) once the account/API layer exists.
- **Reduced cognitive load, measurably**: every feature should be evaluated against whether it reduces the number of decisions and the amount of manual upkeep required from the user, not just whether it adds capability.

## Milestones
Each milestone assumes everything in the milestones before it is retained and continues to work; later milestones add to, rather than replace, earlier ones.

### MVP — Minimum Viable Product
- A prototype scheduling algorithm that prioritizes and places tasks using importance, complexity, due dates, dependencies, fixed events, and simple day boundaries (a single configured start and end time per day).
- Usable interfaces across three surfaces: a CLI supporting both interactive menus and direct commands, a web client, and a desktop client.
- Email + password authentication, laying the foundation for future account-backed features.
- Offline local storage so the tool works without a network connection.
- LLM-assisted intake for external planning sources, with user review before anything is persisted.
- The application is packaged, deployed, and downloadable by a real user outside the development environment.

### V1 — Online Daily Planner
- A scheduling algorithm the owner is confident in as a dependable day-to-day planner, validated against representative real-world scenarios rather than judged as just a working prototype.
- Fleshed-out, polished interfaces for CLI, web, and desktop (beyond MVP-level usability).
- Native mobile apps published on the Android and iOS app stores.
- Cross-device data sync, with online storage as the backing mechanism.
- Stronger account security: two-factor authentication and OAuth/social login.
- Basic recurring/repeating tasks, so periodic work doesn't require manual re-entry.
- Scheduled-block reminders/notifications across devices, keeping the plan visible without requiring the user to keep checking the app.
- Expanded scheduling support: load warnings when a day's scheduled complexity exceeds configured thresholds, and deadline risk indicators that surface how much realistic slack remains before an at-risk task's due date.
- Algorithm protection: distinguish which scheduling algorithms remain offline-available (bundled with the client) versus online-exclusive (executed server-side only) now that networked accounts and sync exist.

### V2 — Flexible Smart Planner
- A second, meaningfully different scheduling algorithm the owner is confident in, giving users a real choice of planning styles.
- Fleshed-out, polished interfaces for the iOS and Android apps (parity with the V1 desktop/web polish pass).
- A lightweight feedback loop that compares estimated vs. actual time/complexity per task and feeds the result back into future scheduling and load-warning accuracy.
- Low-friction quick-capture surfaces (browser extension, email-to-task, voice input) so getting a task into the system is never the bottleneck.
- Expanded scheduling support:
  - Dynamic per-day work hours, with average daily capacity recalculated as availability changes rather than fixed once.
  - Flexible/open-ended day support for users without fixed work hours: schedule against the whole day using realistic default reserved time (sleep, meals, personal time) instead of a hard start/end boundary, so available capacity — and therefore slack and deadline-risk calculations — stays honest rather than assuming the full day is workable.
  - User-provided current energy level as a scheduling input.
  - User-initiated task postponement (deferring a task moves it deliberately rather than only through re-scheduling).

### Dream Product
A single, self-contained picture of the fully realized product:
- **Scheduling**: multiple interchangeable scheduling algorithms, including an optional scheduling AI trained on data contributed by consenting users; handles fixed working hours, flexible/open-ended days with honest capacity and deadline-risk visibility, dependencies, complexity, load thresholds, energy level, and user-initiated postponement; explains its placements in plain language and re-plans gracefully around overrides or missed days. Advanced or proprietary algorithms remain online-exclusive to protect the app's core value, while baseline algorithms stay offline-available.
- **Intake**: LLM-assisted intake for external planning sources, plus an optional personally-trained AI-assisted intake path — offered as an addition, not a replacement, unless it considerably outperforms the LLM path — and a feedback loop that learns from estimated vs. actual time.
- **Capture and connectivity**: two-way calendar sync (writing scheduled blocks back to external calendars, not just importing events), building on the quick-capture surfaces (browser extension, email-to-task, voice input) introduced in V2 so getting a task into the system is never the bottleneck.
- **Interfaces**: professional-grade, accessible, best-in-class UI/UX across CLI, web, desktop, iOS, and Android.
- **Accounts and data**: email+password, 2FA, and OAuth/social login; seamless cross-device sync; local-first data ownership preserved even at full maturity (offline-capable, exportable).
- **Trust**: every AI feature is strictly consent-based, scheduling decisions are transparent, and the system never requires the user to fight it to keep a plan useful.
- **Optional engagement**: opt-in, non-manipulative motivational features (for example, gentle progress reflection or streaks) explored only after the core trust and stress-reduction goals are met, and never at their expense.

## Success Definition
Success is Priority Task Manager becoming a tool the maintainer relies on daily to plan real work, because it demonstrably reduces stress and decision fatigue compared to manual planning. Broader adoption by other productivity-minded users is a welcome secondary outcome of getting the core experience right for one real user first — not a prerequisite for the project being worthwhile.

## Non-Goals
- Being a general-purpose project management tool (no team assignment workflows, no burndown/reporting suite).
- Being a passive tracker: a feature that only records tasks without helping decide when to do them is out of scope for the core experience.
- Chasing feature parity with existing tools (Todoist, Motion, Sunsama) rather than sharpening this project's own differentiator.
- Training or using AI on user data without explicit, informed consent (see Dream Product above) — consent is a hard requirement, not a detail to defer.

## How to Use This Document
- When prioritizing backlog items, prefer work that strengthens scheduling quality, reduces manual data entry, or advances the current milestone over work that only adds incremental CRUD surface.
- When a proposed feature doesn't clearly serve the problem statement, core differentiator, or current milestone above, flag the mismatch instead of building it by default.
- Use the milestone list to sanity-check GitHub Issue milestone assignments, and update this document (not the issues) if the intended milestone scope changes.
- Revisit this document when the target user, product form, core differentiator, or milestone scope materially changes — it should change rarely, unlike STATUS.md.
