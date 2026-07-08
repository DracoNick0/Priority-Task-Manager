---
name: commit-message
description: "Generate a clean conventional commit message from staged git changes. Use when you want a commit title, optional body, or a quick commit-message review based on changes."
argument-hint: "optional context, commit scope, or special constraints"
user-invocable: true
disable-model-invocation: false
---

# Conventional Commit Message Generator

Use this skill when the user wants a commit message derived from git changes.
It should inspect staged content first.
If there are no staged changes, inspect unstaged content.

## When to Use
- The user asks for a commit message, summary, or changelog entry for staged changes
- The user wants the message to follow Conventional Commits
- The user wants help choosing the right commit type or scope

## Procedure
1. Inspect the staged changes first.
   - Prefer `git diff --cached --stat` and `git diff --cached --name-only` for a quick overview.
   - Use `git diff --cached` when you need detail.
   - If there are no staged changes, inspect unstaged changes with `git diff --stat`, `git diff --name-only`, and `git diff`.
   - If staged changes exist, ignore unstaged changes unless the user says otherwise.
2. Identify the primary purpose of the change.
   - `feat` for a new capability
   - `fix` for a bug fix
   - `refactor` for code changes that do not alter behavior
   - `docs` for documentation-only changes
   - `test` for test-only changes
   - `chore` for maintenance work
   - `perf` for performance improvements
   - `build` for build or dependency changes
   - `ci` for CI configuration changes
   - `revert` for a revert
3. Choose a scope only when it is obvious and useful.
   - Prefer a stable subsystem, package, or folder name
   - Omit the scope when the change spans multiple areas or the scope is unclear
4. Write the subject line in imperative mood.
   - Keep it lowercase after the type and scope
   - Keep it concise and specific
   - Avoid trailing punctuation
   - Aim for 50 characters or fewer when possible, and keep it under 72 characters unless the change truly needs more
5. Add a body only when it clarifies intent.
   - Explain why the change exists, not a line-by-line recap
   - Mention user-visible behavior, tradeoffs, or migration notes
   - Keep it short and factual
6. If the inspected changes are mixed or unrelated, say so and recommend splitting them into separate commits.
7. If the intent is still ambiguous after reviewing the inspected diff, ask one focused clarifying question before finalizing the message.

## Output Format
Return one primary Conventional Commits message.
If helpful, include up to two alternatives.

Example:

```text
fix(cli): preserve schedule redraw after cleanup

Keep the active snapshot in sync after cleanup so the dashboard
redraw uses the refreshed schedule state.
```

## Quality Check
Before finishing, verify that the message:
- Matches Conventional Commits syntax
- Describes the dominant inspected change
- Uses a correct type and a sensible scope
- Is concise, imperative, and specific
- Uses staged changes when present, otherwise unstaged changes
