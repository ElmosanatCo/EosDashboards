# 0005 — Local Credential Authentication for Phase 1

**Status:** Accepted

**Date:** 2026-09-02

## Context

The original phase-1 Windows/AD sign-in flow could not be reliably browser-validated from the developer's home VPN connection because the workstation secure channel was unavailable and Chrome rejected IIS integrated-authentication credentials. The first release needs a working local development authentication flow without changing organizational directory infrastructure.

## Decision

Replace phase-1 Windows/AD sign-in with pre-provisioned local username/password authentication followed by mandatory SMS OTP for every new eight-hour application session.

Passwords are 8 to 128 characters long with no character-class composition rule. They are held only as standard salted hashes. The private deployment provisioner is the sole account and password-management mechanism in this slice. Signed-in users can change passwords by supplying their current password. Password recovery requires a purpose-isolated SMS OTP. Changing or resetting a password revokes every active session for that user.

The API will use anonymous IIS access at the transport boundary and enforce application authentication through credentials, OTP, JWTs, refresh cookies, origin/anti-forgery protections, rate limits, and server-side authorization.

## Rationale

This focused implementation preserves the approved second factor and session security while removing an environmental dependency that is outside the local development scope. Extending the existing user/session/OTP architecture avoids the cost and schema breadth of a full identity framework, while preserving a clean boundary for a later organizational identity-provider integration.

## Consequences

- The current Windows/AD adapter, Windows-specific API authorization policy, and IIS Windows Authentication requirement are replaced for this slice.
- A schema migration, private-provisioning input, API endpoints, focused tests, and redesigned Persian RTL sign-in, recovery, and password-change UI are required.
- Future organizational directory integration remains a separately approved discovery and implementation effort.

## Supersedes

This supersedes the Windows/AD identity portion of decision 0004. All unrelated repository, session, OTP, shell, and delivery choices in decision 0004 remain accepted.
