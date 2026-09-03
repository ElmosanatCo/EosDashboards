# System administration and audit dashboard design

**Status:** Approved; implementation plan pending

**Date:** 2026-09-03

## Purpose

Deliver the first operational System Administrator slice: user-account and
fixed-role management, company-department management, and a role-protected
operational dashboard with security and administration audit visibility.

This slice does not create arbitrary roles, granular permissions, Google-link
administration, data-source dashboards, exports, alerts, or a retention
policy.

## Authorization and account rules

- Only `SystemAdministrator` may call or discover the administration routes,
  pages, dashboard, and audit data. The API enforces this independently of the
  client workspace catalogue.
- The fixed roles remain `SystemAdministrator`, `DepartmentManager`,
  `HumanResourcesManager`, and `ChiefExecutiveOfficer`. Administrators assign
  or remove these roles but cannot alter their definitions.
- Every user, including an inactive user, belongs to exactly one department;
  active users also have at least one fixed role. A user is never permanently
  deleted; administrators create, edit, activate, or deactivate accounts.
- Create and administrator password-reset operations set a temporary password.
  After local password verification and the existing SMS OTP, the user must
  choose a new password before using the workspace. Reset, deactivation, and
  every other administrative change to a target account revoke that target's
  active sessions; the acting administrator's session remains intact.
- The API refuses to deactivate the last active System Administrator or remove
  that role from that user.
- A user profile contains the existing organizational identifier (displayed as
  `کد پرسنلی`), account name, first and last name, protected mobile number,
  username, department, and roles. The personnel code is unique and may be
  corrected. Username is optional on creation; an omitted value becomes the
  personnel code. Username changes are independent of later personnel-code
  corrections and remain unique.
- Lists never reveal mobile numbers. Edit forms show only the existing masked
  value and accept a replacement. Passwords, OTPs, and full mobile values never
  appear in audit data, API list/detail projections, logs, or UI history.

## Department rules

- A department is independent by default. An explicit form choice may make it
  a child of one independent department; the parent selector never offers a
  child, so the hierarchy cannot exceed two levels.
- Department names are unique across the organization. Administrators may
  rename a department, change a child to a different independent parent, or
  make a child independent.
- A department with users or child departments cannot be deleted. Users must
  be reassigned and child departments moved or made independent first. A parent
  with children cannot itself become a child.

## Data and application design

The Domain owns account-state, role, department-depth, temporary-password, and
last-active-administrator invariants. Application use cases own authorization,
auditing, session revocation, and transaction boundaries. Infrastructure adds
repository projections, server-side filtering/paging, SQL constraints and
indexes, and optimistic concurrency tokens on mutable user and department
records. The migration is additive and preserves existing data.

Administration APIs are versioned below `/api/v1` and provide paged user lists,
safe user details, create/update, activation state, password reset, fixed-role
reference data, department tree/list, and department create/update/delete
operations. Conflict, duplicate, hierarchy, non-empty-delete, and
last-administrator failures return safe problem details for Persian UI mapping.

Every administrative mutation writes an immutable audit record with event code,
success/failure, local server timestamp, actor identifier when known, target
identifier when applicable, and correlation identifier. Safe metadata may state
which field category changed, never its confidential value. Existing
authentication, OTP, password, session, and Google audit events remain part of
the same stream.

## Workspace and dashboard experience

The role-filtered workspace catalogue adds these System Administrator targets:

| Target | Route purpose |
| --- | --- |
| `داشبورد مدیر سامانه` | Operational summary and latest audit events |
| `مدیریت کاربران` | Paged user directory and closable create/edit workspace tabs |
| `مدیریت واحدها` | Two-level organizational tree and unit operations |
| `ممیزی سامانه` | Filtered, paged administration and security audit history |

The two management pages remain separate workspace destinations, as selected
in the approved layout review. They follow the dark-default RTL
workforce-operations system: compact flat accent-line panels, structured
tables, explicit empty/loading/error/denied states, responsive desktop-first
layouts, and no simulated activity.

The System Administrator dashboard is read from the same authoritative audit
and session data. It shows the latest audit records with Persian descriptions,
time, result, actor, and affected user/subject; a direct link opens the filtered
audit workspace. It also shows these truthful 24-hour metrics: successful
sign-ins, failed sign-in/security attempts, active users, inactive users, and
users with an unrevoked, unexpired session. The last metric is labelled
`کاربران دارای نشست فعال`; it does not claim live browser presence. The audit
workspace supports 7-day, 30-day, and custom date ranges plus applicable event,
actor, target-user, and result filters.

## Verification

- Domain and Application tests cover every account/department invariant,
  temporary-password flow, session revocation, immutable audit creation, and
  24-hour dashboard calculations.
- SQL integration tests cover migration, uniqueness, restrictive deletion,
  filtering/paging, concurrency, aggregate counts, and personal-data-safe
  projections.
- API tests cover System Administrator authorization, safe failures, audit
  filters, management actions, and preserved local/Google authentication.
- React component and browser-flow tests cover role-filtered discovery,
  desktop/phone RTL layouts, forms, confirmations, error states, management
  tabs, audit navigation, and metrics backed by mocked API data.

## Deferred decisions

- Audit/log retention, ownership, and access outside the System Administrator
  role require organizational and IT approval.
- Google identity-link management, custom roles, granular permissions, exports,
  alerts, and employee-data sources remain outside this slice.
