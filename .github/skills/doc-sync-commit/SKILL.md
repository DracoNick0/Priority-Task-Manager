---
name: doc-sync-commit
description: "Analyze staged changes, update documentation only when required by standards, and generate a commit message that reflects code and doc updates."
argument-hint: "optional scope, ticket id, strictness mode (advisory|apply|strict), or commit constraints"
user-invocable: true
disable-model-invocation: false
---

# Doc Sync Commit

Use this skill when you want one pass that:
1. Reviews staged changes.
2. Determines whether documentation updates are required.
3. Applies only necessary documentation changes.
4. Produces a commit message that reflects staged code and documentation state.

This skill is standards-driven. It must avoid speculation and must not edit unrelated docs.

## Primary Sources
Always consult these first:
- docs/DOCUMENTATION_STANDARDS.md
- docs/DOC_INDEX.md
- docs/TERMINOLOGY.md

Use these as verification anchors when relevant:
- PriorityTaskManager.CLI/Program.cs
- PriorityTaskManager/Services/TaskManagerService.cs
- PriorityTaskManager/Scheduling/GoldPanning/GoldPanningStrategy.cs

## When to Use
- User asks for a commit message and wants docs kept in sync.
- User asks for documentation impact analysis of staged changes.
- User wants to reduce documentation drift in commit-time workflows.

## Hard Constraints
- Never invent undocumented behavior.
- Never speculate about implementation details not present in staged or verified code.
- Never update documentation unrelated to staged changes.
- Never duplicate canonical content across docs.
- If confidence is insufficient, do not guess. Report a documentation gap instead.

## Workflow
1. Inspect staged changes only.
   - Prefer:
     - git diff --cached --stat
     - git diff --cached --name-only
     - git diff --cached
2. Classify staged change impact.
   - Command surface
   - Core behavior
   - Architecture boundary
   - Scheduling logic
   - Persistence or data model
   - Test strategy
   - Process or workflow
   - Backlog or planning only
3. Map impact to canonical documentation owners using docs/DOC_INDEX.md.
4. Run documentation decision gates in order.
5. If documentation updates are required and verifiable, apply minimal edits.
6. Validate consistency and standards compliance.
7. Produce commit message that includes code and documentation outcomes.

## Decision Gates
All gates must pass before editing docs.

1. Relevance Gate
- Does staged work change behavior, boundaries, command surface, process, or roadmap artifacts that docs own?

2. Ownership Gate
- Is there a canonical owner for this topic in docs/DOC_INDEX.md?

3. Evidence Gate
- Can the update be justified from staged changes or verified code anchors?

4. Minimality Gate
- Can the doc update be done without touching unrelated sections/files?

5. Consistency Gate
- Will the edit keep docs aligned with docs/DOCUMENTATION_STANDARDS.md and docs/TERMINOLOGY.md?

If any gate fails:
- Do not edit docs.
- Emit Missing Documentation Findings with required follow-up evidence.

## Documentation Update Policy
Always evaluate:
- docs/DOCUMENTATION_STANDARDS.md
- docs/DOC_INDEX.md
- docs/TERMINOLOGY.md

Conditionally update by change type:
- Command surface changes:
  - Update docs/STATUS.md command surface section.
  - Update README.md only if user-facing capability summary changed.
- Core architecture/boundary changes:
  - Update docs/ARCHITECTURE.md.
- Behavior maturity changes:
  - Update docs/STATUS.md.
- Process changes:
  - Update docs/WORKFLOW.md.
- Testing model changes:
  - Update docs/TESTING_STRATEGY.md.
- Roadmap/backlog changes:
  - Update docs/TODO.md.
- Algorithm-specific changes:
  - Update docs/GOLD_PANNING.md or docs/CONSTRAINT_SOLVER.md.
- Complexity semantics changes:
  - Update docs/COMPLEXITY_GUIDE.md.

If staged changes do not require doc updates, leave docs unchanged.

## Consistency Validation
After doc edits, verify:
- Scope compliance with docs/DOCUMENTATION_STANDARDS.md.
- Ownership compliance with docs/DOC_INDEX.md.
- Terminology consistency with docs/TERMINOLOGY.md.
- No stale/broken references introduced.
- No roadmap content leaked into docs/STATUS.md.
- No current-state claims leaked into docs/TODO.md.

## Missing Documentation Findings Format
If refusing updates due to low confidence, return:
- Affected topic
- Suggested canonical document
- Why confidence is insufficient
- What evidence is required

Example:
- Topic: Scheduling mode behavior
- Owner: docs/STATUS.md
- Issue: staged diff renames mode enum, but runtime command behavior was not verifiable
- Needed evidence: Program.cs command routing and TaskManagerService mode branch

## Commit Message Policy
Generate commit messages using the same conventions and quality bar as the conventional-commit-message skill.

1. Follow Conventional Commits syntax.
2. Derive the message from staged content only unless the user explicitly asks to include unstaged work.
3. Identify the dominant purpose and choose the type accordingly:
  - `feat` for new capability
  - `fix` for bug fix
  - `refactor` for non-behavioral code restructuring
  - `docs` for documentation-only changes
  - `test` for test-only changes
  - `chore` for maintenance changes
  - `perf` for performance changes
  - `build` for build/dependency changes
  - `ci` for CI/config automation changes
  - `revert` for explicit reverts
4. Choose a scope only when obvious and useful; omit scope when unclear or too broad.
5. Write the subject line in imperative mood:
  - lowercase after type and scope
  - no trailing punctuation
  - concise and specific
  - target 50 characters where practical, keep under 72 unless needed
6. Add a body only when it improves clarity:
  - explain why, not a line-by-line recap
  - note user-visible effects, tradeoffs, or migration impact
  - keep factual and concise
7. Include documentation-sync outcome in the body when relevant:
  - docs updated and which canonical docs changed
  - docs evaluated but no update required
8. If staged changes are mixed or unrelated, explicitly recommend splitting into separate commits.
9. If commit intent is ambiguous after staged diff review, ask one focused clarifying question before finalizing.

Output format:
- Return one primary Conventional Commits message.
- Optionally include up to two alternatives when ambiguity exists.

Quality check before finishing:
- Message matches Conventional Commits syntax.
- Type and scope match the dominant staged change.
- Subject is concise, imperative, and specific.
- Message does not mention unstaged work unless requested.
- Documentation-sync statement matches actual doc decisions made by this skill.

## Modes
- advisory: analyze and propose edits, do not modify files.
- apply: analyze and apply required doc updates.
- strict: fail the workflow if required docs cannot be updated confidently.

Default mode: apply.

## Output Contract
Return:
1. Staged change summary.
2. Documentation impact decision.
3. Files updated (or explicit no-update decision).
4. Consistency checks performed.
5. Final commit message.
