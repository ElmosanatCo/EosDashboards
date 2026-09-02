# Requirements

**Last updated:** 2026-09-02

## Confirmed functional requirements

### FR-001 — Multiple management dashboards

**Status:** Confirmed

The system provides multiple dashboards for company managers.

Acceptance criteria will be defined after the initial dashboards, metrics, users, and data sources are identified.

## Confirmed technical constraints

### TC-001 — Web application

**Status:** Confirmed

The product is delivered as a web application.

### TC-002 — Backend baseline

**Status:** Confirmed

The backend uses .NET Core, Entity Framework Core, and REST APIs.

### TC-003 — Database baseline

**Status:** Confirmed

The database platform is SQL Server.

### TC-004 — Frontend baseline

**Status:** Confirmed

The frontend uses React.js.

### TC-005 — Persian RTL interface

**Status:** Confirmed

The initial product language is Persian and the interface is RTL-first. The localization foundation must permit future English/LTR support.

### TC-006 — Appearance and shell

**Status:** Confirmed

The application provides light, dark, and system appearance modes plus selectable color palettes. The default palette is navy/teal. The application shell has a fixed header, fixed status bar, collapsible persistent side menu, and a scrollable central content area.

### TC-007 — Typography and component foundation

**Status:** Confirmed

The primary typeface is locally hosted Vazirmatn. Material UI is the frontend component foundation. Paid MUI X capabilities require explicit approval.

### TC-008 — Accessibility

**Status:** Confirmed

User-facing features target WCAG 2.2 level AA, including keyboard access, visible focus, adequate contrast, non-color-only status communication, usable target sizes, and reduced-motion support.

### TC-009 — Layering

**Status:** Confirmed

The backend uses lightweight clean layering: API calls Application, Application coordinates Domain behavior, and Infrastructure alone implements database and external-system access. API contracts are not persistence entities.

### TC-010 — Identity and session experience

**Status:** Confirmed for phase 1

The first release is intranet-only. First visit and post-logout states show one organizational sign-in button. A valid application session signs the user in automatically. Windows/Active Directory can establish organizational identity; all application authorization remains server-side.

### TC-011 — Separate hosting

**Status:** Confirmed

The React UI and ASP.NET Core API are independently hosted on IIS with separate sites/applications, application pools, configuration, and least-privilege identities.

### TC-012 — Entity Framework schema management

**Status:** Confirmed

Entity Framework Code First migrations are the schema source of truth. Production schema changes use reviewed, tested, archived deployment scripts or bundles and never automatic startup migration.

### TC-013 — Primary keys

**Status:** Confirmed

Principal application tables use an auto-incrementing SQL Server `bigint` primary key named `Id`, and corresponding foreign keys use `bigint`. Only documented junction-table, keyless-read-model, and framework-owned exceptions apply.

### TC-014 — Project resources

**Status:** Confirmed

Reusable logos, fonts, icons, images, and document templates are managed from the repository-level `resources/` hierarchy with ownership and licensing metadata.

### TC-015 — Date and time

**Status:** Confirmed

Persist instants in a normalized universal representation. Display dates using the Persian calendar, Persian digits, and the Asia/Tehran time zone unless a feature explicitly requires another representation.

### TC-016 — Quality and operations

**Status:** Confirmed

Changes must satisfy the approved testing, security, observability, performance, deployment, backup, coding, and data-governance rules in `standards.md`.

## Unresolved requirements

- Dashboard catalogue, metrics, filters, and drill-down behavior.
- User roles, authorization boundaries, and dashboard visibility.
- Data sources, ownership, refresh frequency, and historical retention.
- Exact performance service levels, availability targets, retention periods, approved organizational browser versions, and recovery objectives.
- Exporting, printing, alerts, subscriptions, and administration capabilities.
- Exact external-access identity topology and whether LDAP is distinct from Active Directory.
- Future SMS OTP or stronger second-factor mechanism.
