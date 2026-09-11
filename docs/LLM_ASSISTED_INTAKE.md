# LLM Assisted Intake

## Purpose
Define a simple, concrete plan for importing external content and turning it into candidate lists, tasks, and events with LLM assistance.

## Scope
This document covers planned behavior only. It does not define current implementation status.

## Goals
- Accept external planning sources (for example: GitHub, documents, existing todo lists, Canvas content).
- Convert source content into candidate lists, tasks, and events.
- Require user review before imported items are persisted.
- Keep source attribution so users can trace where each generated item came from.

## Non-Goals (MVP)
- Fully automatic persistence without user confirmation.
- Deep workflow automation across every third-party platform.
- Real-time bi-directional sync for all sources.

## Input Sources (MVP Priority)
1. GitHub (issues, pull requests, project items, milestones).
2. Calendar feeds (ICS, Google, Outlook) for event extraction.
3. Documents and notes (Markdown, text, PDF exported text).
4. Existing todo exports (structured JSON/CSV where available).
5. Canvas via official API.

## Source Access Strategy
- Prefer official APIs over scraping.
- Use scraping only as an explicit fallback and mark it unstable.
- Store source connection settings and sync checkpoints per user/source.

## Intake Pipeline
1. Connect: authenticate and validate source access.
2. Fetch: pull source records (full import or incremental sync).
3. Normalize: map raw records into a shared intermediate format.
4. Extract: use deterministic parsing first, then LLM for summarization/classification where needed.
5. Propose: generate candidate lists, tasks, and events.
6. Review: show confidence, source attribution, and suggested fields for user confirmation.
7. Persist: write approved items through core services.

## Intermediate Candidate Model (Concept)
- CandidateList: title, description, source metadata.
- CandidateTask: title, description, importance, estimated duration, not-before, due date, list assignment, source metadata. Fields already scoped as advanced scheduling on `TaskItem` (pinned, divisible, complexity, points, padding) are out of scope for LLM-generated candidates; they take `TaskItem`'s own constructor defaults and are left for the user to set explicitly, not inferred.
- CandidateEvent: title, start/end, location or link, source metadata.
- CandidateMetadata: source ID, source URL, extraction timestamp, model version, per-field confidence scores and needs-review flags (see below).

## Confidence And Field-Level Review
- Confidence is tracked per field, not per candidate.
- The LLM always produces a best-effort value for every field it can reasonably infer; it never leaves a field null solely because confidence is low.
- Any field below the confidence threshold is populated with the LLM's best-effort value and flagged as needing review, surfaced distinctly in the review UX rather than accepted silently.
- A field the LLM cannot infer at all from source content still receives a system default (matching `TaskItem`'s own constructor defaults) and is flagged as needing review, rather than being left null.
- The concrete confidence threshold that triggers a needs-review flag is not yet defined; it is expected to be tuned empirically once a provider is chosen.

## List Assignment
- Candidate tasks are pre-assigned to whichever list is currently open/active in the client at the time intake is run. LLM-driven list selection or new-list proposal is out of scope for this pass.

## Duplicate And Merge Detection
- Detecting or merging candidates against already-approved tasks (for example, on repeated sync of the same source) is out of scope until a later pass.
- Structured sources with a natural external identifier (for example, a GitHub issue or pull request number) should still carry that identifier in candidate source metadata now, so future duplicate/merge detection does not require a later data migration.

## Candidate Persistence and Tiering
- Candidates are persisted (not held only in transient UI state), since extraction can be asynchronous and review may not happen in the same session.
- A candidate is deleted once the user accepts it (after being converted into a real task/list/event through core services) or rejects it; candidates do not linger after a decision is made.
- Free tier: candidates persist only in the client's local store (CLI JSON, Flutter Hive) and are never written to the account-scoped API/Postgres store.
- Subscription tier: candidates persist server-side, account-scoped, the same way tasks/lists/events are (see [ARCHITECTURE_INTEGRATIONS.md](ARCHITECTURE_INTEGRATIONS.md)), so review can continue across devices.
- Extraction requests and quota enforcement always go through the API for both tiers; only the resulting candidates' persistence location differs by tier.
- Quota is measured in LLM tokens consumed, not request count or candidate count.

## UX and Safety Rules
- Never silently overwrite user-edited tasks/events.
- Surface low-confidence suggestions clearly.
- Allow one-click reject, edit, or approve for each candidate.
- Keep an import log for auditability.

## Architecture Alignment
- Core owns normalization, extraction orchestration, candidate generation, and persistence rules.
- CLI/UI layers only orchestrate commands and present review flows.
- Provider integrations should sit behind source-specific interfaces to keep core logic stable.

## Initial Delivery Plan
1. Build GitHub intake with review-first import.
2. Add calendar intake for event generation.
3. Add document/todo file intake.
4. Add Canvas API intake.
5. Add optional VS Code extension as a thin client over the same intake services.

## Open Questions
- Which confidence threshold triggers a needs-review flag on a field?
- Which LLM provider/model is used for extraction? Not yet chosen; tracked as part of provider-abstraction work.
- Which fields are required before a candidate task can be persisted?
- How long should raw source payloads be retained versus normalized facts only?

## Related Documents
- docs/ARCHITECTURE.md
- Repository GitHub Issues
- docs/CONSTRAINT_SOLVER.md
- docs/WORKFLOW.md
