# 0018 — Department manager child-department scope

**Status:** Accepted
**Date:** 2026-09-04

## Context

Some Department Managers are responsible for one or more child departments.
Job-description management and dashboard summaries need an explicit scope so
that records are neither hidden from the responsible manager nor exposed
outside that manager's authority.

## Decision

A Department Manager has full job-description management authority for their
own department and all of their child departments. Create and edit forms let
the manager select the target department from that authorized scope.

The department dashboard provides both:

- an all-managed-departments view; and
- a view filtered to one department in the manager's scope.

Server-side authorization enforces the same scope for every read and write.

## Rationale

This matches the manager's confirmed organizational responsibility while
keeping the scope explicit in forms, dashboard filters, and API authorization.

## Consequences

- Job-description drafts, skills, tasks, imports, analysis, and dashboard
  queries carry an explicit target department.
- The UI must not use a visual filter as the authorization mechanism; the API
  must validate the selected department.
- A manager with no child departments receives the same interface with only
  their own department available.
