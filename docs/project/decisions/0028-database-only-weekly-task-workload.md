# 0028 — Database-only weekly task workload

**Status:** Accepted
**Date:** 2026-09-04

## Context

The organization needs a later workload view that can identify personnel under
pressure or with available capacity. The standard Excel format is already
established for portable job-description content and should not be expanded
for this internal calculation field.

## Decision

Each resolved job-description task stores an average required workload in hours
per week in the database. The value is entered in the manager form, accepts
zero through 168 hours, is required for manager-created and revised records,
and is returned to manager and Human Resources detail views. It is not read
from, written to, or displayed in the standard Excel artifact.

Imported workbooks that do not contain this field remain retained as incomplete
until the manager supplies it.

## Rationale

Separating internal workload analysis from the portable document preserves the
approved Excel contract while creating a reliable basis for future pressure and
capacity metrics.

## Consequences

- The database task table and API task contracts contain the weekly-hours value.
- Missing or invalid values prevent manager create/revise submission and keep
  the record incomplete.
- Future workload dashboards must calculate from database assignments, not
  generated Excel files.
