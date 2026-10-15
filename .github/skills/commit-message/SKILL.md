---
name: commit-message
description: "Analyze staged or unstaged changes, reason about cohesive commit boundaries, and write clear Conventional Commit messages. Use when planning or splitting commits, writing a commit message, or reviewing a proposed commit plan."
argument-hint: "optional context, commit scope, or special constraints"
user-invocable: true
disable-model-invocation: false
---

# Commit Planning and Message Review

Use this skill to recommend the smallest useful set of coherent commits and write a clear Conventional Commit message for each.

This skill is advisory. Inspect and recommend only; do not stage files, create commits, amend commits, or rewrite history.

## When to Use

- The user asks for a commit message, summary, or changelog entry based on changes.
- The user asks what to commit, how to split changes, or whether a proposed commit is cohesive.
- The user wants a Conventional Commit message or a review of commit boundaries.

## Procedure

1. **Establish the exact change set.**
   - Run `git status --short` to distinguish staged, unstaged, and untracked changes.
   - If any changes are staged, inspect the staged changes first with `git diff --cached --stat`, `git diff --cached --name-only`, and focused parts of `git diff --cached`. By default, do not include unstaged or untracked changes in that proposal; call out that they were excluded.
   - If nothing is staged, inspect the unstaged tracked changes with `git diff --stat`, `git diff --name-only`, and focused parts of `git diff`. Inspect untracked files only when the user wants the full working-tree change set or asks to include them.
   - For a large diff, inspect relevant files and hunks selectively. Do not infer intent from filenames alone.

2. **Identify intent and dependencies.**
   - Summarize the user-visible or maintenance outcome the changes deliver.
   - Inspect enough surrounding code, tests, and documentation to understand how the changes depend on one another.
   - Separate the requested work from unrelated pre-existing changes. Do not silently absorb unrelated files into a proposal.
   - If changes in one file serve different purposes, identify the distinct hunks or line ranges.

3. **Reason about commit boundaries before writing messages.**
   - Recommend **one commit** when the implementation, tests, and relevant documentation support one cohesive outcome and would be harder to understand or validate separately.
   - Recommend a **split** when there are independent purposes or behavior changes that can be reviewed, tested, reverted, or released separately, or when a sequence of meaningful, buildable steps improves review.
   - Explain why each proposed boundary is useful. When commits depend on one another, state their order and dependency.
   - Keep tests with the implementation they verify and documentation with the behavior it describes, unless either is an independent change in its own right.
   - Do not split solely by file, directory, language, project, or line count. A cross-layer feature can belong in one commit; split it only when the slices have distinct review value and remain coherent.
   - Prefer each proposed commit to leave the repository buildable and understandable. If a clean split would create an incomplete or misleading commit, recommend one cohesive commit instead.
   - If the intent or boundary is genuinely ambiguous after inspecting the changes, ask one focused question before finalizing the plan.

4. **Choose the Conventional Commit type and scope for each proposed commit.**
   - `feat` for a new capability
   - `fix` for a bug fix
   - `refactor` for code changes that do not alter behavior
   - `docs` for documentation-only changes
   - `test` for an independent test-only change
   - `chore` for maintenance work
   - `perf` for performance improvements
   - `build` for build or dependency changes
   - `ci` for CI configuration changes
   - `revert` for a revert
   - Choose a scope when it names a useful, stable subsystem; omit it when the change spans areas or the scope is unclear.

5. **Write a specific subject for each commit.**
   - Start with `type:` or `type(scope):`.
   - Use imperative mood and describe the actual outcome, not the editing activity.
   - Keep the subject lowercase after the type and scope, concise, and without trailing punctuation. Aim for 50 characters or fewer when practical; keep it under 72 unless the change genuinely needs more.
   - Avoid vague subjects such as `updated`, `misc`, or `fix stuff`.
   - For a breaking change, mark it in the subject with `!` and explain the compatibility impact in the body.

6. **Add a body only when it clarifies intent.**
   - Explain why the change exists, not a line-by-line recap.
   - Mention user-visible behavior, important invariants, trade-offs, or migration/compatibility notes when relevant.
   - Keep it short and factual. Do not claim that tests ran unless the available evidence confirms it.

7. **Map and verify the proposed plan.**
   - For one commit, state why the change is cohesive.
   - For a split, list commits in dependency order and assign every included file or relevant hunk to exactly one commit.
   - Include tests and documentation in the appropriate commit. Give line ranges only when needed to distinguish hunks within a file.
   - Check that the proposal does not omit relevant changes, duplicate files across commits without explanation, or split work without a review or validation benefit.

## Output Format

Start with `Recommendation: One commit` or `Recommendation: Split into N commits`, followed by a concise rationale for the boundary decision.

For every proposed commit, include its order if relevant, complete Conventional Commit message, intent/body when useful, and affected files. For a split, briefly explain dependencies between commits.

Example:

```text
Recommendation: Split into 2 commits

Rationale: The scheduler fix and API development-account update have independent purposes and can be reviewed and reverted separately. Keep the scheduler test with the behavior it verifies.

Commit 1
fix(scheduler): preserve the active snapshot after cleanup

Keep the dashboard redraw in sync with the refreshed schedule.

Files affected:
- PriorityTaskManager/Scheduling/ScheduleCleanup.cs
- PriorityTaskManager.Tests/Scheduling/ScheduleCleanupTests.cs

Commit 2
chore(api): update development account seeding

Files affected:
- PriorityTaskManager.API/Dev/DevAccountSeeder.cs
```

## Quality Check

Before responding, verify that the recommendation:

- Explains why one commit or a split is the better boundary; it does not force a split to meet a size target.
- Keeps each commit cohesive and accounts for all included files or relevant hunks exactly once.
- Keeps implementation-specific tests and documentation with the change they verify or describe.
- States commit order when dependencies exist and avoids proposing misleading or incomplete intermediate commits.
- Uses a correct Conventional Commit type, a useful scope, and a concise, imperative subject for each commit.
- Uses a body only when it adds intent or compatibility context.
- Distinguishes observed test evidence from tests that merely appear in the diff.
