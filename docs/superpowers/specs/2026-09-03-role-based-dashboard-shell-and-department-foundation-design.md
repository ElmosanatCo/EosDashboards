# Role-based dashboard shell and department foundation design

**Status:** Approved design awaiting implementation-plan review

**Date:** 2026-09-03

## Purpose

Establish the smallest real organizational and role foundation needed for the
existing System Administrator to use a role-filtered workspace. Add empty
dashboard pages for department, human-resources, and CEO roles, and a compact
global command search that opens only authorized workspace targets.

This design does not implement administration forms, dashboard data, metrics,
workflows, or external source integrations.

## Confirmed rules

- The fixed system roles are `SystemAdministrator`, `DepartmentManager`,
  `HumanResourcesManager`, and `ChiefExecutiveOfficer`, displayed in Persian.
- A user may hold multiple roles.
- Every user belongs to exactly one department. More than one user may hold
  the Department Manager role for the same department.
- A department is independent or has one parent department; a child cannot
  itself have children. The maximum hierarchy depth is two.
- The existing bootstrap System Administrator account is also assigned the
  Department Manager role and the `نرم افزار` department. `فناوری اطلاعات`
  is a direct child of `نرم افزار`.
- Local password/SMS OTP and explicitly linked Google sign-in behavior remain
  unchanged. A Google identity remains an explicit server-side identity link,
  not self-registration.

## Data model and migration

Add a `Departments` table with a `bigint Id`, required Persian `Name`, nullable
`ParentDepartmentId`, creation/update instants, and a self-referencing foreign
key. The application layer prevents assigning a child as a parent, so a third
level cannot be created. A parent may have zero or more direct children.

Add a required `DepartmentId` foreign key to `Users`. The additive migration
creates the four fixed system roles idempotently, creates the two confirmed
departments, assigns the existing bootstrap System Administrator to `نرم افزار`,
and adds the Department Manager role to that account. It identifies that
account through its existing System Administrator role; it neither embeds nor
logs a person's name, mobile number, email, username, or password. If existing
data cannot be safely assigned, migration must fail rather than assign an
unconfirmed user to a department.

The exact department name/code uniqueness rule, account lifecycle fields,
department deletion/re-parenting, and manager assignment workflow remain for
the later administration design.

## Authorization and session contract

The server remains authoritative for all future data and operation endpoints.
The four stable role codes are resolved for the authenticated user and returned
with the existing session user summary, together with a minimal department
summary. Numeric role identifiers remain internal compatibility data and may
remain in the access token.

The React workspace consumes the stable role codes. Its route catalogue defines
the Persian title, path, icon, searchable labels, and required role codes for
each workspace target. This client-side filtering is a discoverability and
empty-page guard only; future APIs must enforce the same roles server-side.

## Workspace experience

Keep the existing persistent collapsible side menu, tab behavior, theme, and
RTL workforce-operations visual system.

The shared catalogue supplies these initial targets:

| Target | Required role | Current content |
| --- | --- | --- |
| داشبورد بخش | Department Manager | Title and honest no-data state |
| داشبورد منابع انسانی | Human Resources Manager | Title and honest no-data state |
| داشبورد مدیرعامل | Chief Executive Officer | Title and honest no-data state |

Users with several roles see every matching item. A direct restored or typed
workspace route without its required role resolves safely to the fixed home
tab. The System Administrator role does not by itself grant these three
role-specific dashboards.

The fixed header gains a compact central command search, visually aligned with
the approved reference image. `Ctrl+K` focuses it. It filters the same
role-filtered catalogue by Persian title/keywords and opens or activates the
matching internal tab. It does not return unavailable targets. Later approved
operations and dashboard elements will register in this same catalogue.

The screenshot reference guides compact hierarchy, not content:
`resources/images/references/manager-workforce-dashboard-reference.png`.
No depicted numbers, people, labels, workflows, metrics, or branding are
implemented or inferred.

## Tests and verification

- Domain/Application tests cover fixed role bootstrap, multi-role projection,
  the department parent-depth invariant, and no unsafe assignment behavior.
- SQL integration tests cover migration, parent/child persistence, user
  department assignment, and bootstrap data without personal-data assertions.
- API tests cover session responses with stable role codes and department
  summary while preserving local and Google authentication behavior.
- React component tests cover role-filtered sidebar/search results, `Ctrl+K`,
  tab opening, and unauthorized route recovery.
- Browser tests cover visible RTL search, multi-role navigation, compact layout,
  and the three honest empty states. The local IIS release is built from the
  committed `main` source and checked for API/UI/SPA-route readiness.

## Excluded from this slice

- User, role, department, or Google-identity administration screens.
- Creating arbitrary roles or granular per-user permissions.
- Dashboard data, charts, reports, filters, exports, notifications, or AI
  insight behavior.
- Parent-to-child visibility rules for future department dashboard data.
