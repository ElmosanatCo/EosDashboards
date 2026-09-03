# Current Project State

**Last updated:** 2026-09-03

## Phase

Initial authentication and application-shell implementation.

## Completed

- The empty GitHub repository was cloned locally.
- The repository-based project-memory design was approved.
- The implementation plan for the memory structure was prepared.
- The canonical project-memory structure and root agent instructions were created and verified.
- Project-wide development, UI, security, data, testing, deployment, and operational standards were approved.
- The initial intranet authentication experience and the deferred external-access discovery boundary were approved.
- Material UI, RTL-first Persian presentation, theme behavior, the navy/teal default palette, and Vazirmatn were selected.
- The approved standards were consolidated into canonical English and formal Persian documents.
- The repository-level shared resource hierarchy was created.
- The first implementation slice was approved: separate backend/frontend foundations, pre-provisioned system administrator, local username/password sign-in, mandatory SMS OTP, session management, and the initial SPA shell.
- Company branding, footer version/date/time, and closable internal workspace tabs were approved.
- The initial authentication and tabbed-shell design was reviewed and approved.
- A task-by-task, test-driven implementation plan was prepared.
- The user selected inline plan execution with review checkpoints.
- .NET SDK 10.0.400 and Node.js 24.19.0 were installed and verified.
- The isolated `feature/initial-authentication-shell` worktree was created.
- The independently buildable backend solution and React frontend foundation were scaffolded and verified.
- The Domain authentication, session, role, preference, and audit state model was implemented and verified.
- The Application authentication contracts and use cases were implemented and verified with correlation-aware audit attribution, non-cancelable OTP security commits, and access tokens capped by absolute session expiry.
- EF Core SQL Server persistence, explicit repositories, guarded database integration tests, the initial identity migration, and its reviewed idempotent deployment script were implemented and review-hardened with tracked provisioning mutations plus live uniqueness and rowversion-concurrency coverage.
- Infrastructure authentication security primitives were implemented, review-hardened, and verified: secure OTP/opaque-token generation, keyed HMAC-SHA256 hashing, restart-persistent purpose-isolated Data Protection mobile encryption, strict JWT issuer/audience/signature/HS256/lifetime validation with explicit expiry, typed startup-validated security options, and dependency injection. Pending a formal mask-format decision, the implementation conservatively exposes only the final four mobile digits (`*******6789`).
- The replaceable company SOAP SMS adapter was implemented and verified: typed startup-validated HTTPS endpoint/timeout options, one named `HttpClient`, SOAP 1.1 request creation with XML-safe message/mobile serialization, a configured deadline across headers and asynchronously read body, a fully buffered 64 KiB response limit before DTD-prohibited EOF-complete parsing with exactly one result, safe failure mapping, caller-cancellation propagation, and no automatic retry.
- The deployment-only System Administrator provisioner was implemented and verified: normalized idempotent user/role creation and profile updates, exact role-code validation, protected mobile storage with masked-only output, cross-process serialized transactional generated-ID/role/audit persistence, safe interactive confirmation, and composition independent of SMS configuration.
- The user required a cost-conscious development workflow: focused essential tests during implementation, broad verification only at meaningful checkpoints, and no subagents or repeated independent review loops without explicit approval or exceptional documented risk.
- The user explicitly approved tracking local development server settings, including SQL credentials, service endpoints, and API security keys, in this private repository's API/IIS configuration. Public frontend endpoint settings may also be tracked; server credentials and private keys remain server-side because frontend build values are browser-visible. Use the established `D:\Workspaces\ChatGpt\Private Data For AI Projects\EosDashboards` directory as a fallback without repeatedly requesting its values. Values remain excluded from documentation and tool output; production values and personal data remain outside source control.
- The available SQL Server database and IIS sites are development-only resources on the user's machine. Task 13 targets them for local deployment and smoke testing; production deployment to company servers remains a later, separate activity.
- Secure API endpoints expose local username/password challenge creation, purpose-isolated OTP verification, password recovery and signed-in password change, JWT-protected current-user access, refresh rotation, logout, current-user preferences, safe problem details, rate limiting, exact-origin CORS, OpenAPI, and live/ready health checks.
- The React SPA has a locally hosted Vazirmatn font, Persian RTL Material UI theme, polished local-credential sign-in, recovery and password-change views, in-memory access-token client, fixed application shell, home tab, serializable closable-tab workspace, Persian status clock, and server-synchronized appearance/sidebar preferences.
- Fresh pre-integration verification passed on 2026-09-03: the Release backend suite completed 172 tests, including 102 SQL-backed integration tests; frontend lint, typecheck, formatting, build, 10 component/unit tests, and one mocked-network browser flow completed successfully. Separate API/UI publish artifact inspection previously found no source maps or detected embedded secrets.
- Local development deployment artifacts were produced in a versioned temporary directory. Both API and UI contain `web.config`; the reviewed initial idempotent SQL migration script remains available.
- The initial authentication and application-shell implementation was merged into `main` and published to the configured GitHub remote on 2026-09-03 as merge commit `d51fe35`. The external SMS connectivity blocker remains explicit and does not invalidate the user-confirmed live username/password-and-OTP sign-in completed on 2026-09-02.
- The approved EOS SVG was received on 2026-09-02, stored under `resources/branding/eos.svg` without modification, and verified against its SHA-256 record.
- The local IIS deployment uses separate `Default Web Site` applications: UI `/EosDashboards` in `EosDashboardsUiPool` and API `/EosDashboardsApi` in `EosDashboardsApiPool`, both under `C:\inetpub\wwwroot\EosDashboards\` with immutable versioned releases. Both pools use No Managed Code; the current local UI and API readiness endpoints return HTTPS HTTP 200.
- IIS URL Rewrite is not installed locally. The UI SPA fallback uses IIS custom 404 execution, avoiding that unavailable module. API Anonymous Authentication is enabled and Windows Authentication is disabled. The API also disables IIS automatic authentication in-process so the retired Negotiate handler cannot be injected during local credential sign-in.
- The development SQL connection and SMS endpoint are tracked in the API development configuration under decision 0006. The helper still generates independent local security keys, maintains the persistent API-only key ring outside the web root, and uses the current Windows identity for the optional first-administrator provisioning flow. The local development migration completed and one initial System Administrator user, role assignment, and provisioning audit record were created. No personal or production value is stored in this repository.

## In progress

- Linked Google sign-in is being implemented in an isolated worktree. The first four independently verified slices add the `ExternalIdentityLinks` persistence model, unique Google email/subject constraints, optimistic-concurrency row version, EF migration `20260903103049_ExternalIdentityLinks`, and tracked lookup repository; the administrator provisioner now accepts an optional Google email only through its hidden-entry path. A blank entry leaves links unchanged; an explicit update preserves any already-bound Google subject. A verified pre-linked Google identity can now bind its stable subject and receive the normal eight-hour application session; unknown, unverified, and inactive identities are denied without OTP or user creation. The API has a disabled-by-default, startup-validated server-only Google OpenID Connect configuration, PKCE, secure temporary cookies, safe callback redirects, and anonymous provider-discovery/start endpoints. The existing noninteractive deployment bridge supplies a blank optional field for compatibility. No personal identity value is printed or documented; five domain, 15 focused provisioning-application, 13 focused Google/session-application, 17 focused SQL-backed provisioning, and nine focused API configuration/endpoint tests passed against the safe integration-test catalog.
- The local development database applied the additive external-identity migration on 2026-09-03. At the user's request, one approved Google email from the established private configuration was linked to the single active System Administrator through a parameterized transaction. A non-sensitive verification confirmed exactly one active pending Google link and no bound provider subject; no identity value is recorded here.
- Resolve the configured external SMS endpoint/connectivity only if it recurs in a new local live sign-in attempt; the user confirmed that a full username/password-and-OTP sign-in succeeded on 2026-09-02.
- A local provisioning defect was corrected: Persian administrator profile text had been corrupted while being passed from the deployment helper to its child process, despite Unicode SQL Server columns. The helper and provisioner now declare UTF-8 at every process boundary. A safe database-wide text scan found exactly two affected values, both in the initial administrator's first/last-name fields, and none elsewhere. A parameterized corrective update restored the affected profile; a non-sensitive verification confirmed zero remaining corrupted profiles and one exact Unicode profile match. No private value is recorded in repository documentation.
- The private local deployment file is now interpreted by named administrator fields rather than positional values; only username, password, first name, last name, and mobile are consumed. Extra labelled values are ignored. The parser and private-config validation completed without disclosing values.
- A local IIS publication helper now copies already-built API/UI artifacts to new versioned directories, switches the two IIS applications, configures the API from the private file, and verifies readiness. It uses Windows PowerShell only for IIS management and UTF-8 PowerShell for private Persian input.
- The local IIS UI was rebuilt with its `/EosDashboards/` asset base and `/EosDashboardsApi` API base, correcting the prior blank page and same-origin API routing failure. The user confirmed on 2026-09-03 that live username/password sign-in was tested successfully against the provisioned account. The subsequent configured SMS call timed out, so the resulting OTP was marked send-failed; no credential, code, phone number, or endpoint is recorded here.

## Next agreed step

Confirm and correct the local SMS service endpoint/connectivity in the private configuration, then complete one user-initiated browser-based local sign-in flow with SMS OTP, refresh, and logout. Do not deploy to company production servers in this slice.

## Blockers

- The configured local SMS service endpoint timed out during one authorized live sign-in attempt. Login cannot complete until its endpoint, network reachability, or SOAP service contract is corrected. Do not repeat live OTP sends until that is resolved.

## Immediate unresolved questions

- The approved time standard stores technical instants in a universal representation and displays them as Persian-calendar Asia/Tehran time. A request to store local time in the database instead would change OTP/session expiry and audit behavior as well as every `...Utc` field; its exact scope requires an explicit decision before implementation.
- Which business dashboards and metrics are required first?
- Which managers or roles will use each dashboard?
- What source systems will supply dashboard data?
- Is the organization's LDAP service separate from Active Directory, and which supported identity infrastructure is available?
- What are the approved internal hostnames, certificates, browser versions, retention periods, recovery objectives, and monitoring tools?
- Should the reversible privacy-conservative mobile mask that shows only the final four digits be formally approved or changed?
