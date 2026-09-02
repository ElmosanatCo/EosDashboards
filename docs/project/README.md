# Project Memory Index

This directory is the canonical, compact memory for EosDashboards. Update existing sources of truth instead of recording chat transcripts or repeating the same fact in several files.

| Document | Read when | Update when |
| --- | --- | --- |
| `current-state.md` | Every task | Phase, completed work, blocker, immediate question, or next step changes |
| `vision.md` | Product scope or priorities are relevant | Purpose, users, outcomes, success criteria, or boundaries are confirmed or changed |
| `requirements.md` | Designing or implementing behavior | A requirement is confirmed, changed, deferred, or rejected |
| `architecture.md` | Technical design or implementation is relevant | Accepted structure, integration, data flow, security, deployment, or technology changes |
| `decisions/` | A consequential choice or its rationale is relevant | A durable decision is accepted or superseded |
| `roadmap.md` | Planning or sequencing work | Delivery phases, priorities, or dependencies change |

## Status language

- **Confirmed:** explicitly agreed and authoritative.
- **Proposed:** under discussion and not approved for implementation.
- **Deferred:** intentionally postponed.
- **Rejected:** considered and not selected.
- **Unresolved:** a material answer is still required.

Use ISO `YYYY-MM-DD` dates and repository-relative links. Never store secrets, credentials, personal data, or production connection details.
