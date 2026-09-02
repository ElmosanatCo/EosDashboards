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
- The first implementation slice was approved: separate backend/frontend foundations, pre-provisioned system administrator, Windows/AD sign-in, mandatory SMS OTP, session management, and the initial SPA shell.
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
- Secure API endpoints now expose Windows/AD challenge creation, OTP verification, JWT-protected current-user access, refresh rotation, logout, current-user preferences, safe problem details, rate limiting, exact-origin CORS, OpenAPI, and live/ready health checks.
- The React SPA now has a locally hosted Vazirmatn font, Persian RTL Material UI theme, in-memory access-token client, organizational OTP experience, fixed application shell, home tab, serializable closable-tab workspace, Persian status clock, and server-synchronized appearance/sidebar preferences.
- Focused and checkpoint verification passed: backend Release build and 97 SQL-backed integration tests, frontend typecheck/build and 6 component/unit tests, one mocked-network browser flow, and separate API/UI publish artifact inspection with no source maps or detected embedded secrets.
- Local development deployment artifacts were produced in a versioned temporary directory. Both API and UI contain `web.config`; the reviewed initial idempotent SQL migration script remains available.
- The verified implementation is committed and pushed on `feature/initial-authentication-shell`; it has not been merged into `main` while local IIS deployment and the authorized real-development smoke test remain incomplete.
- The approved EOS SVG was received on 2026-09-02, stored under `resources/branding/eos.svg` without modification, and verified against its SHA-256 record.
- The local IIS deployment uses separate `Default Web Site` applications: UI `/EosDashboards` in `EosDashboardsUiPool` and API `/EosDashboardsApi` in `EosDashboardsApiPool`, both under `C:\inetpub\wwwroot\EosDashboards\` with versioned releases. Both pools use No Managed Code and the UI release `20260902-202534` and paired API release `20260902-202354` return HTTPS HTTP 200 health/loading responses.
- IIS URL Rewrite is not installed locally. The UI SPA fallback uses IIS custom 404 execution, avoiding that unavailable module. API Windows Authentication is enabled and Anonymous Authentication is disabled; health checks must use the local Windows identity.
- Required API runtime values are applied outside the artifact from a developer-owned, access-restricted private file. The helper generates independent local security keys, maintains the persistent API-only key ring outside the web root, and uses the current Windows identity for the optional first-administrator provisioning flow. The local development migration completed and one initial System Administrator user, role assignment, and provisioning audit record were created. No secret or personal value is stored in this repository.

## In progress

- Completing the authorized real-development sign-in smoke test for the initial authentication and application-shell vertical slice. A real challenge and OTP verification succeeded with the provisioned local Windows identity without exposing or recording the OTP, mobile number, or challenge token. The direct test harness did not supply the required anti-forgery cookie/header pair on its refresh request, so that harness refresh was correctly rejected and it did not continue to logout. Chrome was configured to attempt integrated authentication for `localhost`, but its local API health request then failed with `ERR_INVALID_AUTH_CREDENTIALS`. During the developer's home-VPN session, the workstation was domain-joined but its Windows secure-channel check was unavailable; no further browser authentication attempt was made.

## Next agreed step

Reconnect or validate the corporate VPN until the workstation secure-channel check is available, then confirm the approved local/intranet Windows-authentication topology before completing one browser-based local sign-in flow and verifying its refresh and logout using the UI's anti-forgery handling. Do not deploy to company production servers in this slice.

## Blockers

- Chrome now attempts integrated authentication for the allowed local host, but IIS rejects the credentials (`ERR_INVALID_AUTH_CREDENTIALS`). The local validation host lacks a confirmed Windows-authentication topology. In the current home-VPN session, the workstation is domain-joined but its Windows secure channel is unavailable, so browser integrated authentication cannot yet be meaningfully validated. Resolve connectivity first, then use an approved intranet hostname with its DNS/SPN configuration, or explicitly approve a narrower IIS authentication boundary that requests Windows authentication only for organizational sign-in.

## Immediate unresolved questions

- Which business dashboards and metrics are required first?
- Which managers or roles will use each dashboard?
- What source systems will supply dashboard data?
- Is the organization's LDAP service separate from Active Directory, and which supported identity infrastructure is available?
- What are the approved internal hostnames, certificates, browser versions, retention periods, recovery objectives, and monitoring tools?
- Should the reversible privacy-conservative mobile mask that shows only the final four digits be formally approved or changed?
