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

## In progress

- Completing local operator deployment and real-development smoke testing for the initial authentication and application-shell vertical slice.

## Next agreed step

Define the two local EosDashboards IIS site/application names, bindings, application pools, and physical paths. Then deploy the ready API/UI artifacts there, apply the migration, configure secrets outside the artifact, provision the initial administrator, and perform the separately approved real SMS sign-in smoke test. Do not deploy to company production servers in this slice.

## Blockers

- Existing elevated IIS inspection found only `WebTasApi` and `WebTasUi`, which contain the separate existing WebTas application. No EosDashboards API/UI IIS targets, separate pool names, bindings, or physical paths are defined yet; those sites must not be overwritten.
- A real SMS test requires explicit receipt approval and local secret/administrator values; none were requested or stored during implementation.

## Immediate unresolved questions

- Which business dashboards and metrics are required first?
- Which managers or roles will use each dashboard?
- What source systems will supply dashboard data?
- Is the organization's LDAP service separate from Active Directory, and which supported identity infrastructure is available?
- What are the approved internal hostnames, certificates, browser versions, retention periods, recovery objectives, and monitoring tools?
- Should the reversible privacy-conservative mobile mask that shows only the final four digits be formally approved or changed?
