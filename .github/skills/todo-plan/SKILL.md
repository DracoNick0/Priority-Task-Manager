---
name: todo-plan
description: "Assess a backlog item (from a GitHub Issue in this repository or pasted freeform text), determine which architecture and process docs govern it, check prerequisites/blockers, and produce a scoped implementation plan before any code changes are made. Use when the user attaches or references a GitHub Issue, backlog entry, roadmap task, or issue number and asks what to do next, how to approach it, or wants a plan."
argument-hint: "a GitHub Issue reference (e.g. '#20'), backlog entry, or roadmap task (pasted text or file excerpt)"
user-invocable: true
disable-model-invocation: false
---

# TODO Plan

Use this skill to turn a backlog item into a grounded, doc-informed implementation plan — without starting implementation yet.

## Primary Sources
Always consult these first:
- `.github/copilot-instructions.md` (doc reading rules and architecture boundaries)
- `docs/DOC_INDEX.md` (canonical ownership map)
- `docs/STATUS.md` (current feature reality)
- The repository's GitHub Issues (backlog, status, prerequisites, blockers — use `github-pull-request_issue_fetch` or the `gh` CLI to read the issue and any linked/blocking issues)

## When to Use
- The user pastes, selects, or references a GitHub Issue (or an issue number) and asks what to do about it, how to approach it, or for a plan.
- The user wants to know which docs govern a piece of planned work before touching code.
- The user wants prerequisite/blocker status checked before starting an item.

## Hard Constraints
- Produce a plan only. Do not make code or documentation edits as part of this skill, unless the user explicitly asks to proceed with execution in the same request.
- Do not invent scope beyond what the issue body and its linked blocking/parent issues state.
- Do not skip prerequisite/blocker checks — never propose starting an item whose blocking issue is still open without flagging it.
- Do not fold in adjacent/unrelated backlog items even if they appear in the same tracking/parent issue.
- If the item is too vague to plan confidently, ask one focused clarifying question instead of guessing.

## Workflow
1. **Identify the item.**
   - If the pasted/selected text or number matches a GitHub Issue in this repository, fetch it: title, body (Status, Prerequisite, Implementation targets, Blockers/Dependencies, Next steps if present), labels, and any linked blocking/parent issues.
   - If it's freeform/ad hoc text not present as an issue, treat it as a standalone task and infer the closest matching category below.
2. **Classify domain(s).** An item can span more than one:
   - CLI / command handling / console output
   - Core business logic / services / task-list-profile coordination
   - Data / persistence / models
   - Scheduling / prioritization / Gold Panning / invariants
   - Integrations / API / external intake
   - Testing strategy / quality gates
   - Documentation / process / workflow
   - Roadmap sequencing only (no code impact yet)
3. **Map domains to required reading**, per `.github/copilot-instructions.md` and `docs/DOC_INDEX.md`:
   - CLI → `docs/ARCHITECTURE_CLI.md`
   - Core → `docs/ARCHITECTURE_CORE.md`
   - Data → `docs/ARCHITECTURE_DATA.md`
   - Scheduling → `docs/ARCHITECTURE_SCHEDULING.md`, plus `docs/GOLD_PANNING.md` and/or `docs/CONSTRAINT_SOLVER.md` as relevant
   - Integrations → `docs/ARCHITECTURE_INTEGRATIONS.md`
   - Testing → `docs/TESTING_STRATEGY.md`
   - Documentation/process → `docs/DOCUMENTATION_STANDARDS.md`, `docs/DOC_INDEX.md`, `docs/WORKFLOW.md`
   - Always also read `docs/STATUS.md` and the issue's full body regardless of domain.
4. **Read the mapped docs**, and where the plan needs it, the specific source files/tests they point to (e.g. `GoldPanningStrategy.cs` for scheduling items, or the relevant `Handlers/*.cs` for CLI items). Do not jump to code without the architecture doc first.
5. **Check prerequisites and blockers.**
   - If the issue (or a linked parent/tracking issue) is still open and marked as a prerequisite/blocker, say so explicitly.
   - Do not produce an execution plan that ignores an unmet prerequisite — either plan the prerequisite first or flag it back to the user and stop short of the blocked step.
6. **Detect conflicts.** If the issue text conflicts with current `docs/STATUS.md` reality or an `docs/ARCHITECTURE_*.md` boundary, surface the conflict instead of silently resolving it (per `.github/copilot-instructions.md`: "If a request conflicts with documented architecture or current status, call it out").
7. **Produce the plan** using the Output Contract below.
8. **Stop.** Do not begin implementation unless the user confirms the plan or explicitly asked for plan-and-execute up front.

## Output Contract
Return, in order:
1. **Todo Item** — restated task, with its GitHub Issue number/title if applicable.
2. **Status & Prerequisites** — current status, prerequisite/blocking issue(s), and whether each is satisfied (open/closed).
3. **Docs Consulted** — docs read, with one or two key facts/constraints pulled from each.
4. **Domain(s) & Affected Boundaries** — which architecture boundaries/components/files are touched.
5. **Plan** — numbered, concrete steps in dependency order, sized as small focused changes (per Code Quality guidance in `.github/copilot-instructions.md`).
6. **Validation** — narrowest relevant build/test command(s) to run per step or at the end.
7. **Documentation Impact** — which canonical docs (per `docs/DOC_INDEX.md`) would need updating once the work lands, or "none".
8. **Open Questions / Risks** — anything ambiguous, conflicting, or blocked that should be confirmed with the user before starting.

## Decision Gates
All must pass before finalizing the plan:
1. **Grounding Gate** — every plan step traces back to a doc or verified source file, not assumption.
2. **Blocker Gate** — no step is proposed for work whose prerequisite issue is still open; the prerequisite is proposed instead (or flagged).
3. **Scope Gate** — the plan covers only the given issue, not adjacent/unrelated backlog entries.
4. **Ambiguity Gate** — if the item lacks enough detail (no acceptance criteria, no matching issue, unclear target), ask one focused clarifying question instead of guessing.
5. **Consistency Gate** — the plan does not contradict documented scheduling invariants, architecture boundaries, or `docs/STATUS.md` current-state claims.

## Notes
- This skill produces a plan, not code changes. Pair it with normal editing tools once the plan is confirmed by the user.
- For items already tracked as GitHub Issues, prefer quoting/extending the existing "Implementation targets" / "Next steps" bullets from the issue body with concrete file-level steps rather than rewriting them from scratch.
- If the item is purely a documentation/process task, the "Plan" section should describe doc edits (scoped per `docs/DOCUMENTATION_STANDARDS.md`) rather than code changes.
