# Administration usability and audit attribution design

**Status:** Proposed for user review

**Date:** 2026-09-03

**Extends:** `2026-09-03-system-administration-and-audit-dashboard-design.md`

## Purpose

Correct the first System Administrator workspace release so user and department
operations are directly usable, audit entries identify their network and device
origin, and every date-selection control is Persian-calendar based.

## Confirmed experience changes

- User and department create/edit forms open as focused responsive modal
  dialogs above their respective management pages. They are not workspace tabs
  and do not change the browser route. A successful operation refreshes the
  source list and closes the dialog; failures remain in the dialog with a safe,
  specific Persian explanation beside the relevant input or operation area.
- The same modal form supports create and edit. Reset-password, deactivation,
  and deletion retain explicit confirmation because they are consequential.
- The footer keeps date and time as separate, labelled Persian values, but
  lays them out on one non-wrapping row whenever the viewport has room. Its
  shell height matches that single-line layout.
- Every user-facing date selector uses the Persian calendar and Persian digits.
  Native Gregorian `date` or `datetime-local` controls are not used. A shared
  picker supplies a Persian calendar date plus a separate, labelled local time
  control where a timestamp is needed. It converts only the selected value to
  the API's existing server-local timestamp contract.

## Audit attribution

Each new audit record stores two nullable, safe attribution values:

| Field | Values | Source and display |
| --- | --- | --- |
| `ClientIpAddress` | Normalized direct remote IP address, or absent | The direct inbound connection seen by the API; shown only to System Administrators. No client-controlled forwarded header is trusted. |
| `ClientDeviceKind` | `Desktop`, `Mobile`, `Tablet`, or `Unknown` | A coarse classification from the request user-agent; shown in Persian. The raw user-agent is never persisted. |

The fields apply to authentication, session, password, administration, and
Google-flow audit events that are emitted in an HTTP request. Provisioning and
other non-request records remain valid with absent attribution. Existing rows
also remain unchanged and appear as `ثبت نشده` in the administration audit.

The Application audit abstraction carries this request-scoped attribution so
all use cases record it consistently. Infrastructure persists it in the
existing immutable audit table; the API returns it only in the already
System-Administrator-protected dashboard and audit projections. The new
migration is additive and indexes neither value unless measured query evidence
later requires it.

## Fault investigation and regression boundary

The development audit history contains no successful `UserCreated` or
`DepartmentCreated` records, proving the reported attempts did not reach a
completed command. Current unit tests cover application happy paths, but no
browser test submits either management form and no authenticated API test
exercises the request-to-command binding. The correction adds both boundaries:

- a protected API integration test creates a root department and a user with
  the exact JSON contract, then asserts the safe response and audit record;
- browser tests open each modal, submit valid Persian RTL data, observe the
  refreshed list and closed dialog, and surface a safe response error without
  losing entered values.

These tests determine and prevent the precise request-path failure; no
unverified production-like record is created during diagnosis.

## Constraints and verification

- Existing user/department hierarchy, fixed-role, temporary-password,
  session-revocation, authorization, and no-secret rules remain unchanged.
- IP and device attribution are audit data, visible only through the existing
  System Administrator audit boundary. Retention remains deferred to the
  organization and IT policy owners.
- Backend tests cover absent/direct request attribution, persistence,
  projection, and protected mutation paths. Frontend tests cover Persian
  picker conversion and modal form outcomes. Browser verification covers
  desktop and phone layouts, including the one-line status bar.
- Before local IIS publication, create a verified development database backup,
  apply the new migration with `ASPNETCORE_ENVIRONMENT=Development`, and let
  the existing publisher preflight the exact expected migration.
