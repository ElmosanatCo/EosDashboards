# EosDashboards Project Development Standards Design

**Date:** 2026-09-02

**Status:** Approved

## Objective

Create a coherent, durable baseline for building a secure, Persian RTL management-dashboard platform over many short AI-assisted development conversations.

## Design summary

The approved approach combines:

- lightweight clean backend boundaries rather than either a monolith with mixed responsibilities or a heavy modular framework;
- separate React and ASP.NET Core IIS applications;
- EF Core Code First with controlled production migrations;
- intranet-first organizational authentication and deferred IT-dependent external identity design;
- an RTL-first Material UI design system with accessible appearance modes, selectable palettes, and Vazirmatn;
- measurable quality, security, performance, audit, recovery, and data-governance gates;
- concise English implementation sources plus formal Persian presentation documents;
- repository-managed durable memory and mandatory documentation-before-integration discipline.

## Key rationale

The lightweight layered model preserves the user's requested API -> business -> data separation while preventing database models from leaking through REST contracts. Infrastructure isolation also creates a safe boundary for future AD, LDAP, or other integrations.

The identity design begins with the known internal environment and a single organizational sign-in action. It avoids inventing an internet directory topology before IT confirms whether LDAP is distinct from AD and whether Entra ID, AD FS, or another supported broker exists.

The UI direction establishes a consistent shell and design language before individual forms and dashboards are created. Charting remains deferred so the first dashboard's actual comparison, trend, hierarchy, and volume needs drive that choice.

Production migration and deployment rules separate runtime from schema-changing privileges and retain auditable, reversible release artifacts. Operational checks cover both process liveness and dependency-aware readiness.

## Sources of truth

- Canonical rules: `../../project/standards.md`
- Architecture: `../../project/architecture.md`
- Requirements: `../../project/requirements.md`
- Accepted decision: `../../project/decisions/0003-project-wide-development-standards.md`
- Formal Persian edition: `../../formal/project-development-standards-fa.md`

## Deferred evidence

Before scaffolding or implementing affected areas, confirm supported framework versions and collect the first dashboard definition. Before production or external authentication work, obtain IT-approved identity topology, hostnames, certificates, browser matrix, monitoring platform, retention policy, backup schedule, and recovery objectives.
