# ADR 0003: Adopt Project-Wide Development Standards

- **Status:** Accepted
- **Date:** 2026-09-02

## Context

EosDashboards will be developed incrementally across many AI-assisted conversations. It needs consistent technical and visual decisions, durable context, auditable rationale, secure organizational operation, and documentation suitable for formal Persian presentation.

## Decision

Adopt `../standards.md` as the canonical project-wide standard. It governs documentation, Git integration, architecture, APIs, EF Core and SQL Server, identity, security, UI/UX, accessibility, resources, performance, testing, dependencies, observability, IIS deployment, recovery, and data governance.

Use concise English sources of truth for implementation and maintain aligned formal Persian documents for printing, archive, and external organizational review.

## Consequences

- Every future feature must comply with the standard or obtain and document an explicit exception.
- Future agents can recover approved cross-cutting decisions without loading chat history.
- Initial scaffolding has more defined quality gates and fewer implicit choices.
- Some environment-specific values remain deliberately unresolved until product or IT discovery.

## Alternatives considered

### Record rules only in chat

Rejected because new conversations cannot reliably recover them and decisions would not be versioned with code.

### Put every rule only in `AGENTS.md`

Rejected because the startup file would become too large. `AGENTS.md` carries the essential baseline and routes agents to the focused canonical standard.

### Select all libraries and infrastructure immediately

Rejected because charting, identity topology, monitoring, and exact versions depend on first-dashboard or IT evidence that is not yet available.
