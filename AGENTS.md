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
- Documentation: concise, plain English.
- User conversation: Persian unless the user requests otherwise.

These are baseline constraints, not a complete architecture. Do not choose unconfirmed versions, features, security behavior, deployment, or UI details without user approval.

## Sources of truth

- Memory index: `docs/project/README.md`
- Current work: `docs/project/current-state.md`
- Product definition: `docs/project/vision.md`
- Requirements: `docs/project/requirements.md`
- Architecture: `docs/project/architecture.md`
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
- Keep files focused so future tasks can load only relevant context.
