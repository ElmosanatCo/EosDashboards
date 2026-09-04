# 0023 — Action-linked job-description quality findings

**Status:** Accepted
**Date:** 2026-09-04

## Context

Quality analysis is useful only if a manager can quickly locate and correct the
affected information. A general warning without a location would make
incomplete and mismatched records expensive to review.

## Decision

Each quality finding includes a direct action key or link to the affected
profile field, selected skill, task title, task start date, or free-text task
description. The action opens the relevant view or edit location; applying a
correction remains an explicit manager action.

## Rationale

Location-aware findings reduce review effort while preserving user control and
the existing approval workflow.

## Consequences

- Quality findings need stable references to the affected version and field or
  task location.
- UI tests must verify that each actionable finding opens the correct location.
- The analyzer must not apply corrections automatically.
