# Architecture

**Last updated:** 2026-09-03

## Accepted baseline

EosDashboards is a web application with:

- a React.js frontend;
- a .NET Core backend exposing REST APIs;
- Entity Framework Core for data access;
- SQL Server as the database platform.

The initial supported stack is .NET 10 LTS, EF Core 10, React 19.2, TypeScript, Material UI 9, Node.js 24 LTS, and Vite. Patch versions are resolved and locked during scaffolding.

## Repository topology

The repository keeps independently openable applications:

```text
backend/
  EosDashboards.sln
  src/{Domain,Application,Infrastructure,Api}
  tools/EosDashboards.AdminProvisioner
  tests/
frontend/
  src/
  public/
  tests/
docs/
resources/
```

The backend opens in Visual Studio. The frontend opens independently in VS Code. They build, test, configure, publish, and host separately.

## Conceptual data flow

The confirmed high-level direction is:

`React UI -> REST API -> Application -> Domain`

`Infrastructure -> Domain/Application ports -> SQL Server, AD/LDAP, and approved external systems`

The API is thin. Application coordinates use cases and transaction boundaries. Domain contains business concepts and rules. Infrastructure alone accesses EF Core, SQL Server, directory services, and other external systems. Domain persistence entities are never exposed as public API contracts.

The API project carries `Microsoft.EntityFrameworkCore.Design` as private design-time-only tooling metadata because EF CLI commands use API as the startup project and require a direct reference there. API source and runtime behavior do not access EF Core or the database; Infrastructure remains the sole database-access layer.

## Identity boundary

Phase 1 uses local credentials for active pre-provisioned database users. A username is unique and a password is stored only as a standard salted password hash. Every new application session requires successful username/password verification followed by a six-digit SMS OTP. Passwords are 8 to 128 characters long with no character-class composition rule. The OTP is valid for five minutes, permits five verification attempts, and has a 60-second resend cooldown.

After OTP verification, the application creates an eight-hour session. JWT access tokens normally expire ten minutes after issuance and are held in browser memory. Refresh remains available at every instant strictly before the absolute session expiry; the final access token is shortened to end at that expiry and never outlives the session. Tokens are renewed through a hashed, revocable refresh credential carried only by a Secure, HttpOnly cookie. Logout or session expiry revokes access and returns to the local sign-in form. Plaintext local passwords are never stored.

An enabled Google Web OAuth client supports a separate server-owned OpenID
Connect Authorization Code flow with PKCE. Google sign-in is available only to
an active user with an explicitly provisioned `ExternalIdentityLinks` record.
The first verified email match binds Google's stable subject; later sign-ins
use that subject. The API creates the same session and refresh cookie used by
OTP verification. React discovers only whether Google is enabled and never
receives the client secret, authorization code, ID token, or linked email.

The first user is created or updated before application startup with an idempotent deployment-only administrator provisioning tool and receives the System Administrator and Department Manager roles in the `نرم افزار` department. The fixed system roles are System Administrator, Department Manager, Human Resources Manager, and CEO; users can hold several roles and every user has one department. Personal profile values and application-user passwords enter through secure runtime input and never enter source control. Under decision 0006, the private repository may track approved local-development, server-side runtime settings such as database connection values, service endpoints, API security keys, and Google OAuth configuration; they never enter frontend artifacts, documentation, logs, or production configuration. A signed-in user changes a password by verifying the current password; password recovery completes a purpose-isolated SMS OTP challenge. Password changes and resets revoke that user's active sessions.

The company SMS service is integrated through an Infrastructure adapter for the `SendSmsMessage` SOAP operation. The endpoint and timeouts are typed configuration values; automated tests use a fake sender and never contact the real service. A send timeout is not automatically retried because the service does not provide an idempotency contract.

Organizational directory and federation-provider access is deferred until IT discovery. Any future directory integration is server-side, protected by TLS with certificate validation, and never exposed directly to browsers or the internet.

## Hosting topology

- React UI and ASP.NET Core API are separate IIS sites/applications and separate application pools.
- The SQL Server database and IIS targets currently available on the developer's machine are development-only targets for local integration and hosting. They are not production infrastructure.
- The initial vertical slice is published to those local development IIS targets. The UI and API readiness endpoints return HTTPS HTTP 200. The API uses anonymous IIS access at its transport boundary; application authentication is enforced by its credential, OTP, JWT, refresh-cookie, and authorization controls. End-to-end live sign-in remains blocked by the configured external SMS endpoint timing out.
- Production deployment will occur later on separate company servers using separately approved hostnames, certificates, identities, configuration, secrets, and operational controls.
- Only approved UI origins are allowed by API CORS policy.
- HTTPS is mandatory and HSTS is enabled in production.
- Environments, databases, configuration, secrets, deployment artifacts, and least-privilege runtime identities are separated.
- Production deployments use versioned artifacts, controlled offline handling, smoke tests, health probes, and a documented rollback path.

## Data architecture

- EF Core Code First migrations are authoritative.
- Only Infrastructure accesses the database.
- Principal tables use auto-incrementing `bigint` keys named `Id`; narrow exceptions are documented in `standards.md`.
- Lazy loading is disabled. Reads favor projections and no tracking. Large results are filtered and paged on the server.
- Business-operation transaction boundaries belong to Application and are implemented through Infrastructure.
- The initial migration contains `Users`, `Roles`, `UserRoles`,
  `OtpChallenges`, `UserSessions`, `UserPreferences`, and `AuditLogs`; the
  additive `ExternalIdentityLinks` migration holds explicit provider links.
- The department migration adds a required user department, a two-level
  self-referencing `Departments` table, fixed roles, and the `نرم افزار` /
  `فناوری اطلاعات` organizational baseline. It fails rather than guessing an
  assignment for an existing user without the System Administrator role.
- Mobile numbers are encrypted at application level with protected keys outside source control. OTP codes and refresh credentials are stored only as keyed hashes.
- The System Administrator slice uses optimistic concurrency for mutable user
  and department records, server-paged safe projections, immutable audit
  queries, and a 24-hour operational summary. User and department mutations
  revoke the target user's sessions except the acting administrator's own
  session, and retain no password, OTP, or full mobile value in audit metadata.

## Frontend workspace

The frontend is an RTL-first React SPA. Internal workspace tabs are route-aware descriptors rather than permanently mounted page trees. Only the active page is rendered while serializable page/filter state is preserved, limiting memory growth.

- Home is a fixed non-closable tab.
- Reopening the same logical route focuses its tab; materially different route parameters form a distinct tab key.
- Individual, other, and all closable tabs can be closed. Dirty pages require confirmation.
- The active tab controls the browser URL and supports navigation history.
- Session tab descriptors survive refresh but are cleared on logout.
- Overflow is accessible through a compact list on narrow displays.
- The sidebar, route guard, and `Ctrl+K` command search share one
  role-filtered target catalogue. Department, Human Resources, and CEO pages
  remain honest no-data dashboards. The approved System Administrator slice
  adds its operational dashboard, users, departments, and audit targets;
  client filtering is not API authorization.

The supplied EOS logo and company name `علم و صنعت` appear on sign-in and in the shell. The fixed status bar displays build-derived version, live local-system time, and Persian-calendar date.

## Cross-cutting architecture

Persisted application times are local wall-clock values produced by the application server clock and truncated to millisecond precision. Domain and Application properties use names such as `CreatedAt` and `ExpiresAt`; Infrastructure maps them to SQL Server `datetime2(3)`. No persisted property or API contract uses a `Utc` name, no database timestamp stores an offset, and normal application logic does not perform an Asia/Tehran conversion. External protocol adapters may create a transient protocol-required representation without changing this local persistence boundary.

- Central exception handling returns safe standard error objects with trace identifiers.
- Transport and background adapters provide the current correlation identifier through an Application port; every audit record created by one operation uses that same identifier.
- Structured logging, audit records, liveness, readiness, metrics, and alerting support operations.
- REST endpoints are versioned under `/api/v1` and documented through OpenAPI.
- Caching is explicit, bounded, permission-safe, and measured.
- Long-running reports or calculations execute as background work when required.

## Unresolved architecture topics

- Operational database versus analytical or warehouse data sources.
- Data ingestion, synchronization, caching, and refresh strategy.
- Dashboard rendering and charting libraries.
- Exact external identity provider/topology and directory integration protocol.
- Configuration provider, secret store, monitoring product, backup schedule, and disaster-recovery objectives.
