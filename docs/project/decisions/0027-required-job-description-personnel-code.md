# 0027 — Required job-description personnel code

**Status:** Accepted
**Date:** 2026-09-04

## Context

The personnel code was previously treated as optional in the manager form,
which allowed a manually created or revised job description to be saved
without a reliable personnel identifier.

## Decision

Personnel code is required for manager-created and revised job descriptions.
The form marks it as required, the client prevents saving while it is empty,
and the API rejects create or revise commands without it. The Excel format does
not gain a new required column; an imported workbook that has no code may be
retained as `ناقص` so the manager can enter the code before approval.

## Rationale

The rule keeps manually maintained records identifiable without breaking the
approved tolerant import behavior for legacy workbooks that do not contain a
personnel-code column.

## Consequences

- Quality calculation continues to mark an empty imported code as `ناقص`.
- Existing incomplete records remain editable until a code is supplied.
- API contracts and UI create/revise flows treat the code as a required value.
