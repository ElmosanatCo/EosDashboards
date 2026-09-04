# 0020 — Database-canonical job-description artifacts

**Status:** Accepted
**Date:** 2026-09-04

**Supersedes:** The source-conflict gate in decision 0016

## Context

Job descriptions need structured database data for application behavior and an
Excel representation for human review and download. Treating both as
independent sources would create an unnecessary synchronization problem.

## Decision

The database is the canonical source for a job-description version. The system
first persists the normalized personnel, task, skill, status, and review data.
It then generates the standard Excel workbook from that persisted version and
stores the generated workbook artifact in the database, linked to the same
version.

Manual entry and Excel upload both follow this same flow after normalization.
Dashboards, searches, statistics, quality analysis, and approval workflow read
the structured database data directly. The generated Excel artifact is only a
stored presentation/download representation and is never parsed to calculate
dashboard statistics.

## Rationale

One canonical structured record removes source drift and makes the Excel file a
reproducible, reviewable representation of the exact version used by the
application.

## Consequences

- A completed version cannot be treated as complete until its database record
  and generated Excel artifact are linked.
- Regeneration uses the stored database version rather than a previously
  generated workbook.
- Tests verify generation from the matching database version and verify that
  operational queries do not depend on workbook parsing.
