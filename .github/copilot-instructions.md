applyTo: '**'

# Copilot Instructions for Priority Task Manager

## Default Workflow
- Before coding, review `docs/STATUS.md` and `docs/ARCHITECTURE.md`; use the architecture map to select the focused `docs/ARCHITECTURE_*.md` guidance. If the request conflicts with documented architecture or current status, explain the conflict and propose an aligned path.
- For a GitHub issue or feature/fix request, use `.github/skills/slice/SKILL.md` and plan first, even if implementation is requested. Define ordered, coherent segments with acceptance criteria, focused checks, and manual user-test steps when useful. Keep implementation, tests, and relevant documentation together; split only at meaningful review boundaries, not by file or size.
- Stop after the plan. Each `proceed` authorizes exactly the next segment: implement and verify only that segment, report results and useful manual test steps, then stop for review. Apply feedback to the current segment before advancing. If no plan is active, ask what to proceed with. The user may explicitly bypass checkpoints; `proceed` does not authorize staging or committing.
- See `docs/WORKFLOW.md` for the full development process.

## Project Guardrails
- Core business logic belongs in `PriorityTaskManager/` and must not depend on UI behavior. `TaskManagerService` coordinates task/list/profile operations and delegates prioritization through `IUrgencyStrategy`; scheduling logic belongs under `PriorityTaskManager/Scheduling/**`.
- `PriorityTaskManager.CLI/` is archived. Do not modify, build, or test it unless the user explicitly requests restoration.
- Preserve documented scheduling invariants; distinguish implementation defects from incorrect test expectations and use deterministic time for time-sensitive behavior. Follow `docs/TESTING_STRATEGY.md` and run the narrowest relevant checks, reporting anything not run and why.
- Use canonical terminology from `docs/TERMINOLOGY.md`, especially Guest mode vs. Free account vs. Subscription tier, Scheduling mode vs. Strategy, and Global defaults vs. List-scoped settings.

## Documentation
- Follow `docs/DOC_INDEX.md` and `docs/DOCUMENTATION_STANDARDS.md`; update only the canonical owner and use valid relative links.
- Keep `docs/STATUS.md` user-observable: do not put internal implementation details or backlog there. Track planned work in GitHub Issues.

## GitHub Issues
- Use `gh` CLI and verified issue content; do not guess issue details or URLs. Every issue belongs to the "Priority Task Manager Roadmap" project and an applicable milestone ((MVP) Minimum Viable Product, (V1) Online Daily Planner, (V2) Flexible Smart Planner, or Dream Product Future Issues); blocked-by or sub-issue work generally shares an earlier milestone.
- Set parent/child and blocked-by relationships using GitHub's native GraphQL mutations (`addSubIssue`/`removeSubIssue`, `addBlockedBy`/`removeBlockedBy` with node IDs), not body mentions alone. Restate a dependency only when its rationale or scope needs explanation.
- When closing a superseded issue, explain why and update affected relationships and references. Ask before closing or substantially rescoping overlapping issues when intent is ambiguous.

## API Deployment
- Changes under `PriorityTaskManager.API/` or `PriorityTaskManager/` that affect the hosted API are not live until Fly.io is redeployed; flag this when relevant and follow `docs/WORKFLOW.md`. If hosted API behavior seems stale, check Fly releases before assuming a code defect.

## Code Conventions
- Follow existing patterns and use descriptive names. Add XML documentation to public C# members; comment only non-obvious reasoning.
