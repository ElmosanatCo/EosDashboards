# 0017 — Job-description workflow and quality statuses

**Status:** Accepted
**Date:** 2026-09-04

## Context

Job-description approval progress and completeness of entered information are
different concerns. Combining them in one status would hide whether a record
is waiting for approval or merely has missing information.

## Decision

Use two independent statuses.

The workflow status is:

- `منتظر تأیید`: newly created or revised and waiting for Department Manager
  confirmation;
- `در حال بررسی`: sent by the Department Manager and waiting for Human
  Resources review;
- `تأیید شده`: approved by both the Department Manager and Human Resources and
  therefore active;
- `رد شده`: returned by Human Resources with a reason and eligible for manager
  revision and resubmission;
- `آرشیو شده`: an approved record retained for a departed person without
  deleting its history.

The independent data-quality status is:

- `سالم`: approved required information is present; or
- `ناقص`: one or more approved fields have no value.

A record may have any applicable workflow status together with `ناقص`. Quality
status is informative and does not block approval unless a later explicit
decision adds such a rule.

## Rationale

Separating progress from completeness lets managers and Human Resources see
both what action is pending and what information needs attention.

## Consequences

- Database, API, UI filters, dashboard counts, and Excel metadata must keep the
  two status axes distinct.
- Tests must cover combinations such as `در حال بررسی` + `ناقص`.
- Any future blocking rule based on missing values requires an explicit new
  decision.
