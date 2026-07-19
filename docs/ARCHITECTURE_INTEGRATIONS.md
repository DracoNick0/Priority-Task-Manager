# Integrations Architecture

This document defines boundaries for future integrations, external intake, APIs, and additional front ends.

## Scope

This document defines the architectural boundaries to use when integration work is introduced. Use [STATUS.md](STATUS.md) for current runtime maturity, [TODO.md](TODO.md) for planned integration work, and [LLM_ASSISTED_INTAKE.md](LLM_ASSISTED_INTAKE.md) for LLM-assisted intake planning.

## Responsibilities

Future integration architecture should cover:

- API or service layers for additional clients.
- External source providers such as GitHub, calendars, documents, todo exports, or Canvas.
- Candidate extraction and normalization flows.
- Review-and-confirm workflows before persistence.
- Provider guardrails such as rate limits, retries, validation, and source traceability.

## Boundary Principles

- Integrations should call core services rather than duplicating task/list/event persistence logic.
- External provider code should sit behind source-specific abstractions.
- Imported content should be normalized into candidate models before becoming persisted tasks, lists, or events.
- User review should happen before generated or imported items are persisted.
- Source metadata and decision traceability should be preserved for imported candidates.

## Placement Guidance

- Put reusable intake, normalization, validation, and persistence orchestration in core or a future service layer, not in CLI-only code.
- Put provider-specific clients behind interfaces so new providers do not change core scheduling or persistence contracts.
- Keep UI review flows thin; they should present candidates and call core approval/persistence operations.
- Keep LLM/provider configuration and guardrails separate from scheduling algorithms.

## Relationships With Existing Areas

| Area | Relationship |
| --- | --- |
| Core business logic | Owns approved task/list/event mutations and shared validation |
| Data and persistence | Owns persisted state shape and imported source metadata once approved |
| CLI and user interaction | May provide a local review flow, but should not own provider logic |
| Scheduling | Consumes approved tasks/events after persistence; should not call providers |

## Invariants

- No integration should silently overwrite user-edited tasks or events.
- Imported or generated records should not bypass core validation.
- External fetch/extraction failures should produce actionable diagnostics and should not corrupt existing local data.
- Provider-specific assumptions should not leak into scheduler or persistence internals.