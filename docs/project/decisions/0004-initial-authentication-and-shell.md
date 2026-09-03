# ADR 0004: Deliver an Initial Authentication and Tabbed Shell Vertical Slice

- **Status:** Accepted
- **Date:** 2026-09-02

## Context

The repository contains documentation but no application code. The first useful implementation must establish durable backend/frontend boundaries, database conventions, secure organizational access, and the visual shell before dashboard-specific work begins.

The first release requires pre-provisioned local accounts, mandatory SMS second-factor verification, and one initial full-access administrator. Future identity-provider integration remains unresolved.

## Decision

Deliver one complete vertical slice with:

- a separate `backend/` Visual Studio solution and `frontend/` VS Code React SPA within the same repository;
- .NET 10 LTS, EF Core 10, React 19.2, TypeScript, Material UI 9, Node.js 24 LTS, and Vite;
- a controlled deployment-only tool that pre-provisions one System Administrator without storing personal data in source control;
- local username/password sign-in followed by mandatory six-digit SMS OTP for each new eight-hour session;
- ten-minute in-memory JWT access tokens and revocable hashed refresh credentials in Secure, HttpOnly cookies;
- a Persian RTL application shell with branding, themes, fixed header/status bar, collapsible menu, and closable route-aware internal tabs;
- automated tests using fake identity and SMS adapters, with no automated real SMS sends.

## Rationale

A full vertical slice validates every architectural boundary and produces a visible, secure foundation. Separate top-level applications match the intended Visual Studio/VS Code workflows and independent IIS hosting. Pre-provisioning prevents unapproved first-login account creation. Infrastructure adapters keep the legacy SOAP SMS service replaceable.

## Consequences

- Dashboard implementation can begin on a proven authenticated shell.
- Phase 1 requires the company SMS service to be reachable for new sessions.
- Initial administrator creation is an explicit deployment operation.
- User/role administration, real dashboards, external login, and charting remain outside this slice.
- Environment-specific database credentials, administrator personal data, cryptographic keys, and SMS endpoint values remain outside source control.

## Alternatives considered

### Backend-first or frontend-mock-first delivery

Not selected because each delays validation of either the real user experience or the real security/data path.

### Automatic first-login administrator creation

Not selected because a deployment-controlled pre-provisioning step provides a clearer security boundary.

### Seed personal data through an EF migration

Rejected because it would persist personal data in repository history.
