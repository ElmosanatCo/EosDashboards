# 0014 — Cost-conscious verification and environment learning

**Status:** Accepted
**Date:** 2026-09-04

## Context

AI-assisted development can waste time and tokens by loading unrelated context,
repeating successful checks, rerunning unchanged failures, or rediscovering the
same local environment problem in later conversations.

## Decision

Every task starts by reading `current-state.md`, `README.md`, and
`standards.md`, followed by only the canonical documents relevant to the task.

Verification uses the smallest test and diagnostic scope that provides credible
evidence for the changed behavior and its risks. Broader suites run only at
meaningful checkpoints. A successful command is not repeated unless code,
configuration, environment, or the claim being verified has changed.

A failed operation is not repeated blindly. Before retrying, the agent must
identify the likely cause and change or verify the relevant cause, input,
configuration, environment, or diagnostic hypothesis.

For every material tooling, environment, deployment, test, or integration
failure, the agent records the symptom, root cause, durable remedy, safe
verification evidence, and prevention rule. Reusable development and
environment remedies belong in `standards.md`; current task state belongs in
`current-state.md`; consequential rationale belongs in a decision record.

## Rationale

This keeps AI-assisted work economical while turning local failures into durable
project knowledge. Future agents can avoid known environmental traps instead of
repeating operations that are already known to fail.

## Consequences

- Startup context is predictable and intentionally small.
- Focused tests are preferred during implementation, with broad verification at
  integration and publication checkpoints.
- Material failures require a documented resolution or an explicit unresolved
  blocker before the task ends.
- The standards document gains reusable operational guidance, not raw chat or
  incident transcripts.
