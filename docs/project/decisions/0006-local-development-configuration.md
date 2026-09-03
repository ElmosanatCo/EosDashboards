# Decision 0006 — Local development configuration

**Status:** Accepted

**Date:** 2026-09-03

## Context

Repeated secret-entry and missing-configuration failures slowed local development on the user's personal machine. The repository is private and used for this local development workflow.

## Decision

The user explicitly permits local development SQL credentials, service endpoint settings, API security keys, and other server-side local settings to be tracked in API/IIS configuration. Public frontend endpoint settings may also be tracked where needed. The established fallback source is `D:\Workspaces\ChatGpt\Private Data For AI Projects\EosDashboards`; do not repeatedly ask for values already available there.

Their values must never be printed in tool output, logs, error responses, tests, or documentation. Personal data and all production configuration, credentials, and connection details remain outside source control.

## Consequences

- Local API and IIS execution can use tracked configuration without repeated private-file lookup or manual environment-variable setup.
- Browser-delivered frontend configuration remains limited to public values such as API addresses; server credentials and private keys cannot be safely used there because they are visible to every browser user at runtime.
- The private repository is intentionally trusted with these local development values.
- Production deployment continues to require separately managed configuration.
