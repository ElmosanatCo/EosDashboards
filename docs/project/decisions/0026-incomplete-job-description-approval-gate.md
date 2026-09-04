# 0026 — Incomplete job-description approval gate

**Status:** Accepted
**Date:** 2026-09-04

## Context

Imported workbooks can contain skill names and task titles that do not match
the current catalogs. Dropping those values makes the description appear
empty and prevents the manager from correcting the source data. A quality
label that is only informative would also allow an unresolved draft to reach
Human Resources.

## Decision

Keep the raw imported skill and task text visible and retained until the
manager maps it to an existing catalog value or creates a new authorized
catalog value. Any missing required field or unresolved catalog value makes
the quality status `ناقص` and the workflow status `منتظر رفع نقص`.

The manager and Human Resources may view an incomplete record, but the manager
approval action and the transition to Human Resources are blocked. Once the
manager resolves the issues and the record becomes `سالم`, it moves to
`منتظر تأیید`; explicit manager approval is still required. The same invariant
applies after a later revision.

This decision supersedes the part of decision 0017 that made quality status
informational and allowed incomplete records to proceed.

## Rationale

The workflow must not lose source text or silently guess a catalog mapping, and
Human Resources should review only a manager-confirmed, catalog-linked draft.

## Consequences

- The database and API must retain unresolved skill/task values and expose them
  to the detail and edit surfaces.
- The edit flow must offer existing-catalog selection and authorized creation
  for each unresolved value.
- Quality calculation, approval endpoints, list/detail UI, dashboard actions,
  and tests must enforce the `منتظر رفع نقص` gate.
