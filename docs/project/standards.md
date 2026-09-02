# Project Development Standards

**Status:** Confirmed

**Approved:** 2026-09-02

**Last updated:** 2026-09-02

This is the canonical implementation standard for EosDashboards. Feature specifications may add stricter rules but may not silently weaken these rules. Proposed exceptions require explicit approval and an auditable decision record.

## 1. Documentation and traceability

- Maintain analysis, requirements, design, architecture, operations, and decision documentation with the code.
- Keep concise technical sources of truth in plain English and formal printable documents in Persian.
- Update affected documents whenever a durable requirement, decision, rule, state, or next step changes.
- Do not store raw chat transcripts, secrets, credentials, personal data, or production connection details.
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
- Keep secrets outside source control and protect them per environment.
- Audit authentication, permission, administration, and sensitive export events without recording credentials, tokens, or sensitive payloads.
- Address material dependency or security findings before merge, or record an explicitly approved exception.

## 8. Organizational authentication

- Phase 1 is intranet-only.
- First visit and post-logout states show one button: organizational sign-in.
- A valid application session signs the user in automatically without showing the sign-in page.
- Windows/AD may recognize the internal organizational identity; application roles and policies remain authoritative for application access.
- Logout revokes the application session even if the browser still has a Windows identity.
- Every new phase-1 application session requires SMS OTP after organizational identity recognition; no local password is stored.
- OTPs are six digits, valid for five minutes, limited to five verification attempts, and subject to a 60-second resend cooldown and endpoint rate limits.
- Store OTPs only as keyed hashes and mobile numbers in protected encrypted form; mask mobile numbers in UI and logs.
- An eight-hour application session uses ten-minute access tokens and a revocable refresh credential. Logout or expiry requires a new OTP.
- Any LDAP interaction is server-side, uses TLS, validates certificates, and follows current directory hardening requirements.
- Never expose AD or LDAP directly to a browser or the internet.
- External access, the precise AD/LDAP relationship, available Entra ID/AD FS services, and any future stronger-factor design remain deferred pending IT discovery.
- SMS OTP must remain replaceable behind an Application port and Infrastructure adapter; it is not treated as the sole permanent high-assurance option.

## 9. UI, RTL, and design system

- Use Material UI as the component foundation. Paid MUI X features require explicit approval.
- Design Persian RTL first while preserving an internationalization foundation for future English LTR.
- Use one coherent design system for spacing, typography, elevation, shape, color, focus, motion, forms, feedback, and charts.
- Adopt a balanced corporate visual direction: medium density, professional modern appearance, controlled color, and brief functional motion.
- Use navy/teal as the default palette.
- Support light, dark, and system appearance modes independently from selectable color palettes.
- Persist appearance per user in the database and cache it locally to avoid a theme flash.
- Keep the top header and bottom status bar fixed. Only central content scrolls.
- Provide a persistent collapsible hamburger side menu and remember its supported user preference.
- Present opened SPA pages as closable internal workspace tabs. Keep home fixed, focus existing logical tabs, protect dirty pages, synchronize the active route, restore tab descriptors after refresh, and clear them on logout.
- Render only the active tab's page tree while preserving approved serializable state for inactive tabs.
- Design explicit loading, empty, error, offline, permission-denied, and success states.
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

## 11. Localization, time, and accessibility

- Store instants in a normalized universal representation and convert only at boundaries.
- Display user-facing dates using the Persian calendar, Persian digits, and Asia/Tehran time unless explicitly specified otherwise.
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
