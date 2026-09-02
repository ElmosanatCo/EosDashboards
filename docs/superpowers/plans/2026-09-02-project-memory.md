# Project Memory Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a compact, repository-based project memory that gives every new Codex task the confirmed context, current state, and rules needed to continue EosDashboards safely.

**Architecture:** A concise root `AGENTS.md` acts as the automatically discovered entry point. It requires agents to read a small operational snapshot and routes them to focused canonical documents under `docs/project/`, so detailed context is loaded only when relevant.

**Tech Stack:** Markdown, Git, Codex `AGENTS.md` instruction discovery

**Spec:** `docs/superpowers/specs/2026-09-02-project-memory-design.md`

## Global Constraints

- Seed documents only with information confirmed in the approved design.
- Documentation uses concise, plain English.
- User conversation remains Persian unless the user requests otherwise.
- Do not invent framework versions, dashboard details, authentication, deployment, security, or UI behavior.
- Do not store raw chat transcripts, temporary reasoning, secrets, credentials, personal data, or production connection details.
- Use ISO `YYYY-MM-DD` dates and repository-relative links.
- Keep `AGENTS.md` short enough to remain well below Codex's default combined project-instruction limit of 32 KiB.

## File Map

- `AGENTS.md`: Automatically loaded project identity, startup protocol, source-of-truth rules, and documentation update contract.
- `docs/project/README.md`: Memory index that explains which canonical document to read and update.
- `docs/project/current-state.md`: Minimal mandatory snapshot of phase, completed work, next step, blockers, and immediate questions.
- `docs/project/vision.md`: Confirmed business purpose, users, outcomes, and scope boundaries.
- `docs/project/requirements.md`: Status-labelled functional and non-functional requirements.
- `docs/project/architecture.md`: Accepted technology baseline and explicitly unresolved architecture topics.
- `docs/project/roadmap.md`: Ordered phases without speculative deadlines.
- `docs/project/decisions/README.md`: Decision-record rules and naming convention.
- `docs/project/decisions/0001-repository-project-memory.md`: Rationale and consequences for the memory architecture itself.

---

### Task 1: Create the automatically discovered memory entry layer

**Files:**

- Create: `AGENTS.md`
- Create: `docs/project/README.md`
- Create: `docs/project/current-state.md`
- Test: static content and link checks described below

**Interfaces:**

- Consumes: the approved design at `docs/superpowers/specs/2026-09-02-project-memory-design.md`
- Produces: a startup path from `AGENTS.md` to `docs/project/current-state.md` and the topic index

- [ ] **Step 1: Create `AGENTS.md` with the compact startup and maintenance contract**

Use this content:

```markdown
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
```

- [ ] **Step 2: Create `docs/project/README.md` as the routing index**

Use this content:

```markdown
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
```

- [ ] **Step 3: Create the initial `docs/project/current-state.md` snapshot**

Use this content:

```markdown
# Current Project State

**Last updated:** 2026-09-02

## Phase

Project inception and requirements discovery.

## Completed

- The empty GitHub repository was cloned locally.
- The repository-based project-memory design was approved.
- The implementation plan for the memory structure was prepared.

## In progress

- Establishing the canonical project-memory files and agent instructions.

## Next agreed step

Complete and verify the project-memory structure, then continue product discovery before scaffolding the application.

## Blockers

None.

## Immediate unresolved questions

- Which business dashboards and metrics are required first?
- Which managers or roles will use each dashboard?
- What source systems will supply dashboard data?
```

- [ ] **Step 4: Verify Task 1 content and links**

Run:

```powershell
Get-Item AGENTS.md, docs/project/README.md, docs/project/current-state.md | Select-Object FullName, Length
rg -n "docs/project/(current-state|README|vision|requirements|architecture|roadmap)\.md|docs/project/decisions/" AGENTS.md
```

Expected: all three files exist and every canonical path appears in `AGENTS.md`. Links to documents created in Task 2 may not resolve until Task 2 is complete.

- [ ] **Step 5: Commit the entry layer**

```powershell
git add -- AGENTS.md docs/project/README.md docs/project/current-state.md
git commit -m "docs: add project memory entry point"
```

---

### Task 2: Seed the canonical project sources of truth

**Files:**

- Create: `docs/project/vision.md`
- Create: `docs/project/requirements.md`
- Create: `docs/project/architecture.md`
- Create: `docs/project/roadmap.md`
- Create: `docs/project/decisions/README.md`
- Create: `docs/project/decisions/0001-repository-project-memory.md`
- Modify: `docs/project/current-state.md`
- Test: static consistency and unsupported-assumption checks described below

**Interfaces:**

- Consumes: status definitions from `docs/project/README.md` and baseline constraints from `AGENTS.md`
- Produces: canonical destinations for product, requirement, architecture, decision, and roadmap updates

- [ ] **Step 1: Create the product vision**

Create `docs/project/vision.md` with:

```markdown
# Product Vision

**Last updated:** 2026-09-02

## Confirmed purpose

EosDashboards is a company web application that will provide multiple dashboards to managers.

## Confirmed users

- Company managers.

Specific manager roles, departments, permissions, and audiences are unresolved.

## Expected outcome

Managers can access the dashboards relevant to their responsibilities through one web application.

The dashboards, metrics, source systems, update frequency, and success measures are unresolved and require product discovery.

## Scope boundary

The confirmed scope includes the dashboard platform. Authentication behavior, administration features, alerting, reporting, data-entry workflows, mobile support, and integrations are not yet confirmed.
```

- [ ] **Step 2: Create status-labelled requirements**

Create `docs/project/requirements.md` with:

```markdown
# Requirements

**Last updated:** 2026-09-02

## Confirmed functional requirements

### FR-001 — Multiple management dashboards

**Status:** Confirmed

The system provides multiple dashboards for company managers.

Acceptance criteria will be defined after the initial dashboards, metrics, users, and data sources are identified.

## Confirmed technical constraints

### TC-001 — Web application

**Status:** Confirmed

The product is delivered as a web application.

### TC-002 — Backend baseline

**Status:** Confirmed

The backend uses .NET Core, Entity Framework Core, and REST APIs.

### TC-003 — Database baseline

**Status:** Confirmed

The database platform is SQL Server.

### TC-004 — Frontend baseline

**Status:** Confirmed

The frontend uses React.js.

## Unresolved requirements

- Dashboard catalogue, metrics, filters, and drill-down behavior.
- User roles, authorization boundaries, and dashboard visibility.
- Data sources, ownership, refresh frequency, and historical retention.
- Performance, availability, audit, security, localization, accessibility, and browser targets.
- Exporting, printing, alerts, subscriptions, and administration capabilities.
```

- [ ] **Step 3: Create the accepted architecture baseline without inventing details**

Create `docs/project/architecture.md` with:

```markdown
# Architecture

**Last updated:** 2026-09-02

## Accepted baseline

EosDashboards is a web application with:

- a React.js frontend;
- a .NET Core backend exposing REST APIs;
- Entity Framework Core for data access;
- SQL Server as the database platform.

The exact framework versions and internal component boundaries are not yet selected.

## Conceptual data flow

The confirmed high-level direction is:

`React client -> REST API -> application and data-access logic -> SQL Server`

This is a conceptual constraint, not approval of a specific solution structure, hosting topology, or data-import architecture.

## Unresolved architecture topics

- .NET and React versions and frontend build tooling.
- Repository and solution structure.
- Authentication, authorization, and identity provider.
- Operational database versus analytical or warehouse data sources.
- Data ingestion, synchronization, caching, and refresh strategy.
- API conventions, validation, error format, pagination, and versioning.
- Dashboard rendering and charting libraries.
- Configuration, secret management, observability, testing, deployment, backup, and disaster recovery.
```

- [ ] **Step 4: Create the delivery roadmap**

Create `docs/project/roadmap.md` with:

```markdown
# Roadmap

**Last updated:** 2026-09-02

## Phase 1 — Project memory

**Status:** Complete

Create and verify the repository-based context and decision system.

## Phase 2 — Product discovery

**Status:** In progress

Identify users, dashboards, metrics, data sources, quality attributes, and the first deliverable slice.

## Phase 3 — Architecture and foundation

**Status:** Planned

Approve detailed architecture, select versions and libraries, scaffold the solution, and establish automated verification.

## Phase 4 — First vertical dashboard slice

**Status:** Planned

Deliver one approved dashboard end to end, including its data path, API, UI, authorization, and tests.

Later phases will be defined from validated requirements and feedback. No delivery dates are committed.
```

- [ ] **Step 5: Create decision-record guidance and the initial memory decision**

Create `docs/project/decisions/README.md` with:

```markdown
# Decision Records

Create a record for an accepted choice with meaningful alternatives or long-term consequences.

Use filenames in the form **NNNN-short-title.md**. Each record contains Status, Date, Context, Decision, Rationale, Consequences, and Supersedes/Superseded by when applicable.

Do not rewrite an accepted record to hide history. Add a new record that supersedes it.
```

Create `docs/project/decisions/0001-repository-project-memory.md` with:

```markdown
# 0001 — Repository-based project memory

**Status:** Accepted  
**Date:** 2026-09-02

## Context

EosDashboards will be developed through multiple short AI conversations. Repeating the full history wastes context, while relying on chat memory risks losing requirements and decisions.

## Decision

Use a concise root `AGENTS.md` as the automatic entry point and focused canonical documents under `docs/project/`. Every task reads the current-state snapshot, loads only relevant topic documents, and updates durable knowledge before completion.

## Rationale

This structure preserves continuity while limiting startup context. Canonical topic files expose the latest truth more clearly than chronological chat logs.

## Consequences

- Documentation changes are part of completing relevant implementation work.
- Agents must distinguish confirmed facts from proposals and unresolved questions.
- The current-state snapshot must remain short and current.
- Raw conversations and sensitive information are not stored.
```

- [ ] **Step 6: Mark the memory foundation complete in the operational snapshot**

In `docs/project/current-state.md`:

- change `Establishing the canonical project-memory files and agent instructions.` under **In progress** to `Product discovery: identifying the first dashboard's users, business questions, metrics, and data sources.`;
- change the **Phase** value to `Requirements discovery.`;
- add `The canonical project-memory structure and root agent instructions were created and verified.` under **Completed**;
- change **Next agreed step** to `Discover and confirm the first dashboard's users, business questions, metrics, and data sources before application scaffolding.`

- [ ] **Step 7: Verify canonical facts and absence of invented commitments**

Run:

```powershell
rg -n "\.NET Core|Entity Framework Core|REST API|SQL Server|React\.js" AGENTS.md docs/project
rg -n "authentication is|OAuth|JWT|\.NET [0-9]|React [0-9]|Vite|Redux|Docker|Azure|on-premises" AGENTS.md docs/project
```

Expected: the first command finds every confirmed technology baseline. The second command produces no output because none of those implementation choices has been confirmed.

- [ ] **Step 8: Commit canonical sources**

```powershell
git add -- docs/project
git commit -m "docs: seed canonical project context"
```

---

### Task 3: Verify memory discovery and repository consistency

**Files:**

- Modify only if verification exposes an error: `AGENTS.md`, `docs/project/*.md`, or `docs/project/decisions/*.md`
- Test: repository status, Markdown references, instruction size, placeholder scan, and startup-path review

**Interfaces:**

- Consumes: all files produced in Tasks 1 and 2
- Produces: a verified project-memory foundation ready for a new Codex task

- [ ] **Step 1: Check every canonical path referenced by the entry documents**

Run from the repository root:

```powershell
$required = @(
  'AGENTS.md',
  'docs/project/README.md',
  'docs/project/current-state.md',
  'docs/project/vision.md',
  'docs/project/requirements.md',
  'docs/project/architecture.md',
  'docs/project/roadmap.md',
  'docs/project/decisions/README.md',
  'docs/project/decisions/0001-repository-project-memory.md'
)
$required | Where-Object { -not (Test-Path $_) }
```

Expected: no output.

- [ ] **Step 2: Check instruction size and forbidden placeholders**

Run:

```powershell
(Get-Item AGENTS.md).Length
$forbidden = @('T' + 'BD', 'TO' + 'DO', 'fill' + ' in', 'implement' + ' later')
Get-ChildItem AGENTS.md, docs/project -Recurse -File | Select-String -SimpleMatch $forbidden
```

Expected: `AGENTS.md` is below 8192 bytes, leaving substantial room below the 32 KiB default combined limit. The placeholder scan produces no output.

- [ ] **Step 3: Review the mandatory startup path**

Run:

```powershell
Get-Content -Raw AGENTS.md
Get-Content -Raw docs/project/current-state.md
Get-Content -Raw docs/project/README.md
```

Expected: a new task can identify the product baseline, current phase, next step, and routing rules without opening the remaining canonical documents.

- [ ] **Step 4: Check the final change set**

Run:

```powershell
git status --short
git log --oneline --decorate -5
```

Expected: the working tree is clean and the history contains separate commits for the entry layer and canonical context after the approved design and implementation-plan commits.

- [ ] **Step 5: Correct and commit verification findings if needed**

If any prior check fails, edit only the incorrect memory files, repeat all Task 3 checks, then run:

```powershell
git add -- AGENTS.md docs/project
git commit -m "docs: correct project memory verification findings"
```

If every check passes without edits, do not create an empty commit.
