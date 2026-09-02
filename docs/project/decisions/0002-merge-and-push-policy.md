# 0002 — Merge and push policy

**Status:** Accepted
**Date:** 2026-09-02

## Context

Project continuity depends on the remote repository containing both the latest implementation and the durable project memory. A local-only merge can leave GitHub behind the authoritative local state, while merging or publishing before documentation updates can lose requirements and agreements between AI conversations.

## Decision

Before any local merge or push, update `AGENTS.md` and the canonical project documents with every durable requirement, decision, agreement, state change, and next step introduced by the work, then verify those updates.

A local merge must be followed by verification and a push of the destination branch to its configured remote. If the push fails or cannot be performed, the integration is incomplete and must be reported without discarding the branch or worktree.

## Rationale

Keeping documentation, local Git history, and the remote repository synchronized makes a new task's recovered context match the code it receives.

## Consequences

- A local-only merge is not an allowed completed state.
- Documentation freshness is a precondition for both merges and pushes.
- Merge completion reports must include the result of the corresponding push.
- Push failures leave the recoverable local state intact for diagnosis or retry.
