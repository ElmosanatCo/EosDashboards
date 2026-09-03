# Requirements

**Last updated:** 2026-09-03

## Confirmed functional requirements

### FR-001 — Multiple management dashboards

**Status:** Confirmed

The system provides multiple dashboards for company managers.

Acceptance criteria will be defined after the initial dashboards, metrics, users, and data sources are identified.

### FR-002 — Department-oriented dashboard access

**Status:** Confirmed

Each manager can receive dashboards appropriate to the valuable data and responsibilities available in that manager's department. Exact assignments and permissions are deferred to dashboard discovery.

### FR-003 — CEO overview

**Status:** Confirmed

The CEO can monitor valuable company-wide information to support management decisions. Exact executive metrics and sources are deferred to dashboard discovery.

### FR-004 — Initial system administrator

**Status:** Confirmed

One active user is initially pre-provisioned directly through a controlled deployment tool and assigned the System Administrator role with full application access. The System Administrator must manage user accounts and their access assignments through the application. This does not permit self-registration or change the approved local-credential/SMS-OTP sign-in flow.

### FR-005 — Local credential sign-in with SMS OTP

**Status:** Confirmed for phase 1

The user signs in with a pre-provisioned username and password. After successful password verification, every new application session requires a valid SMS OTP sent to the mobile number stored for that user. Sign-in and password-recovery OTP messages are Persian branded messages that identify `داشبورد علم و صنعت` and state their distinct purpose. A signed-in user can change their password by supplying the current password. A user who has forgotten a password can reset it by completing a separate SMS OTP challenge. User and password administration UI remain deferred; the deployment tool manages pre-provisioned accounts and passwords in this slice.

### FR-008 — Pre-linked Google sign-in

**Status:** Confirmed for phase 1

An active, pre-provisioned user whose Google email has been explicitly linked
by the deployment-only administrator procedure may sign in through Google. The
first verified Google sign-in binds the immutable provider subject. It creates
the normal application session without a local password or SMS OTP. Google
does not self-register users, roles, permissions, or links, and the local
credential route remains available. If the application cannot reach Google
while starting the authorization flow, it returns the visitor to that local
sign-in page with an explicit temporary-unavailability message, never a
technical JSON error.

### FR-009 — Company department hierarchy

**Status:** Confirmed

The System Administrator must define company departments. A department may be
independent or an immediate child of one other department. The hierarchy has a
maximum of two levels: a parent department and its direct children; a third
level is not permitted.

### FR-006 — Tabbed SPA workspace

**Status:** Confirmed

The React application is an SPA whose opened pages appear in closable internal workspace tabs. The home tab is fixed, duplicate logical pages focus their existing tab, parameter-distinct pages may open separately, and tab descriptors survive refresh within the current session but are cleared on logout.

### FR-007 — Branding and status bar

**Status:** Confirmed

The UI displays the company name `علم و صنعت` and the approved EOS logo. The fixed bottom status bar displays the actual application version, live local-system time, and Persian-calendar date.

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

Manager-facing pages provide light, dark, and system appearance modes, defaulting to dark, plus selectable teal, indigo, emerald, amber, and rose interaction accents. The last applied appearance and palette are cached locally and rendered from the first sign-in-page paint, then synchronized with the authenticated user's server preference. The application shell has a fixed header, internal tab bar, and status bar; a persistent desktop side menu and temporary overlay side menu on phones; and a scrollable central workspace only. The header provides a compact global command search that returns only pages, operations, and eligible future in-dashboard elements permitted to the current user, and opens the chosen page in an internal tab.

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

The first release uses pre-provisioned local usernames and passwords followed
by mandatory SMS OTP, plus an optional server-owned Google route for explicitly
linked active users. First visit and post-logout states keep the local sign-in
form available. A valid application session signs the user in automatically.
Application authorization remains server-side. Passwords must be 8 to 128
characters long with no character-class composition rule; their plaintext
values are never stored or logged.

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

Persist the application server's local date and time only, at millisecond precision, using SQL Server `datetime2(3)`. Persisted-time names must not contain `Utc` or underscore-based time suffixes. Store no UTC value or offset and perform no Tehran-time conversion in normal application logic. Display dates with the Persian calendar and Persian digits using applicable local system time unless a feature explicitly requires another representation.

### TC-016 — Quality and operations

**Status:** Confirmed

Changes must satisfy the approved testing, security, observability, performance, deployment, backup, coding, and data-governance rules in `standards.md`.

### TC-017 — Initial supported stack

**Status:** Confirmed

The initial foundation uses .NET 10 LTS, EF Core 10, React 19.2, TypeScript, Material UI 9, Node.js 24 LTS, and Vite. Patch releases are locked by generated dependency files and maintained within the supported release lines.

### TC-018 — Independently openable frontend and backend

**Status:** Confirmed

The single repository contains a `backend/` Visual Studio solution and an independent `frontend/` VS Code application. They build, test, configure, and deploy separately.

### TC-019 — Configurable integration and session values

**Status:** Confirmed

Database, JWT/session, OTP, SMS service, and Google identity-provider settings
have typed API configuration sections. Under decision 0006, local development
server settings may be tracked in the private repository's API/IIS
configuration. Browser-delivered frontend configuration remains public only.
The logical development database name is `EosDashboard`.

## Unresolved requirements

- Dashboard catalogue, metrics, filters, and drill-down behavior.
- Detailed access policies beyond the four fixed initial roles, and whether later access assignments include granular permissions.
- Exact user-account lifecycle operations, including activation, deactivation, password administration, and identity-link management.
- Department metadata, manager assignment, deletion, and re-parenting rules.
- Data sources, ownership, refresh frequency, and historical retention.
- Exact performance service levels, availability targets, retention periods, approved organizational browser versions, and recovery objectives.
- Exporting, printing, alerts, subscriptions, and administration capabilities.
- Exact external-access identity topology and whether LDAP is distinct from Active Directory.
- Whether a stronger additional factor should supplement or replace SMS OTP in a later release.
