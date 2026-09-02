# Architecture

**Last updated:** 2026-09-02

## Accepted baseline

EosDashboards is a web application with:

- a React.js frontend;
- a .NET Core backend exposing REST APIs;
- Entity Framework Core for data access;
- SQL Server as the database platform.

Material UI is the frontend component foundation. The exact supported framework versions and build tooling will be selected immediately before scaffolding from supported releases.

## Conceptual data flow

The confirmed high-level direction is:

`React UI -> REST API -> Application -> Domain`

`Infrastructure -> Domain/Application ports -> SQL Server, AD/LDAP, and approved external systems`

The API is thin. Application coordinates use cases and transaction boundaries. Domain contains business concepts and rules. Infrastructure alone accesses EF Core, SQL Server, directory services, and other external systems. Domain persistence entities are never exposed as public API contracts.

## Identity boundary

Phase 1 is intranet-only. Windows/Active Directory establishes the user's organizational identity and the application creates its own authorized session. The approved session design uses short-lived JWT access tokens held in browser memory and a revocable refresh mechanism in a Secure, HttpOnly cookie. Logout revokes the application session and returns to the single-button sign-in screen.

LDAP access, if required, is server-side and protected by TLS with certificate validation. Directory protocols are never exposed directly to browsers or the internet. External access, the relationship between AD and LDAP, and possible Entra ID or AD FS capabilities remain deferred until IT discovery.

## Hosting topology

- React UI and ASP.NET Core API are separate IIS sites/applications and separate application pools.
- Only approved UI origins are allowed by API CORS policy.
- HTTPS is mandatory and HSTS is enabled in production.
- Environments, databases, configuration, secrets, deployment artifacts, and least-privilege runtime identities are separated.
- Production deployments use versioned artifacts, controlled offline handling, smoke tests, health probes, and a documented rollback path.

## Data architecture

- EF Core Code First migrations are authoritative.
- Only Infrastructure accesses the database.
- Principal tables use auto-incrementing `bigint` keys named `Id`; narrow exceptions are documented in `standards.md`.
- Lazy loading is disabled. Reads favor projections and no tracking. Large results are filtered and paged on the server.
- Business-operation transaction boundaries belong to Application and are implemented through Infrastructure.

## Cross-cutting architecture

- Central exception handling returns safe standard error objects with trace identifiers.
- Structured logging, audit records, liveness, readiness, metrics, and alerting support operations.
- REST endpoints are versioned under `/api/v1` and documented through OpenAPI.
- Caching is explicit, bounded, permission-safe, and measured.
- Long-running reports or calculations execute as background work when required.

## Unresolved architecture topics

- .NET and React versions and frontend build tooling.
- Repository and solution structure.
- Operational database versus analytical or warehouse data sources.
- Data ingestion, synchronization, caching, and refresh strategy.
- Dashboard rendering and charting libraries.
- Exact external identity provider/topology and directory integration protocol.
- Configuration provider, secret store, monitoring product, backup schedule, and disaster-recovery objectives.
