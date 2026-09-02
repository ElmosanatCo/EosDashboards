# EosDashboards Agent Instructions

## Required startup

1. Read `docs/project/current-state.md`.
2. Read `docs/project/README.md` and only the canonical documents relevant to the task.
3. Treat repository documentation as authoritative over chat recollection.
4. Report contradictions or missing decisions; do not silently invent answers.

## Confirmed project baseline

- EosDashboards is a web application that will provide multiple dashboards to company managers.
- Backend: .NET Core, Entity Framework Core, and REST APIs.
- Database: SQL Server.
- Frontend: React.js.
- UI foundation: Material UI, Persian RTL, and the locally hosted Vazirmatn font.
- Frontend behavior: React SPA with closable internal workspace tabs; the home tab is fixed.
- Hosting: the React UI and ASP.NET Core API are separate IIS sites/applications with separate application pools.
- Architecture: lightweight clean layering; API -> Application -> Domain, with Infrastructure as the only database and external-system access layer.
- Repository layout: independently openable `backend/` Visual Studio solution and `frontend/` VS Code workspace in one repository.
- Authentication phase 1: Windows/AD organizational sign-in followed by mandatory SMS OTP for every new eight-hour application session; no local password.
- Initial access: one pre-provisioned database user has the System Administrator role; user/role administration UI is deferred.
- All principal application tables use auto-incrementing SQL Server `bigint` primary keys named `Id`, subject to the documented narrow exceptions.
- Branding: company name is `علم و صنعت`; the approved EOS logo must be used unchanged, but its source file is currently missing from the repository and no substitute is permitted.
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
- Mark unapproved ideas as proposed or unresolved.
- Add a decision record only for consequential choices whose rationale must remain auditable.
- Never store secrets, credentials, personal data, or production connection details.
- Keep `current-state.md` short and replace stale operational details.
- Version documentation with the code it describes.

## Change discipline

- Preserve user changes and avoid unrelated edits.
- Make assumptions explicit when they are necessary.
- Verify changes in proportion to risk and report what was checked.
- Keep development cost-conscious: use the smallest sufficient test set during implementation, avoid repeated full-suite runs, and reserve full verification for meaningful checkpoints such as task completion, integration, or publication.
- Do not use subagents, parallel review loops, or repeated independent reviews unless the user explicitly requests them or a concrete high-risk blocker cannot be resolved efficiently in the primary task.
- Prefer focused tests for changed behavior and critical security boundaries; do not add redundant, low-value, or coverage-only tests.
- Keep files focused so future tasks can load only relevant context.

## Integration and publication

- Before any local merge or push, update `AGENTS.md` and the canonical project documents with all durable requirements, decisions, agreements, state changes, and next steps introduced by the work.
- Verify the documentation updates and the merged result before publication.
- A local merge must never remain local-only. After a successful merge and verification, push the destination branch to its configured remote.
- If the required push cannot be completed, the integration is incomplete; preserve the work and report the failure clearly.
