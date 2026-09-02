# EosDashboards Project Memory Design

**Status:** Approved in conversation on 2026-09-02  
**Scope:** Persistent, repository-based context for AI-assisted development

## Purpose

EosDashboards will be developed across many short AI conversations. The repository therefore needs a compact, durable source of truth that lets a new Codex task recover the project's important context without replaying prior conversations or loading unnecessary text.

The memory system must preserve:

- agreed product goals and scope;
- confirmed requirements and capabilities;
- architecture and technology decisions;
- working agreements and project rules;
- current implementation status and next steps;
- unresolved questions that can materially change the product.

It must not preserve raw chat transcripts, repeated explanations, temporary exploration, or unsupported assumptions.

## Confirmed Initial Context

- Repository: `https://github.com/ElmosanatCo/EosDashboards`
- Product: a web application that provides multiple dashboards to company managers.
- Backend baseline: .NET Core, Entity Framework Core, and REST APIs.
- Database baseline: SQL Server.
- Frontend baseline: React.js.
- Documentation language: concise, plain English.
- Conversation language: Persian unless the user requests otherwise.

These statements are initial constraints, not a complete architecture or product specification.

## Chosen Approach

Use a short root-level `AGENTS.md` as the automatically discovered entry point and place detailed knowledge in focused documents under `docs/project/`.

This approach is preferred over a single large instruction file because it keeps startup context small. It is preferred over a chronological conversation log because canonical topic documents make the current truth easier to find and prevent obsolete decisions from competing with newer ones.

## Information Architecture

### `AGENTS.md`

The repository entry point for AI agents. It will contain only:

- the project's identity and confirmed technology baseline;
- the required startup reading sequence;
- rules for distinguishing confirmed facts from proposals;
- the documentation maintenance protocol;
- links to the canonical project documents;
- essential engineering and verification rules once those rules are agreed.

It must remain concise. Detailed requirements, rationale, and history belong in the referenced documents.

### `docs/project/README.md`

The memory index. It describes each canonical document, when an agent should read it, and where new information belongs.

### `docs/project/current-state.md`

A short operational snapshot that every new task must read. It records:

- the current project phase;
- completed and active work;
- the next agreed step;
- active blockers;
- unresolved questions that affect immediate work.

Historical progress is removed or condensed when it no longer helps the next task.

### `docs/project/vision.md`

The durable product definition: business purpose, intended users, outcomes, scope boundaries, and success criteria.

### `docs/project/requirements.md`

Canonical functional and non-functional requirements. Each requirement must be clearly marked as confirmed, proposed, or deferred until it is confirmed. Superseded text is replaced rather than duplicated.

### `docs/project/architecture.md`

The current system architecture, component boundaries, data flow, technology choices, integration constraints, security considerations, and deployment assumptions. It describes the current accepted design and links to decision records for rationale.

### `docs/project/decisions/`

Immutable decision records for choices with meaningful alternatives or long-term consequences. Each record includes context, decision, rationale, consequences, and status. If a decision changes, a new record supersedes the old one instead of rewriting history.

### `docs/project/roadmap.md`

Ordered delivery phases, milestones, priorities, and dependencies. It is not a detailed task tracker and contains only planning information needed across conversations.

## Startup Protocol

At the start of a new task in this repository, an AI agent must:

1. Read `AGENTS.md` and `docs/project/current-state.md`.
2. Read `docs/project/README.md` to identify additional documents relevant to the task.
3. Read only those relevant documents before proposing or changing work.
4. Treat canonical documents as authoritative over chat recollection.
5. Surface contradictions or missing decisions instead of silently inventing an answer.

This protocol makes core context recoverable while keeping input context proportional to the task.

## Documentation Update Protocol

Before completing work, the acting agent must evaluate whether the task introduced durable information. If it did, the agent must update the appropriate canonical document in the same change.

The following information must be recorded:

- a requirement was confirmed, changed, deferred, or rejected;
- an architectural or product decision was made;
- a project-wide working rule was agreed;
- the implementation state or next agreed step changed;
- a material unresolved question or blocker appeared or was resolved.

The following information must not be recorded:

- raw conversation transcripts;
- temporary reasoning or discarded exploration;
- facts already expressed clearly in a canonical document;
- guesses presented as facts;
- secrets, credentials, personal data, or production connection details.

Updates must be concise and integrated into the current source of truth. A decision record is added only when rationale and alternatives need to remain auditable.

## Consistency Rules

- `current-state.md` reflects the latest operational state but does not redefine requirements or architecture.
- `requirements.md` defines what the product must do; `architecture.md` defines how the accepted system will support it.
- Decision records explain why consequential choices were made.
- `roadmap.md` orders delivery but does not turn proposals into confirmed requirements.
- Dates use ISO `YYYY-MM-DD` format.
- Links use repository-relative paths.
- Documentation changes are versioned in Git with the code they describe.

## Verification

The memory system is successful when a fresh Codex task opened at the repository root can:

1. identify the product and confirmed technology baseline;
2. find the current phase and next agreed step;
3. locate relevant requirements and decisions without reading every document;
4. distinguish confirmed facts from proposals and unresolved questions;
5. update durable project knowledge before finishing relevant work.

Verification will include checking all links, confirming the startup instructions are unambiguous, scanning for duplicated or contradictory facts, and reviewing the total startup context for unnecessary text.

## Initial Delivery Boundary

The first implementation creates the structure above and seeds it only with confirmed information from the conversation. It does not scaffold the application, choose unconfirmed framework versions, invent dashboard requirements, or define authentication, deployment, security, or UI behavior before those topics are discussed and approved.
