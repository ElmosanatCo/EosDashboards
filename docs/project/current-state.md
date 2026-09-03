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
- The user explicitly approved tracking the local development SQL credentials, SMS endpoint settings, and API security keys in this private repository's API development configuration. Their values remain excluded from documentation and tool output. Production values and personal data remain outside source control.
- The available SQL Server database and IIS sites are development-only resources on the user's machine. Task 13 targets them for local deployment and smoke testing; production deployment to company servers remains a later, separate activity.
- Secure API endpoints expose local username/password challenge creation, purpose-isolated OTP verification, password recovery and signed-in password change, JWT-protected current-user access, refresh rotation, logout, current-user preferences, safe problem details, rate limiting, exact-origin CORS, OpenAPI, and live/ready health checks.
- The React SPA has a locally hosted Vazirmatn font, Persian RTL Material UI theme, polished local-credential sign-in, recovery and password-change views, in-memory access-token client, fixed application shell, home tab, serializable closable-tab workspace, Persian status clock, and server-synchronized appearance/sidebar preferences.
- Fresh pre-integration verification passed on 2026-09-03: the Release backend suite completed 172 tests, including 102 SQL-backed integration tests; frontend lint, typecheck, formatting, build, 10 component/unit tests, and one mocked-network browser flow completed successfully. Separate API/UI publish artifact inspection previously found no source maps or detected embedded secrets.
- Local development deployment artifacts were produced in a versioned temporary directory. Both API and UI contain `web.config`; the reviewed initial idempotent SQL migration script remains available.
- The initial authentication and application-shell implementation is committed on `feature/initial-authentication-shell`. The user authorized integration and publication on 2026-09-03 with the external SMS connectivity blocker retained explicitly; final merge verification and publication are the current release activity.
- The approved EOS SVG was received on 2026-09-02, stored under `resources/branding/eos.svg` without modification, and verified against its SHA-256 record.
- The local IIS deployment uses separate `Default Web Site` applications: UI `/EosDashboards` in `EosDashboardsUiPool` and API `/EosDashboardsApi` in `EosDashboardsApiPool`, both under `C:\inetpub\wwwroot\EosDashboards\` with immutable versioned releases. Both pools use No Managed Code; the current local UI and API readiness endpoints return HTTPS HTTP 200.
- IIS URL Rewrite is not installed locally. The UI SPA fallback uses IIS custom 404 execution, avoiding that unavailable module. API Anonymous Authentication is enabled and Windows Authentication is disabled. The API also disables IIS automatic authentication in-process so the retired Negotiate handler cannot be injected during local credential sign-in.
- Required API runtime values are applied outside the artifact from a developer-owned, access-restricted private file. The helper generates independent local security keys, maintains the persistent API-only key ring outside the web root, and uses the current Windows identity for the optional first-administrator provisioning flow. The local development migration completed and one initial System Administrator user, role assignment, and provisioning audit record were created. No secret or personal value is stored in this repository.

## In progress

- Integrating the verified feature branch into `main` and publishing the merged result to the configured GitHub remote.
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
