# Decision 0006 — Local development configuration

**Status:** Accepted

**Date:** 2026-09-03

## Context

Repeated secret-entry and missing-configuration failures slowed local development on the user's personal machine. The repository is private and used for this local development workflow.

## Decision

The user explicitly permits local development SQL credentials, SMS endpoint settings, and API security keys to be tracked in `backend/src/EosDashboards.Api/appsettings.Development.json`.

Their values must never be printed in tool output, logs, error responses, tests, or documentation. Personal data and all production configuration, credentials, and connection details remain outside source control.

## Consequences

- Local API execution can use the tracked development configuration without repeated private-file lookup or manual environment-variable setup.
- The private repository is intentionally trusted with these local development values.
- Production deployment continues to require separately managed configuration.
