# 0005 — Local Credential Authentication for Phase 1

**Status:** Accepted

**Date:** 2026-09-02

## Context

The first release needs a browser-verifiable local authentication flow that does not depend on unapproved organizational identity infrastructure.

## Decision

Use pre-provisioned local username/password authentication followed by mandatory SMS OTP for every new eight-hour application session.

Passwords are 8 to 128 characters long with no character-class composition rule. They are held only as standard salted hashes. The private deployment provisioner is the sole account and password-management mechanism in this slice. Signed-in users can change passwords by supplying their current password. Password recovery requires a purpose-isolated SMS OTP. Changing or resetting a password revokes every active session for that user.

The API will use anonymous IIS access at the transport boundary and enforce application authentication through credentials, OTP, JWTs, refresh cookies, origin/anti-forgery protections, rate limits, and server-side authorization.

## Rationale

This focused implementation preserves the approved second factor and session security while removing an environmental dependency that is outside the local development scope. Extending the existing user/session/OTP architecture avoids the cost and schema breadth of a full identity framework, while preserving a clean boundary for a later organizational identity-provider integration.

## Consequences

- IIS Anonymous Authentication is enabled for the API transport boundary; application credentials, OTP, JWTs, refresh cookies, origin/anti-forgery protections, rate limits, and server-side authorization protect application access.
- A schema migration, private-provisioning input, API endpoints, focused tests, and redesigned Persian RTL sign-in, recovery, and password-change UI are required.
- Future organizational directory integration remains a separately approved discovery and implementation effort.
