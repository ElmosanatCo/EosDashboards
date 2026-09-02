# Initial Authentication and Tabbed Application Shell Design

**Date:** 2026-09-02

**Status:** Approved in conversation; awaiting written-spec review

## 1. Goal

Create the first production-shaped vertical slice of EosDashboards: separately maintainable backend and frontend foundations, controlled initial administrator provisioning, Windows/AD organizational sign-in, mandatory SMS OTP, secure application sessions, and a polished Persian RTL tabbed shell ready for future dashboards.

No real dashboard, user-management screen, external/internet authentication, or charting library is included.

## 2. Product context

Department managers will eventually receive dashboards based on the valuable data and responsibilities available in their departments. The CEO will receive a broader company-wide view for monitoring and management decisions. This slice establishes the secure shared platform but does not define those dashboards or their metrics.

The initial database user is a pre-provisioned System Administrator with full application access. Personal values supplied for that user are runtime deployment input and are intentionally absent from this specification and repository history.

## 3. Technology baseline

- Backend: .NET 10 LTS, ASP.NET Core 10, EF Core 10, SQL Server.
- Frontend: React 19.2, TypeScript, Material UI 9, Vite, Node.js 24 LTS.
- Primary font: self-hosted Vazirmatn.
- Database logical name: `EosDashboard`.
- Production host: separate IIS applications/sites and separate application pools for UI and API.

Generated lock files pin actual patch-level dependencies. Supported security patches are maintained within these approved release lines.

## 4. Repository and solution layout

```text
backend/
  EosDashboards.sln
  src/
    EosDashboards.Domain/
    EosDashboards.Application/
    EosDashboards.Infrastructure/
    EosDashboards.Api/
  tools/
    EosDashboards.AdminProvisioner/
  tests/
    EosDashboards.Domain.Tests/
    EosDashboards.Application.Tests/
    EosDashboards.IntegrationTests/
frontend/
  src/
  public/
  tests/
  package.json
docs/
resources/
```

The backend opens through `backend/EosDashboards.sln` in Visual Studio. The frontend opens independently through `frontend/` in VS Code. Neither application embeds the other's source or build output. Each has independent configuration, build, test, and publish commands.

## 5. Backend boundaries

### Domain

Owns user, role, OTP, session, and audit business state and invariants without framework, database, directory, or SMS dependencies.

### Application

Owns sign-in use cases and ports for user/session persistence, organizational identity, SMS delivery, time, randomness, cryptographic protection, and audit recording. It determines outcomes but not HTTP/SOAP/database details.

### Infrastructure

Implements EF Core persistence, SQL Server access, Windows identity adaptation, mobile encryption, keyed hashing, protected key use, and the SOAP SMS client. It is the only database/external-system access layer.

### API

Owns HTTP contracts, authentication scheme wiring, CORS, anti-forgery behavior, centralized error responses, OpenAPI, health endpoints, and dependency composition. Controllers contain no business logic.

## 6. Data model

All principal keys are auto-incrementing SQL Server `bigint` values named `Id`.

### Users

- stable organizational identifier, unique;
- current domain/account name and display data;
- first and last name;
- encrypted normalized mobile number and masked display value;
- active flag and audit timestamps.

### Roles

- unique stable role code;
- Persian display name;
- active/system flags.

The initial stable code is `SystemAdministrator`. Authorization grants it every phase-1 policy.

### UserRoles

A pure junction with a composite `UserId`/`RoleId` key.

### OtpChallenges

- owning user;
- opaque random public challenge token distinct from the numeric key;
- keyed hash of the six-digit code, never plaintext;
- created/expiry/consumed timestamps;
- failed-attempt count, send result, and terminal status.

### UserSessions

- owning user;
- keyed hash of the refresh credential;
- created, absolute expiry, last refresh, revocation time and reason;
- audit-safe client/network metadata where approved.

### UserPreferences

- unique user relationship;
- appearance mode (`light`, `dark`, or `system`);
- selected palette, initially navy/teal;
- side-menu collapsed state.

### AuditLogs

Immutable application records for sign-in, OTP send/verify, refresh, logout, provisioning, and security failures. They contain actor/subject, event code, time, result, trace identifier, and safe metadata—never codes, tokens, complete mobile numbers, or credentials.

## 7. Initial administrator provisioning

`EosDashboards.AdminProvisioner` is a deployment-only console tool. It receives the connection, organizational identity, personal attributes, and mobile number through secure runtime input. It:

1. validates and normalizes input;
2. verifies the target schema;
3. creates or locates the system role;
4. creates or updates the matching organizational user;
5. assigns the System Administrator role idempotently;
6. writes an audit record;
7. returns a safe result without echoing secrets or the complete mobile number.

The tool does not run automatically with the API. No real administrator data appears in migrations, tracked settings, test fixtures, or logs.

## 8. Sign-in and session flow

1. The unauthenticated SPA shows one organizational sign-in button.
2. The browser calls the challenge endpoint with credentials enabled; IIS/ASP.NET Core supplies Windows identity.
3. The API maps the stable organizational identifier to one active pre-provisioned user. Unknown or inactive users receive a generic denial and an audit record.
4. Application generates a cryptographically secure six-digit OTP, stores only its keyed hash, and requests SMS delivery through the application port.
5. Successful delivery returns an opaque challenge token plus masked mobile, expiry, and resend timing. No session exists yet.
6. Verification accepts the challenge token and code. Five failed attempts, five-minute expiry, consumption, supersession, or invalid status terminates the challenge.
7. Success consumes the challenge atomically, creates an eight-hour session, returns a ten-minute JWT access token in the response body, and places the random refresh credential in a Secure, HttpOnly cookie. Only its keyed hash is persisted.
8. The SPA holds the access token in memory. Refresh rotates the credential and access token within the same absolute eight-hour boundary.
9. Logout revokes the session, expires the cookie, clears client auth/tab state, and returns to sign-in. Expiry has the same user-visible outcome and requires a new OTP.

OTP resend has a 60-second cooldown, invalidates the prior usable challenge, and is rate-limited per organizational user and network source. Because the SOAP operation has no idempotency contract, an ambiguous timeout is not retried automatically.

## 9. API surface

```text
POST /api/v1/auth/challenges
POST /api/v1/auth/challenges/{token}/verify
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me

GET  /api/v1/users/me/preferences
PUT  /api/v1/users/me/preferences

GET  /health/live
GET  /health/ready
```

Only challenge creation consumes the IIS-established Windows identity. Normal protected API calls require the application JWT. Cookie-changing endpoints apply exact-origin CORS, secure cookie policy, and anti-forgery/origin defenses. Errors use a consistent safe problem-details response with trace identifier.

## 10. SMS integration

Infrastructure adapts the approved company SOAP service operation `SendSmsMessage(message, mobile) -> boolean`. Endpoint, timeout, and related non-secret behavior settings are typed API configuration. Environment-specific values and any credentials remain outside tracked settings.

The adapter validates response shape, maps failure and timeout to explicit Application outcomes, masks sensitive logging, and accepts cancellation. Automated tests replace it with a deterministic fake; they never send real messages.

## 11. Frontend experience

### Sign-in

The centered RTL sign-in card displays the approved EOS logo, company name `علم و صنعت`, application title, appearance control, and one organizational sign-in button. It has explicit loading and traceable safe error states.

After identity recognition, the OTP view displays the user's name, masked mobile, six accessible digit inputs, five-minute countdown, resend cooldown, cancel/back action, and error/status feedback that does not rely on color alone.

### Authenticated shell

- fixed top header with branding, current user, System Administrator role, theme/palette controls, and logout;
- fixed bottom status bar with build-derived application version, live Asia/Tehran clock, and Persian-calendar date using Persian digits;
- persistent collapsible hamburger side menu;
- only the central workspace scrolls;
- initial home page with a welcome state and a notice that dashboards will be added later.

The supplied black/red SVG remains geometrically and chromatically unchanged. Until a dark-background variant is provided, dark mode presents it on a controlled light/neutral backing surface for contrast.

### Internal workspace tabs

The SPA represents every opened page as an internal tab descriptor:

- home is fixed and cannot close;
- opening an existing logical route focuses it;
- relevant route parameters participate in the tab key, allowing distinct parameterized pages;
- individual tabs, other tabs, and all closable tabs can close;
- dirty forms require confirmation before close or navigation;
- the active tab synchronizes with browser URL/history;
- serializable tab/filter state survives refresh for the current session;
- logout clears all tab state;
- only the active page tree is mounted; inactive state is retained as approved serializable state;
- narrow layouts move overflow tabs to an accessible compact selector;
- keyboard focus, navigation, and close actions meet the accessibility standard.

## 12. Configuration and secrets

Tracked API settings contain typed sections and safe placeholders for database, token/session, OTP, SMS, CORS, protection keys, and health behavior. Real hostnames, usernames, passwords, administrator data, signing/hashing keys, encryption keys, and environment-specific service values never enter Git.

Development values use .NET user secrets or ignored local environment configuration. IIS receives production values through the IT-approved protected mechanism. Runtime database identity will eventually be least-privileged; elevated schema deployment remains separate.

## 13. Error handling and observability

- Central middleware maps unexpected errors to safe Persian-capable problem details with trace identifiers.
- Application outcomes distinguish denial, invalid/expired OTP, rate limit, dependency failure, and internal failure without revealing user enumeration details.
- Structured technical logs and immutable audit records remain separate.
- SMS failure or timeout never creates an authenticated session.
- Readiness includes required database connectivity; SMS dependency status is reported without disclosing endpoint or credentials and without blocking liveness.

## 14. Testing

- Domain tests cover OTP lifecycle, attempt limits, expiry, consumption, session expiry/revocation, and role invariants.
- Application tests cover known/unknown/inactive users, SMS success/failure/timeout, resend rules, verification, refresh rotation, logout, and full-access policy.
- Integration tests cover EF mappings/migrations, API contracts, errors, cookies, CORS/origin protection, authorization, and health endpoints using isolated test data.
- Frontend component tests cover sign-in, OTP, countdown/resend, theme/palette, menu, footer, tab behavior, dirty-close confirmation, and logout clearing.
- End-to-end tests use fake Windows identity and fake SMS to cover administrator sign-in through the home shell and key denial paths.
- Automated tests never contact the real SMS service or use real personal data.

## 15. Acceptance criteria

- `backend/EosDashboards.sln` opens/builds/tests independently in Visual Studio.
- `frontend/` installs/builds/tests independently in VS Code with locked dependencies.
- EF migrations create the approved schema in an isolated target database.
- The provisioning tool idempotently creates the initial full-access administrator without repository-stored personal data.
- A known Windows/AD user must complete SMS OTP; an unknown, inactive, invalid-code, expired-code, exhausted, or revoked case is denied safely.
- Successful verification creates the approved token/session behavior; logout and eight-hour expiry require a new OTP.
- The branded Persian RTL shell, themes, palette, collapsible menu, internal tabs, version, Persian date, and Tehran time satisfy the approved behavior and accessibility rules.
- Health, error, log, and audit behavior is verifiable.
- Static analysis, formatting, backend/frontend builds, and all automated tests pass.

## 16. Explicitly deferred

- Real dashboards, metrics, data ingestion, and charting.
- User, role, and permission administration UI.
- Department/CEO authorization assignments beyond the initial full-access administrator.
- Internet/external authentication and exact LDAP/AD/Entra ID/AD FS topology.
- Trusted-device OTP bypass or password-based local login.
- Production hostnames, certificates, monitoring product, backup schedule, and recovery objectives.
