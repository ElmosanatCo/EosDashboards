# Google Sign-in Design

**Date:** 2026-09-03

**Status:** Proposed — approved in-chat outline, pending document review

## Goal

Let an active, pre-provisioned EosDashboards user sign in with an explicitly linked Google account. A successful Google sign-in creates the existing eight-hour application session directly, without requiring the local password or SMS OTP. The existing username/password plus mandatory SMS OTP flow remains available and unchanged.

This is account linking, not self-registration. A Google account never creates a user, role, or permission.

## Chosen approach

The API owns an OpenID Connect Authorization Code flow with PKCE. The browser is redirected to Google and returns to a server-side callback. This avoids exposing the Google client secret or handling Google tokens in React.

The local callback URI is:

```text
https://localhost/EosDashboardsApi/api/v1/auth/google/callback
```

The Google OAuth client is a Web application client configured with that exact authorized redirect URI. Production uses its separately approved HTTPS hostname and corresponding OAuth client configuration; no production hostname is introduced in this slice.

## User experience

1. The sign-in page obtains the enabled external providers from the anonymous API capability endpoint.
2. When Google is enabled, the page presents a clear `ورود با Google` action alongside the existing password-and-SMS route. The local credential form and recovery action remain available.
3. Selecting Google navigates the current page to the API start endpoint. The API immediately redirects to Google; there is no intermediate empty/loading page.
4. After success, the API establishes the standard application refresh session and redirects to `/EosDashboards/`. The SPA bootstraps the ordinary in-memory access token from that session and shows the dashboard.
5. Cancellation, a denied Google consent result, an unavailable provider, or an unauthorized Google account return the visitor to the sign-in view with helpful, non-enumerating Persian feedback. No password or OTP is requested as a fallback automatically.

On narrow screens the Google action follows the existing responsive form rules. It must remain an obvious, properly padded control with keyboard focus and sufficient contrast in every saved theme and palette.

## Account linking and data model

Add an `ExternalIdentityLinks` table with an auto-incrementing `bigint Id` and these conceptual fields:

- `UserId`, required foreign key to the existing user;
- `Provider`, initially the constant `Google`;
- `NormalizedEmail`, the pre-approved Google email, normalized with the documented username/email comparer;
- `ProviderSubject`, the stable Google OpenID Connect `sub`, initially absent until the first successful Google authorization;
- created and linked timestamps.

Database constraints prevent one Google email or one Google subject from being linked to more than one EosDashboards user. The deployment-only administrator provisioner accepts an optional Google email and creates or updates that user's pre-approved link. It never prints the email.

For the first Google login, the API requires a Google-verified email matching an unbound pre-approved link, then atomically stores the provider subject. On later logins it identifies the user by the provider subject and still requires an active EosDashboards user. Google email changes do not silently change an existing link; they require an explicit administrator update through the provisioner until account-management UI is approved.

## API and authentication flow

The anonymous API exposes:

```text
GET /api/v1/auth/providers
GET /api/v1/auth/google/start
GET /api/v1/auth/google/callback
```

`providers` reveals only whether Google sign-in is enabled. `start` creates a short-lived, single-use correlation record and redirects to Google. The correlation uses a Secure, HttpOnly, host-only cookie with `SameSite=Lax`, a ten-minute maximum lifetime, protected state, an OpenID Connect nonce, and the PKCE verifier. `callback` validates the correlation/state/nonce, exchanges the authorization code server-to-server, and validates the Google ID token using Google's issuer metadata, signature keys, issuer, audience, expiry, nonce, and verified-email claim.

The callback accepts only the configured client identifier and redirect URI. It creates the same `UserSession`, refresh cookie, and access-token behavior used after a verified local OTP, including the existing absolute eight-hour expiry and refresh rotation. It redirects without an access token in the URL, fragment, logs, or browser storage.

Local password sign-in, OTP verification, recovery, password change, logout, session refresh, anti-forgery, CORS, and authorization policies remain unchanged. Google sign-in never weakens the Secure `__Host-` cookie requirement, and live manual validation remains on the IIS HTTPS application rather than the HTTP Vite preview.

## Configuration and operations

The API receives a typed `GoogleAuthentication` section with `Enabled`, `ClientId`, `ClientSecret`, and `RedirectUri`. The client identifier may be disclosed to the browser only through the normal OAuth redirect, while the secret stays server-side. Under decision 0006, local development values may be held in the private repository's API/IIS configuration but are never printed, logged, or documented. The frontend receives no secret.

When disabled or incompletely configured, startup validation prevents an accidental partial activation and `/auth/providers` reports Google unavailable. The deployment guide will include the concise Google Cloud Console setup: consent screen, Web OAuth client, exact authorized redirect URI, server-side configuration, optional linked-email provisioning, publish, and HTTPS smoke check.

## Security and observability

- Only an active application user with a pre-approved link can enter; unknown accounts get no account-existence detail.
- Authorization code, PKCE verifier, state, nonce, ID token, refresh credential, client secret, and complete Google email are absent from logs, audit metadata, errors, URLs after callback, and browser storage.
- Safe audit events distinguish start, success, cancellation/denial, correlation rejection, identity validation failure, unlinked identity, and disabled-provider behavior without sensitive values.
- Anonymous start/callback endpoints receive focused abuse protection and safe error handling. The callback permits the expected top-level cross-site navigation; it does not rely on the application's API-origin header rule intended for cookie-changing XHR calls.
- A local password reset or password change does not alter an external-identity link. A disabled user remains unable to sign in by either route.

## Verification

- Domain/Application tests cover link uniqueness, first-link binding, subsequent subject lookup, inactive-user denial, and standard session creation with the existing absolute expiry.
- API tests cover disabled-provider discovery, protected state/correlation rejection, callback failure mapping, safe redirects, and absence of tokens from responses and logs.
- Infrastructure tests use a deterministic fake Google token/code exchanger and metadata validator; automated tests never call Google.
- Frontend tests cover provider discovery, Google action availability, every theme/palette contrast state, and return-to-sign-in error feedback.
- A mocked browser flow covers redirect initiation, successful automatic dashboard entry, cancellation, and unlinked-account denial.
- After configuration is complete, one user-authorized IIS HTTPS smoke flow verifies the provided linked account. No automated or unapproved real Google authorization is used.

## Out of scope

- Google self-registration, just-in-time user/role creation, and automatic email-based matching without a pre-approved link;
- additional identity providers, Google Workspace domain restrictions, account-link management UI, unlinking, or delegated administration;
- production OAuth client setup, production hostname/certificate changes, and production deployment.
