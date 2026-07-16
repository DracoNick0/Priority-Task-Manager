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
- CandidateTask: title, notes, due date, complexity hint, dependencies, source metadata.
- CandidateEvent: title, start/end, location or link, source metadata.
- CandidateMetadata: source ID, source URL, extraction timestamp, confidence score, model version.

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
- Which confidence threshold should auto-group candidates for bulk approval?
- Which fields are required before a candidate task can be persisted?
- How long should raw source payloads be retained versus normalized facts only?

## Related Documents
- docs/ARCHITECTURE.md
- docs/TODO.md
- docs/CONSTRAINT_SOLVER.md
- docs/WORKFLOW.md
