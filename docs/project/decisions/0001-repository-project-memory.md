# 0001 — Repository-based project memory

**Status:** Accepted
**Date:** 2026-09-02

## Context

EosDashboards will be developed through multiple short AI conversations. Repeating the full history wastes context, while relying on chat memory risks losing requirements and decisions.

## Decision

Use a concise root `AGENTS.md` as the automatic entry point and focused canonical documents under `docs/project/`. Every task reads the current-state snapshot, loads only relevant topic documents, and updates durable knowledge before completion.

## Rationale

This structure preserves continuity while limiting startup context. Canonical topic files expose the latest truth more clearly than chronological chat logs.

## Consequences

- Documentation changes are part of completing relevant implementation work.
- Agents must distinguish confirmed facts from proposals and unresolved questions.
- The current-state snapshot must remain short and current.
- Raw conversations and sensitive information are not stored.
