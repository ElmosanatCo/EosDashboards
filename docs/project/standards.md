# Project Development Standards

**Status:** Confirmed

**Approved:** 2026-09-02

**Last updated:** 2026-09-03

This is the canonical implementation standard for EosDashboards. Feature specifications may add stricter rules but may not silently weaken these rules. Proposed exceptions require explicit approval and an auditable decision record.

## 1. Documentation and traceability

- Maintain analysis, requirements, design, architecture, operations, and decision documentation with the code.
- Keep concise technical sources of truth in plain English and formal printable documents in Persian.
- Update affected documents whenever a durable requirement, decision, rule, state, or next step changes.
- Record every durable user agreement, accepted decision, material blocker, and confirmed resolution in the appropriate canonical source before ending the task. Resolutions must capture the symptom, root cause, durable remedy, and safe verification evidence needed to avoid repeated investigation; retain a decision record when its rationale must remain auditable.
- Do not store raw chat transcripts, personal data, or production connection details. Decision 0006 permits local development SQL credentials, SMS endpoint settings, and API security keys in the tracked API development configuration of this private repository; documentation and tool output must still omit their values.
- Use decision records for consequential choices and retain rationale, status, and supersession history.
- Before every local merge or push, update and verify `AGENTS.md` and the affected canonical documents.
- Every successful local merge must be verified and immediately pushed to the destination branch. A failed push means integration is incomplete.

## 2. Source control and release history

- Keep `main` healthy and releasable.
- Develop features and fixes on focused, short-lived branches.
- Merge only after required builds, analysis, tests, documentation, and review pass.
- Keep commits cohesive and messages concise and purpose-oriented.
- Do not mix unrelated changes.
- Do not bypass review on `main` except for a documented emergency, which requires retrospective review.
- Deploy production only from an identifiable version tag and maintain release notes for material changes.

## 3. Architecture and boundaries

- Use lightweight clean layering without unnecessary abstraction or module proliferation.
- Dependency direction is API -> Application -> Domain.
- Infrastructure implements Application/Domain ports and is the only layer allowed to access EF Core, SQL Server, AD/LDAP, files, or external services.
- API controllers are thin: transport validation, use-case invocation, and response mapping only.
- Application coordinates use cases, authorization-relevant business behavior, and transaction boundaries.
- Domain contains business concepts and rules without infrastructure dependencies.
- Use explicit, domain-focused data-access contracts rather than a giant generic repository.
- API contracts/DTOs are distinct from persistence entities.

## 4. API design

- Version routes under `/api/v1/...`.
- Maintain OpenAPI metadata with the implementation.
- Use consistent JSON naming and standard safe error objects containing a trace identifier.
- Apply central exception handling. Use local `try/catch` only for recovery, translation, added context, or a deliberate retry boundary; never swallow or repeatedly log the same exception.
- Make I/O asynchronous and propagate cancellation.
- Apply server-side pagination to growing collections and allowlist filter and sort fields.
- Validate all input at the server trust boundary.
- Allow only exact approved UI origins through CORS.

## 5. Resource and lifetime management

- Dependency injection owns and disposes injected services and EF contexts.
- Use `using`/`await using` only for resources created and owned by the current scope.
- Do not manually dispose injected dependencies.
- Avoid holding database contexts, streams, or large materialized result sets longer than required.

## 6. Database and Entity Framework

- EF Core Code First migrations are the schema source of truth.
- Permit direct migration application in development only.
- Never run production migrations automatically at application startup.
- Generate, review, test, archive, and run production migration scripts or bundles with a deployment identity. Runtime identities have no schema-change permission.
- Use full, readable English names and avoid ambiguous abbreviations.
- Prefer a simple normalized schema, necessary relationships, and enforced uniqueness.
- Do not make undocumented manual database changes. Emergency changes must be recorded and represented by a formal migration.
- Every principal application table has an auto-incrementing SQL Server `bigint` primary key named `Id`; related foreign keys are `bigint`.
- A pure many-to-many junction may use a composite key. A junction with lifecycle, status, audit, or other domain data receives its own `Id`.
- Keyless read-only views/projections and framework-owned internal tables are exempt from the principal-table key rule.
- Disable lazy loading. Prefer no-tracking projections for reads.
- Base indexes on foreign keys, uniqueness, and measured query patterns.
- Define one transaction boundary per business operation and avoid distributed transactions unless explicitly justified.

## 7. Security and authorization

- Require HTTPS and production HSTS.
- Use short-lived JWT access tokens and validate signature, issuer, audience, lifetime, and replay-relevant identifiers.
- Hold access tokens in browser memory, not persistent browser storage.
- Use a Secure, HttpOnly, revocable refresh cookie with appropriate same-site and anti-forgery protections.
- Enforce authorization on the server using roles and policies, least privilege, and deny-by-default behavior.
- Never rely on hidden UI elements as authorization.
- Rate-limit sign-in and other sensitive endpoints.
- Keep production secrets outside source control and protect them per environment. By explicit user approval, the private repository may track local development SQL credentials, service endpoint settings, API security keys, and other server-side local settings in API/IIS configuration. Track public frontend endpoint settings where needed. Do not stop work or repeatedly request established local values; use the tracked configuration or the approved private-data directory as the fallback. Never expose values through logs, errors, documentation, or test output. Do not place server credentials or private keys in frontend build configuration because browser-delivered values are public at runtime.
- Audit authentication, permission, administration, and sensitive export events without recording credentials, tokens, or sensitive payloads.
- Address material dependency or security findings before merge, or record an explicitly approved exception.

## 8. Authentication

- Phase 1 uses a pre-provisioned local username and password followed by mandatory SMS OTP.
- First visit and post-logout states show the local sign-in form.
- A valid application session signs the user in automatically without showing the sign-in page.
- Application roles and policies remain authoritative for application access.
- Logout revokes the application session.
- Every new phase-1 application session requires SMS OTP after successful password verification.
- Passwords are stored only as standard salted hashes. They are 8 to 128 characters long and have no character-class composition rule. Plaintext passwords never enter logs, audit records, error responses, source control, or tracked settings.
- Signed-in password change requires the current password. Password recovery requires a purpose-isolated SMS OTP and never creates an authenticated session. Password change or reset revokes all active sessions for that user.
- System-administrator user/access management is approved product scope but is not implemented in the current authentication slice. Until its separately approved implementation is released, the controlled deployment tool remains the sole account/password-management mechanism.
- OTPs are six digits, valid for five minutes, limited to five verification attempts, and subject to a 60-second resend cooldown and endpoint rate limits.
- Store OTPs only as keyed hashes and mobile numbers in protected encrypted form; mask mobile numbers in UI and logs.
- An eight-hour application session uses ten-minute access tokens and a revocable refresh credential. Logout or expiry requires a new OTP.
- Any future LDAP interaction is server-side, uses TLS, validates certificates, and follows current directory hardening requirements.
- Never expose a directory service directly to a browser or the internet.
- Directory, federation, and any future stronger-factor design remain deferred pending IT discovery.
- SMS OTP must remain replaceable behind an Application port and Infrastructure adapter; it is not treated as the sole permanent high-assurance option.

## 9. UI, RTL, and design system

- Use Material UI as the component foundation. Paid MUI X features require explicit approval.
- Design manager-facing pages as one consistent Persian RTL workforce-operations workspace. The visual direction is calm, technical, authoritative, and suited to serious industrial management work; it is not a generic AI or consumer SaaS dashboard.
- Use Vazirmatn as the primary font. Use full RTL layout and Persian content, Persian numerals where appropriate, and deliberate LTR formatting for identifiers, technical terms, dates, and numerical data. Preserve an internationalization foundation for future English LTR.
- Use a compact, desktop-first operational workspace with responsive mobile behavior. Do not use gradients, glassmorphism, floating panels, oversized rounded cards, excessive whitespace, generic hero sections, or consumer-SaaS visual patterns.
- Use one coherent design system for spacing, typography, elevation, shape, color, focus, motion, forms, feedback, data visualization, and tables.
- Treat visual quality, order, and harmony as core acceptance criteria. Before implementing a UI change, define its hierarchy, spacing, typography, color, responsive behavior, states, and RTL implications; review the rendered result and refine it until it is professionally cohesive. Do not assemble interfaces from disconnected defaults, rushed layouts, or placeholder visual decisions.
- Support light, dark, and system appearance modes; default manager-facing pages to dark mode. The default dark palette uses page `#0D1113`, primary surface `#13191C`, secondary surface `#182024`, separator `#2A3538`, primary text `#EDF2F0`, muted text `#96A4A6`, and teal accent `#38B8AA`. The light palette uses page `#F2F5F3`, primary surface `#FBFCFA`, secondary surface `#F3F6F4`, raised surface `#E8EFEB`, separator `#D8E0DC`, stronger border `#C3CFCA`, primary text `#17201F`, and muted text `#5C6B69`; it remains precise, technical, and airy rather than decorative or consumer-oriented.
- Offer teal, indigo, emerald, amber, and rose accent choices. Accent colors affect interaction highlights only; semantic status colors remain fixed: green for approved/healthy, amber for pending or attention, red for rejected/critical, and blue or teal for informational or active states. Maintain accessible contrast in every appearance and palette.
- Persist appearance per user in the database and cache the last applied appearance and palette locally so the sign-in page renders with the last selected theme without a flash.
- Keep the top header, internal tab bar immediately beneath it, and bottom status bar fixed. Prevent whole-page scrolling; only the workspace between the tab bar and status bar scrolls. A side-menu drawer and its temporary mobile overlay must begin exactly at the header's lower edge and end exactly at the status bar's upper edge; derive those bounds from shared shell metrics, never unrelated fixed heights.
- Provide a persistent collapsible hamburger side menu and remember its supported user preference.
- Present opened SPA pages as closable internal workspace tabs. Keep the first tab, `خانه`, fixed and non-closable. Pages opened from the menu, a table row, an AI insight, a report, or a dashboard action open or activate a workspace tab; reopen an exact page by activating its existing tab. Use concise Persian tab titles, icons, active states, close controls, and an unsaved-change marker when required. Use a compact, quiet inactive state and an amber/gold underline for the active tab. On narrow screens, make the strip horizontally scrollable and provide an accessible overflow menu for open tabs.
- Render only the active tab's page tree while preserving approved serializable state for inactive tabs.
- Include brand, current role and organizational scope, a compact global command search, theme control, notifications, and user profile in the header. The search returns only pages, permitted operations, and later eligible in-dashboard elements that the current user may access; choosing a result opens or activates its internal workspace tab. Do not expose unavailable targets through search. The status bar shows data synchronization state, last-update time, active organization or department, system health, and the existing required application version and Persian-calendar local-time information.
- Use flat panels with thin 1px borders, small corner radii, compact spacing, and a thin colored top accent line. Every primary panel has the accent line; active, selected, and hovered panels change it to gold/amber without shadows, glow, or excessive animation. Establish hierarchy through typography, row density, dividers, and tonal contrast. Treat tables, lists, review queues, and structured rows as primary interface elements.
- Use subtle 150–200 ms transitions only for state changes, panel emphasis, tab activation, navigation, filters, notifications, and theme changes. Design explicit empty, loading, error, success, denied-access, offline, and no-data states. Never rely on color alone: pair status with clear Persian text and, where useful, a compact icon. Provide compact filtering, search, sorting, inline actions, tooltips for unfamiliar icons, confirmation for consequential actions, and undo where relevant.
- Keep AI visible but restrained; do not make a large chatbot the central visual element. Every AI insight includes a concise title, evidence, reason, confidence level, organizational impact, and actionable next step, with links to its supporting department, report, person, job description, or source data.
- Use these role-content defaults when the corresponding approved page is designed: System Administrator—company departments, manager accounts, roles, permissions, and system activity; Department Manager—department personnel, job descriptions, Excel upload, submission status, incomplete data, and requested corrections; HR Manager—review and approval inbox, approve/reject/request-edits actions, HR statistics, data quality, skill coverage, and organizational gaps; CEO—read-only strategic dashboard, major changes, critical workforce risks, organization-level trends, reports, and AI-supported decision insights. These defaults do not approve the underlying data, permissions, workflows, metrics, or sources; those remain subject to dashboard discovery and authorization design.
- Use `resources/images/references/manager-workforce-dashboard-reference.png` as an internal visual reference for compact header hierarchy, central command search, action/attention strips, dense summary panels, structured operational lists, and restrained insight/change panels. Reuse its composition principles only when they fit an approved feature; do not copy its data, labels, workflows, metrics, or branding.
- Defer the charting library until the first dashboard's visualization requirements are known.

## 10. Typography and shared resources

- Vazirmatn is the primary application, chart, form, and printable-output typeface.
- Self-host optimized web font files; do not require internet access for typography.
- Store authoritative shared resources under:

```text
resources/
|-- branding/
|-- fonts/
|-- icons/
|-- images/
`-- templates/
```

- Record source, ownership, license, version, and usage guidance in `resources/README.md`.
- Keep required light, dark, color, and monochrome logo variants when supplied.
- Optimize fonts and images before use and avoid uncontrolled duplicate copies.
- Never commit confidential or redistribution-restricted assets.

## 10.1 Unicode data integrity

- Treat Persian and every other non-ASCII value as Unicode end-to-end.
- At every cross-process, file, database, and external-service text boundary, explicitly select UTF-8 or the destination's documented Unicode encoding. Do not rely on Windows console code pages or process defaults.
- Before a deployment or provisioning tool writes user-supplied text, verify text-boundary integrity with a synthetic Unicode probe or non-sensitive validation result. Do not reveal the supplied value in logs, diagnostics, source control, or test output.
- Use Unicode SQL Server data types (`nvarchar`/`nchar`) for application text that can contain Persian or other non-ASCII characters.

## 11. Localization, time, and accessibility

- Persist application timestamps as the current local date and time of the application server. Use SQL Server `datetime2(3)` so stored values contain exactly year, month, day, hour, minute, second, and millisecond, with no offset or finer precision.
- Name persisted-time fields without `Utc` or underscore-based time suffixes: use names such as `CreatedAt`, `UpdatedAt`, `ExpiresAt`, `OccurredAt`, and `RevokedAt`.
- Application time logic uses the same millisecond-precision local server time directly. Do not persist UTC values and do not convert persisted values to Asia/Tehran time. Protocol-specific transient conversion is permitted only where an external standard, such as JWT numeric-date claims, requires it; it must not alter the persistence model.
- Display user-facing dates using the Persian calendar and Persian digits based on the applicable local system time, unless a feature explicitly requires another representation. Every date-selection control uses the Persian calendar; native Gregorian date/datetime controls are prohibited. When both are shown, render the date and time as separate, clearly labelled visual values rather than one dense combined string; use one non-wrapping row where room permits.
- Target WCAG 2.2 level AA.
- Provide keyboard operation, visible focus, sufficient contrast, semantic labels, usable target sizes, and reduced-motion behavior.
- Never communicate status or validation through color alone.
- Include accessibility checks in component review and critical-flow testing.

## 12. Responsive and browser behavior

- Optimize for desktop and management displays while keeping laptop and tablet use intact.
- Support essential phone flows; complex dashboards may use an approved simplified phone layout.
- Define the minimum supported viewport for each complex feature.
- Initially test current stable Microsoft Edge, Google Chrome, and Mozilla Firefox; IT will confirm the organizational support matrix.
- Do not require browser extensions or vendor-specific behavior.
- Test zoom, font scaling, high-density displays, and selected print layouts.

## 13. Performance and caching

- Target approximately two seconds for ordinary initial intranet pages under defined normal conditions; establish dashboard-specific service levels during discovery.
- Render the application shell promptly and progressively load data regions.
- Cache repeated expensive results only with explicit duration, invalidation, ownership, and permission-safe cache keys.
- Never share authorization-sensitive cached data across inappropriate users or roles.
- Filter, sort, aggregate, and page large datasets on the server; do not download entire datasets for browser processing.
- Cancel or coalesce obsolete duplicate requests where practical.
- Move long-running reports and calculations to background processing when needed.
- Version and cache static assets and optimize their transfer size.
- Measure response time, error rate, memory, database work, and slow queries before optimizing.
- Specify expected data volume and refresh behavior for every dashboard.

## 14. Testing and quality gates

- Keep development and verification cost-conscious. Choose the smallest test scope that provides credible evidence for the changed behavior and its material risks.
- During implementation, run focused tests for the affected component or flow. Avoid repeatedly running full backend, frontend, integration, browser, or multi-environment suites.
- Keep agent token use cost-conscious: load only task-relevant context, avoid redundant scans and repeated diagnostics, and do not rerun a successful command unless code, configuration, environment, or the claim being verified changed.
- Run broader suites only at meaningful checkpoints: completion of an integrated task, pre-merge/publication, or when a cross-cutting change creates a concrete regression risk.
- Test essential behavior, boundaries, security controls, and regressions. Do not add redundant tests merely to increase test count or coverage.
- Use one primary implementation/review path by default. Additional agents or repeated independent review loops require explicit user approval or a documented exceptional risk.
- Unit-test Domain and Application behavior.
- Integration-test API, Infrastructure, authentication/authorization boundaries, and database behavior.
- Component-test forms and reusable UI behavior.
- End-to-end test critical user flows.
- Every bug fix begins with or includes a regression test that demonstrates the failure and the correction.
- Before merge and push, run the build, static analysis, formatting checks, and all relevant tests.
- Treat important compiler and analyzer warnings as errors.
- Measure coverage as a diagnostic; do not optimize for a percentage at the expense of meaningful scenarios.
- Give complete scenario attention to authentication, authorization, security controls, calculations, and critical workflows.
- Do not merge failing or knowingly untested behavior.

## 15. Coding and dependency quality

- Use clear English names for code, routes, schemas, and configuration.
- Keep each component focused and avoid oversized classes and methods.
- Do not place business logic in controllers, UI components, or persistence code.
- Replace scattered magic values with named constants or configuration.
- Comments explain the reason for non-obvious decisions, not what readable code already states.
- Enforce automatic, consistent formatting.
- Add a dependency only for a demonstrated need after reviewing maintenance, security, license, and delivery-size impact.
- Pin dependency resolution through the appropriate lock files.
- Do not adopt abandoned, unsupported, or materially vulnerable packages.
- Upgrade dependencies on a focused branch with relevant tests.
- Remove dead code, temporary experiments, and unused packages before merge.

## 16. Logging, audit, and observability

- Separate technical logs from business/security audit records.
- Propagate a correlation/trace identifier across every request and downstream operation.
- Record errors with time, environment, route, and trace identifier without exposing sensitive data.
- Audit sign-in success/failure, logout, permission changes, settings changes, administrative actions, and sensitive exports.
- Audit records identify actor, action, time, result, and affected subject and are not editable through ordinary application workflows.
- Restrict log and audit access by role and define retention with IT and applicable policy owners.
- Keep server clocks synchronized and timestamp records consistently.
- Monitor service and database health, error rates, latency, and resource saturation.
- Alert on material failures such as database outage, abnormal error growth, or suspicious authentication failure patterns.

## 17. IIS hosting and deployment

- Host UI and API separately with separate IIS application pools and least-privilege identities.
- Separate development, test, and production configuration and databases.
- Build outside production servers and deploy immutable, versioned, traceable artifacts.
- Use controlled application-offline handling when replacement requires it.
- Back up affected data before destructive migration steps.
- Run reviewed database migration artifacts with a deployment identity before or during the controlled release procedure.
- Execute post-deployment smoke tests for startup, health, database connectivity, sign-in, and critical routes.
- Expose appropriately protected liveness and readiness endpoints.
- Retain the previous deployable artifact and a documented application rollback procedure.
- Record deployment version, operator/process, result, migration, smoke-test outcome, and rollback if used.
- Treat correct preservation of non-ASCII provisioned profile text as a deployment smoke-test requirement whenever provisioning input is changed.

## 18. Backup and recovery

- IT must approve backup ownership, schedule, location, encryption, access, and retention.
- Maintain periodic full database backups and the required differential/log strategy based on approved data-loss tolerance.
- Do not keep the only backup on the production server or disk.
- Protect and back up the configuration, certificates, and resources required for restoration through approved secure mechanisms.
- Test restoration periodically; backup-job success alone is not proof of recoverability.
- Define and approve recovery-time and recovery-point objectives before production launch.
- Maintain and exercise a written service, database, and configuration recovery runbook.
- Record recovery-test results and resolve discovered gaps.

## 19. Data governance and confidentiality

- Assign an organizational owner and data approver to each dashboard.
- Define every metric's source, meaning, unit, calculation, refresh time, and last-updated presentation.
- Apply role-based, least-privilege data access in both service authorization and queries.
- Use synthetic or appropriately anonymized sensitive data in non-production environments.
- Authorize and audit sensitive export, printing, and reporting.
- Mark sensitive outputs with generation time, user identity, and approved confidentiality classification where required.
- Follow approved legal and organizational retention and deletion policy.
- Collect and display only necessary personal data.
- Version and approve changes to metric meaning or calculation.
- Make completeness, freshness, and consistency assessable for critical dashboard data.

## Deferred decisions

- Charting library.
- First dashboard, metrics, roles, data sources, refresh schedules, and analytical architecture.
- External identity topology and second-factor implementation.
- Approved organizational browsers, hostnames, certificates, monitoring product, retention periods, backup schedule, and recovery objectives.
