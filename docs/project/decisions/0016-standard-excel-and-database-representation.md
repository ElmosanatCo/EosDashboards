# 0016 — Standard Excel and database representation

**Status:** Accepted
**Date:** 2026-09-04

## Context

Standardized job-description workbooks must remain available for review and
download, while dashboards, search, skill/task analysis, and approval workflows
need structured data that can be queried reliably.

## Decision

After Excel or manual input is standardized, persist both representations:

- the normalized personnel, task, skill, status, and review data in the
  database; and
- the corresponding standard Excel artifact linked to the same draft/version.

Neither representation is silently discarded. The structured database
representation is the sole source for operational queries, statistics, quality
analysis, and workflow state. The linked Excel artifact is used for human
review and download; dashboards never re-read Excel artifacts to calculate
statistics.

The authoritative conflict-resolution and rebuild policy for a mismatch between
the two representations remains an explicit design gate before implementation.

## Rationale

Structured persistence prevents dashboards and analysis from repeatedly parsing
workbooks, while retaining the approved human-readable document preserves the
existing review and exchange workflow.

## Consequences

- A job-description draft/version must identify and link its database data and
  standard Excel artifact.
- Tests must verify that both representations describe the same version.
- Artifact storage, retention, replacement, and rebuild behavior require an
  implementation design before publication.
