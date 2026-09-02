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
