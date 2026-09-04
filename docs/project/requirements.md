# Requirements

**Last updated:** 2026-09-04

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

The user signs in with a pre-provisioned username and password. After successful password verification, every new application session requires a valid SMS OTP sent to the mobile number stored for that user. Sign-in and password-recovery OTP messages are Persian branded messages that identify `داشبورد علم و صنعت` and state their distinct purpose. A signed-in user can change their password by supplying the current password. A user who has forgotten a password can reset it by completing a separate SMS OTP challenge. System Administrators manage application users and administrator password resets through the protected administration workspace; the deployment tool remains responsible for bootstrap provisioning and Google-link management.

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

### FR-010 — System administration and audit visibility

**Status:** Confirmed

The System Administrator manages users, assignments of the four fixed roles,
and company departments through the application. Accounts are activated or
deactivated rather than deleted. The System Administrator also receives an
operational dashboard and filtered audit history for administration and
security actions, including sign-ins, failed attempts, password changes, and
access changes. Request-originated entries show a direct IP and coarse device
kind, never a raw user-agent. The dashboard's user-presence metric is active
sessions, not live browser presence.

Audit filtering is user-facing: events are selected by their Persian labels,
and the actor and affected subject are selected from the managed-user list by
name and personnel code while the API receives their internal IDs. The audit
page keeps its title, filters, and refresh action fixed; only the audit-history
table scrolls when the result set is long. Date-selection controls use the
Persian calendar and keep time in a separate field.

User and department create/edit operations open as responsive dialogs above
their management page rather than as extra workspace tabs. The form surface
fills the dialog content width so desktop layouts do not introduce an
unintentional side gutter; entered values remain visible when a save fails.

### FR-011 — Department job-description management and quality analysis

**Status:** Confirmed for the initial department-manager design

Department Managers manage job-description drafts for personnel within their
authorized department scope, including all of their child departments. They can
list personnel and description status, view, edit, download, create manually,
and upload one or more Excel files. Excel input is normalized into the approved
standard format and reports independent success or failure for each file. After
standardization, the normalized structured data is also persisted in the
database first. The standard Excel file is then generated from the persisted
database version and the resulting file is stored in the database, linked to
that same version for display and download. The structured database data is the
sole source for search, statistics, quality analysis, and approval workflow;
the generated Excel artifact is never re-read as the source of dashboard
statistics.

When creating or editing a description, the manager selects the target
department from their own department and child departments. The manager has
full job-description management authority within that scope. The department
dashboard provides an all-managed-departments view and a view filtered to any
one department in that scope.

Every manager-facing department selector that supports a combined view must
show `همه بخش‌ها` as its first selectable option and represent that choice with
an explicit all-value, not an empty value. Selecting it loads the combined view;
selecting a named department loads only that department.

The first department dashboard reads its statistics from the structured
database and includes personnel counts, active and archived personnel counts,
healthy and incomplete description counts, workflow-status counts, skill and
task counts, skill coverage and identified gaps, department breakdowns, and
manager actions such as approving descriptions and following up incomplete
records. The parent Department Manager owns the child-department workflow;
child departments do not have separate job-description approval inboxes.

The dashboard also includes active project counts and the number of active
people working on each project. Project statistics come from active database
assignments, not generated Excel artifacts.

Missing optional values do not fail an import. An explicitly supplied
department outside the manager's authorized scope fails that file with a
clear reason. Empty or explanatory rows may be ignored, useful extra columns
are appended to the related task description, and task numbering is normalized
in the generated standard draft.

The task start date is optional, but an empty date makes the data-quality
status `ناقص`. A personnel code is required for a manager-created or revised
record and must be entered before it can be saved from the form. It is not part
of the Excel format; an imported workbook may be retained as `ناقص` until the
manager supplies the missing code. Excel sheet names and source-column names are not contractual;
the importer identifies supported content and common labels from the workbook.
Manager create and edit forms select the authorized target department before
catalog choices are made; newly created department-scoped skills and tasks use
that target, while a checked public skill is available across departments.
Task dates in these forms are selected as dates without a time component. When
an imported skill is mapped to a catalog skill, that catalog skill is shown as
selected in the skill list; changing the mapping removes the previous visual
selection and selects the new catalog skill.

Every change creates retained version history that can be compared and
reported. A version comparison identifies changed profile fields, selected
skills, task titles, task dates, and free-text descriptions.

Each catalog task may be marked as a project. A project task can be assigned to
multiple personnel, allowing the dashboard to count active projects and the
people working on each project. Each personnel-task record may have an optional
start date and optional end date. A missing end date means the task is active;
an end date in the past makes it inactive. Inactive ended tasks remain in the
database and version history but are omitted from the current generated Excel
artifact. Each database task assignment also stores the required average weekly
workload in hours for future workload-pressure and available-capacity analysis;
this internal field is not part of the Excel format or generated workbook.

The approval workflow status is separate from data quality status. A newly
created or revised complete record is `منتظر تأیید` until the Department
Manager reviews and confirms it. Any record with quality status `ناقص` instead
has workflow status `منتظر رفع نقص` and cannot be sent for approval. After the
manager resolves all missing or unlinked information, it returns to `منتظر
تأیید` for an explicit manager review. A record sent onward is `در حال بررسی`
while it awaits Human Resources review. A record is `تأیید شده` and active only
after both the Department Manager and Human Resources have approved it. Human
Resources may return a record as `رد شده` with a reason; the manager can revise
and resubmit it. A departed person's approved record may be `آرشیو شده`
without deleting its history.

Independently, the data quality status is `سالم` when the required information
is present and all imported skills and tasks are linked to catalog values, and
`ناقص` when one or more required fields are empty or an imported skill/task is
still unlinked. A record may be visible to managers and Human Resources while
`ناقص`, but it cannot pass the manager approval action or enter Human Resources
review until it becomes `سالم`.

Skills and task titles are catalog values and must be unique within their
approved department scope. Each department has its own task catalog; a task
title is not a global organization-wide value and a task from one department
is not automatically available in another. Only task descriptions are free
text. The standard Excel file
contains the person's selected skills but does not contain required skills for
each task. Managers maintain task-to-required-skill relationships in the
database. Within each department in the manager's own and child-department
scope, the Department Manager owns that department's task catalog and may
define task titles and accept or reject task suggestions. Manual skill or task
values may become reviewable catalog suggestions; they are not silently added.
The manager can select or remove authorized public and department-specific
skills in manual and edit forms. Public skills are visible to all managers;
their final edit/delete authority remains with Human Resources once another
department uses them. The department manager who registers a public skill may
edit or deactivate it while its usage remains within that manager's department;
the API must enforce this ownership and usage boundary.

Catalog names and task titles that already exist must produce an explicit,
actionable conflict in the form; the request must not fail silently and the
entered form values remain available for correction. Deactivated catalog
skills and tasks are hidden from the active view but can be shown with an
inactive/all filter and reactivated by an authorized manager or Human Resources
operator. Reactivation is a consequential state change and therefore requires
the same explicit Persian confirmation; cancelling it sends no mutation request.

When resolving an imported value, the manager can choose any public skill or a
skill specific to the target department, or create a new public or
department-specific skill. A new task can be marked as a project during the
same resolution flow; an existing task's project state is shown when selected.

The system may analyze a description against the skill and task catalogs for
missing, unsupported, incomplete, or conflicting entries. Initial analysis is
deterministic: catalog tasks explicitly map to required skills, and the system
compares those requirements with the person's selected skills. Findings are
evidence-linked review suggestions and never automatically change a record,
catalog, or approval status. No external AI service is required or assumed for
this initial capability.

### FR-006 — Tabbed SPA workspace

**Status:** Confirmed

The React application is an SPA whose opened pages appear in closable internal workspace tabs. The home tab is fixed, duplicate logical pages focus their existing tab, parameter-distinct pages may open separately, and tab descriptors survive refresh within the current session but are cleared on logout.

### FR-007 — Branding and status bar

**Status:** Confirmed

The UI displays the company name `علم و صنعت` and the approved EOS logo. On phone-sized layouts, the brand moves from the fixed header to the temporary navigation menu; desktop layouts keep it in the header. The fixed bottom status bar displays the actual application version, live local-system time, and Persian-calendar date.

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

Manager-facing pages provide light, dark, and system appearance modes, defaulting to dark, plus selectable teal, indigo, emerald, amber, and rose interaction accents. The last applied appearance and palette are cached locally and rendered from the first sign-in-page paint, then synchronized with the authenticated user's server preference. The application shell has a fixed header, internal tab bar, and status bar; a persistent desktop side menu and temporary overlay side menu on phones; and a scrollable central workspace only. Every user-visible number uses Persian digits, including numbers embedded in usernames, passwords when revealed, personnel/organizational identifiers, versions, masked contact values, dates, times, counts, and IP addresses. Values submitted to APIs and internal technical identifiers retain their required LTR/ASCII representation. The header provides a compact global command search that returns only pages, operations, and eligible future in-dashboard elements permitted to the current user, and opens the chosen page in an internal tab.

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

Entity Framework Code First migrations are the schema source of truth. Production
schema changes use reviewed, tested, archived deployment scripts or bundles and
never automatic startup migration. Every production API release that contains a
pending migration must include an automated or operator-triggered deployment
stage that runs the matching migration artifact with a separately authorized
deployment identity before switching IIS to the new API. The developer does not
need direct database access, and the runtime API identity must not receive schema
change permission. If the migration stage fails, the API release must not be
activated.

### TC-013 — Primary keys

**Status:** Confirmed

Principal application tables use an auto-incrementing SQL Server `bigint` primary key named `Id`, and corresponding foreign keys use `bigint`. Only documented junction-table, keyless-read-model, and framework-owned exceptions apply.

### TC-014 — Project resources

**Status:** Confirmed

Reusable logos, fonts, icons, images, and document templates are managed from the repository-level `resources/` hierarchy with ownership and licensing metadata.

### TC-015 — Date and time

**Status:** Confirmed

Persist the application server's local date and time only, at millisecond precision, using SQL Server `datetime2(3)`. Persisted-time names must not contain `Utc` or underscore-based time suffixes. Store no UTC value or offset and perform no Tehran-time conversion in normal application logic. Display and select dates with the Persian calendar and Persian digits using applicable local system time unless a feature explicitly requires another representation.

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

- Any final visual/layout details of the standard workbook template.
- Exact duplicate-matching and suggestion presentation details for the
  department task catalog.
- Dashboard catalogue, metrics, filters, and drill-down behavior beyond the
  approved System Administrator dashboard.
- Detailed access policies beyond the four fixed initial roles, and whether later access assignments include granular permissions.
- Google identity-link management and account lifecycle behavior beyond the
  approved local-account administration rules.
- Department metadata and manager-assignment policy beyond the approved
  two-level structure, uniqueness, deletion, and re-parenting rules.
- Data sources, ownership, refresh frequency, and historical retention.
- Exact performance service levels, availability targets, retention periods, approved organizational browser versions, and recovery objectives.
- Exporting, printing, alerts, subscriptions, and administration capabilities.
- Exact external-access identity topology and whether LDAP is distinct from Active Directory.
- Whether a stronger additional factor should supplement or replace SMS OTP in a later release.
