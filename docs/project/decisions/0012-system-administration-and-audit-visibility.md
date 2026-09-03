# 0012 — System administration and audit visibility

**Status:** Accepted

**Date:** 2026-09-03

## Context

The approved administration scope needed concrete operational lifecycle,
department, and audit decisions before implementation. The existing audit and
session records also enable a small truthful System Administrator dashboard
without inventing a business-data source.

## Decision

System Administrators manage accounts, assignments of the four fixed roles,
and the two-level department hierarchy through separate workspace pages. Users
are never permanently deleted; every account retains exactly one department,
and active accounts require at least one role. Account creation and administrator resets use temporary
passwords that must be changed after the existing local-password/SMS-OTP flow.
Administrative changes revoke the target's sessions, while preserving the
acting administrator's session. The final active System Administrator cannot
be deactivated or stripped of that role.

Department names are organization-wide unique. A department is independent or
an explicit direct child of an independent department. Only an empty department
may be deleted; re-parenting and making a child independent are allowed only
when the two-level invariant remains true.

The System Administrator receives an operational dashboard and an audit page.
They expose immutable security and administration events without secret or full
mobile data. Request-originated records retain the direct inbound IP and a
coarse desktop/mobile/tablet/unknown device kind; raw user-agent and forwarded
headers are neither retained nor trusted. Dashboard sign-in and security metrics cover the preceding 24
hours. The session count is explicitly labelled as users with active sessions,
not live online users.

## Rationale

This delivers the approved operating boundary with clear recovery paths,
prevents administrative lockout, protects personal credentials, and makes
security activity visible without treating an unexpired session as proof of
live presence.

## Consequences

- New server-authorized user, department, audit-query, and dashboard endpoints,
  migrations, UI pages, audit events, and focused tests are required.
- Personnel code is the UI label for the existing organizational identifier;
  it may be corrected. An omitted username defaults to it but is later
  independent and editable.
- Audit retention and non-administrator access remain unapproved.
- All user-facing date-selection controls use the Persian calendar.
- Google-link administration, custom roles, granular permissions, exports, and
  alerts remain excluded.
