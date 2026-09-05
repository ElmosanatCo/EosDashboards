# EosDashboards Agent Instructions

## Required startup

1. Read `docs/project/current-state.md`, `docs/project/README.md`, and `docs/project/standards.md` before any task action. These are the minimum startup context and establish current state, source routing, cost discipline, testing discipline, and the required error-learning workflow.
2. Read only the canonical documents relevant to the task after the minimum startup context; do not load broad unrelated documentation.
3. Treat repository documentation as authoritative over chat recollection.
4. Report contradictions or missing decisions; do not silently invent answers.
5. Before responding in a new chat, proactively reconstruct relevant prior context without requiring a user reminder: inspect `main` history and the commits, decisions, plans, and canonical documents related to the requested area. Do not rely on an old feature-branch name when its work has been merged into `main`.

## New-chat continuity

At the start of every new chat, automatically recall the project's prior agreements and implementation state from the canonical memory and relevant `main` history. Start with `current-state.md`, `README.md`, and `decisions/`; then read the task-relevant requirements, architecture, standards, roadmap, specifications, plans, and recent `main` commits. Ask the user only when these sources conflict or leave a material decision unresolved.

## Confirmed project baseline

- EosDashboards is a web application that will provide multiple dashboards to company managers.
- Backend: .NET Core, Entity Framework Core, and REST APIs.
- Database: SQL Server.
- Frontend: React.js.
- UI foundation: Material UI, Persian RTL, and the locally hosted Vazirmatn font.
- UI quality is a primary acceptance criterion: every interface change must be intentional, visually coherent, professionally finished, and reviewed as rendered UI; never assemble screens as disconnected default components or rushed placeholder layouts.
- Every workspace page must expose the shared help icon in the upper-left corner; its Persian page-specific modal must use the fixed sections وظایف این صفحه، امکانات، شیوه انجام کار، و محدودیت‌ها and remain truthful to the approved implementation stage.
- Manager-facing pages use the approved dark-default, Persian RTL workforce-operations visual system with teal interaction emphasis, flat accent-line panels, fixed internal tabs, and restrained evidence-linked AI insights. Retain the existing persistent collapsible hamburger side menu.
- The fixed header includes a compact global command search. It lists and opens only the current user's authorized pages, operations, and future eligible dashboard elements; use `resources/images/references/manager-workforce-dashboard-reference.png` as an internal visual-composition reference only.
- Frontend behavior: React SPA with closable internal workspace tabs; the home tab is fixed.
- Hosting: the React UI and ASP.NET Core API are separate IIS sites/applications with separate application pools.
- Architecture: lightweight clean layering; API -> Application -> Domain, with Infrastructure as the only database and external-system access layer.
- Repository layout: independently openable `backend/` Visual Studio solution and `frontend/` VS Code workspace in one repository.
- Authentication phase 1: local username/password sign-in followed by mandatory SMS OTP for every new eight-hour application session. System Administrators manage users, fixed-role assignments, and departments through the application; the deployment tool remains the bootstrap and Google-link mechanism.
- An active, pre-linked user may alternatively enter through the approved
  server-owned Google OpenID Connect code flow. This never self-registers an
  account and remains disabled until its server-only client configuration is
  complete.
- Initial access: one pre-provisioned database user has System Administrator and Department Manager roles in the `نرم افزار` department. The System Administrator will manage user accounts, fixed-role access assignments, and company departments; a department hierarchy permits only parent departments and their direct children. The approved System Administrator dashboard presents audited administration/security activity and truthful active-session/sign-in metrics; custom roles, granular permissions, Google-link management, audit retention, exports, and alerts remain unapproved.
- Manager-facing primary panels use the shared flat accent-card treatment: a thin top line changes to the approved gold on hover. User and department create/edit forms remain responsive in-page dialogs, and their form card fills the dialog content width at desktop and mobile sizes.
- Persisted application times use the local clock of the application server, are named without `Utc`, and have millisecond precision only. No UTC persistence or Tehran-time conversion is used for application time logic.
- Display and select all user-facing dates with the Persian calendar and Persian digits. Keep date/time visually separate, using one non-wrapping row where space permits.
- Render every user-visible number with Persian digits, including numbers embedded in usernames, revealed passwords, personnel/organizational identifiers, versions, masked contact values, dates, times, counts, and IP addresses. Keep submitted form/API values and internal technical identifiers in the representation required by their contracts.
- All principal application tables use auto-incrementing SQL Server `bigint` primary keys named `Id`, subject to the documented narrow exceptions.
- Branding: company name is `علم و صنعت`; use the approved `resources/branding/eos.svg` unchanged. Do not recolor or substitute it.
- Documentation: concise, plain English.
- User conversation: Persian unless the user requests otherwise.

The full approved rules are in `docs/project/standards.md`. The approved initial vertical-slice design is in `docs/superpowers/specs/2026-09-02-initial-authentication-shell-design.md`. Do not choose unconfirmed dashboard behavior, external authentication topology, data sources, or charting libraries without user approval.

## Sources of truth

- Memory index: `docs/project/README.md`
- Current work: `docs/project/current-state.md`
- Product definition: `docs/project/vision.md`
- Requirements: `docs/project/requirements.md`
- Architecture: `docs/project/architecture.md`
- Engineering standards: `docs/project/standards.md`
- Decisions: `docs/project/decisions/`
- Delivery sequence: `docs/project/roadmap.md`

## Durable-memory rule

Before finishing a task, update the appropriate canonical document when a durable requirement, decision, project-wide rule, blocker, implementation state, or next step changed.

- Integrate facts into the current source of truth; do not append raw chat logs.
- Record every durable user agreement, accepted decision, project-wide rule, recurring operational fact, material blocker, and confirmed resolution in the appropriate canonical source before ending the task. For a resolved issue, record the symptom, root cause, durable remedy, and any safe verification evidence needed to prevent repeated investigation; link a decision record when the rationale must remain auditable.
- Mark unapproved ideas as proposed or unresolved.
- Add a decision record only for consequential choices whose rationale must remain auditable.
- Never store personal data or production connection details. Under decision 0006, the user explicitly permits local development SQL credentials, service endpoint settings, API security keys, and other server-side local settings in tracked API/IIS configuration in this private repository. Track public frontend endpoint settings where needed. Do not stop work or repeatedly request established local configuration values; use the tracked configuration or `D:\Workspaces\ChatGpt\Private Data For AI Projects\EosDashboards` as the fallback. Do not print values in output or documentation. Never put server credentials or private keys in frontend build configuration because browser-delivered values are public at runtime.
- Keep `current-state.md` short and replace stale operational details.
- Version documentation with the code it describes.

## Change discipline

- Preserve user changes and avoid unrelated edits.
- Make assumptions explicit when they are necessary.
- Verify changes in proportion to risk and report what was checked.
- Keep development and token use cost-conscious: use the smallest sufficient test set during implementation, avoid repeated full-suite runs, and reserve full verification for meaningful checkpoints such as task completion, integration, or publication. Read only task-relevant files, avoid redundant scans and repeated diagnostics, and do not rerun a successful command unless code, configuration, environment, or the claim being verified changed.
- Do not blindly repeat a failed operation. First identify the symptom and likely environmental or procedural cause; retry only after a relevant cause, input, configuration, or environment has changed or the retry itself is the diagnostic step.
- Learn from failures: for every material tooling, environment, deployment, test, or integration failure, record the symptom, root cause, durable remedy, safe verification evidence, and prevention rule in the appropriate canonical document. Put reusable development/environment remedies in `docs/project/standards.md`; put current task state in `current-state.md`; use a decision record when the rationale is consequential.
- Do not use subagents, parallel review loops, or repeated independent reviews unless the user explicitly requests them or a concrete high-risk blocker cannot be resolved efficiently in the primary task.
- Use Inline Execution as the default workflow for this project. Do not suggest Subagent-Driven execution unless the user explicitly requests it.
- Prefer focused tests for changed behavior and critical security boundaries; do not add redundant, low-value, or coverage-only tests.
- Require an explicit confirmation before every user-visible delete, removal, archive, or deactivation action. Cancellation or dismissal must leave state unchanged and must not send a mutation request.
- Keep files focused so future tasks can load only relevant context.
- Treat Persian and every other non-ASCII value as Unicode end-to-end. At a cross-process, file, database, or web-service boundary, explicitly select UTF-8 or the destination's documented Unicode encoding; never depend on a Windows console code page or process default.
- Before a deployment tool writes user-supplied text, validate that the text survives its boundary unchanged. Use a synthetic Unicode probe or a non-sensitive integrity result and never expose the checked value in logs, diagnostics, source control, or test output.

## Integration and publication

- Before any local merge or push, update `AGENTS.md` and the canonical project documents with all durable requirements, decisions, agreements, state changes, and next steps introduced by the work.
- The user established the Persian release shorthand `نهایی کن`: when used for a completed change, it is explicit authorization to commit the verified source, merge it into the intended destination branch, push that branch to its configured remote, publish the matching API/UI artifacts to the configured local IIS installation, and run the documented post-publication smoke checks. Do not treat the phrase as authorization to discard unrelated user changes or to deploy to production.
- Verify the documentation updates and the merged result before publication.
- A local merge must never remain local-only. After a successful merge and verification, push the destination branch to its configured remote.
- If the required push cannot be completed, the integration is incomplete; preserve the work and report the failure clearly.
