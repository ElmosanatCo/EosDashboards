# Current Project State

**Last updated:** 2026-09-02

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
- The available SQL Server database and IIS sites are development-only resources on the user's machine. Task 13 targets them for local deployment and smoke testing; production deployment to company servers remains a later, separate activity.
- Secure API endpoints currently expose Windows/AD challenge creation, OTP verification, JWT-protected current-user access, refresh rotation, logout, current-user preferences, safe problem details, rate limiting, exact-origin CORS, OpenAPI, and live/ready health checks. They are scheduled to be replaced by the approved local credential and purpose-isolated OTP design.
- The React SPA currently has a locally hosted Vazirmatn font, Persian RTL Material UI theme, in-memory access-token client, organizational OTP experience, fixed application shell, home tab, serializable closable-tab workspace, Persian status clock, and server-synchronized appearance/sidebar preferences. Its sign-in screen is scheduled to be replaced by the approved polished local-credential, recovery, and password-change experience.
- Focused and checkpoint verification passed: backend Release build and 97 SQL-backed integration tests, frontend typecheck/build and 6 component/unit tests, one mocked-network browser flow, and separate API/UI publish artifact inspection with no source maps or detected embedded secrets.
- Local development deployment artifacts were produced in a versioned temporary directory. Both API and UI contain `web.config`; the reviewed initial idempotent SQL migration script remains available.
- The verified implementation is committed and pushed on `feature/initial-authentication-shell`; it has not been merged into `main` while local IIS deployment and the authorized real-development smoke test remain incomplete.
- The approved EOS SVG was received on 2026-09-02, stored under `resources/branding/eos.svg` without modification, and verified against its SHA-256 record.
- The local IIS deployment uses separate `Default Web Site` applications: UI `/EosDashboards` in `EosDashboardsUiPool` and API `/EosDashboardsApi` in `EosDashboardsApiPool`, both under `C:\inetpub\wwwroot\EosDashboards\` with versioned releases. Both pools use No Managed Code and the UI release `20260902-202534` and paired API release `20260902-202354` return HTTPS HTTP 200 health/loading responses.
- IIS URL Rewrite is not installed locally. The UI SPA fallback uses IIS custom 404 execution, avoiding that unavailable module. API Windows Authentication is currently enabled and Anonymous Authentication is disabled; the approved local-credential transition will reverse those IIS settings when it is implemented.
- Required API runtime values are applied outside the artifact from a developer-owned, access-restricted private file. The helper generates independent local security keys, maintains the persistent API-only key ring outside the web root, and uses the current Windows identity for the optional first-administrator provisioning flow. The local development migration completed and one initial System Administrator user, role assignment, and provisioning audit record were created. No secret or personal value is stored in this repository.

## In progress

- The approved local-credential authentication replacement has a reviewed implementation plan. It retains mandatory SMS OTP, adds password recovery and signed-in password change, keeps account/password administration in the private deployment tool, and replaces the current sparse sign-in page with the approved Persian RTL visual direction. The previous Windows-authentication browser smoke test is intentionally superseded; no additional Windows/AD, Chrome, or OTP test is needed before the new flow is implemented.
- A local provisioning defect was corrected: Persian administrator profile text had been corrupted while being passed from the deployment helper to its child process, despite Unicode SQL Server columns. The helper and provisioner now declare UTF-8 at every process boundary. A safe database-wide text scan found exactly two affected values, both in the initial administrator's first/last-name fields, and none elsewhere. The next private-data provisioning run will repair those two values; no private value is recorded in repository documentation.

## Next agreed step

Complete the focused local-credential verification, publish the updated API/UI to the local development IIS applications, repair the initial administrator profile through the UTF-8 provisioning path, and complete one browser-based local sign-in flow with refresh and logout. Do not deploy to company production servers in this slice.

## Blockers

- None for the approved local-credential implementation. The workstation's home-VPN secure-channel result does not block this authentication topology. It remains an organizational connectivity matter outside this slice and has not been changed.

## Immediate unresolved questions

- The private deployment file currently contains six administrator values, while the approved local-credential installer contract requires exactly five in this order after `Method`: username, password, first name, last name, mobile. The user must reconcile this non-sensitive structural mismatch before local deployment; values must not be copied into the repository.
- Which business dashboards and metrics are required first?
- Which managers or roles will use each dashboard?
- What source systems will supply dashboard data?
- Is the organization's LDAP service separate from Active Directory, and which supported identity infrastructure is available?
- What are the approved internal hostnames, certificates, browser versions, retention periods, recovery objectives, and monitoring tools?
- Should the reversible privacy-conservative mobile mask that shows only the final four digits be formally approved or changed?
