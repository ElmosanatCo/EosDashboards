# Local Credential Authentication and Sign-in Experience Design

**Date:** 2026-09-02

**Status:** Approved

## Goal

Use local username/password authentication with mandatory SMS OTP for every new eight-hour application session. Deliver an elegant Persian RTL sign-in, password-recovery, and password-change experience suitable for a management application. Keep account and password administration out of the UI for this slice.

## Scope

- Pre-provisioned active accounts sign in with a unique username and password.
- Successful password verification begins a purpose-isolated SMS OTP challenge for sign-in.
- A signed-in user changes a password by supplying the current and new passwords.
- A user resets a forgotten password through a purpose-isolated SMS OTP challenge.
- Password changes and resets revoke all active sessions for that user.
- The deployment-only provisioner receives the administrator username and password through the existing private runtime input path.
- API IIS Anonymous Authentication is enabled at the transport boundary; application authentication is enforced by credentials, OTP, JWTs, refresh cookies, origin protections, rate limits, and authorization.
- The current minimal sign-in screen becomes a polished, responsive, accessible Material UI/Vazirmatn Persian RTL experience.

Out of scope: user/role administration UI, password-history policy, self-registration, email recovery, organizational directory integration, and production deployment.

## Authentication and Session Flows

### Sign-in

1. The visitor supplies username and password over HTTPS.
2. The API performs a generic, rate-limited verification against an active pre-provisioned account. It never reveals whether a username exists.
3. On success, the API creates and sends a `SignIn` OTP challenge using the existing protected mobile number and returns only its opaque token, masked mobile, expiry, and resend time. The opaque token can request one replacement challenge after its 60-second cooldown; a replacement invalidates the prior challenge without retaining the password in the browser.
4. The visitor submits the OTP. Only a valid `SignIn` challenge creates the existing eight-hour session, ten-minute JWT access token, and Secure/HttpOnly rotating refresh cookie.

### Forgot password

1. The visitor supplies a username. The API returns a generic result whether or not an eligible account exists.
2. For an eligible account, the API creates and sends a `PasswordReset` OTP challenge. It has the same five-minute expiry, five-attempt limit, and cooldown as sign-in OTPs, but is not interchangeable with them. Its opaque token can also request a replacement after the cooldown.
3. The visitor supplies the OTP and a new password in one HTTPS completion request. A valid reset challenge is consumed atomically, the password hash is replaced, and all existing sessions for that user are revoked. The API does not create a session.

### Change password

1. An authenticated user supplies the current and a new password to a JWT-protected endpoint.
2. The API verifies the current password, updates the hash, revokes all active sessions for that user, and expires the current refresh cookie.
3. The UI clears local application state and returns to the sign-in screen. The user must complete password sign-in and OTP again.

## Password and Data Design

- `Users` receives a unique normalized `Username` and a `PasswordHash` field. Existing organizational identifier and account-display fields remain for continuity; they no longer establish browser authentication.
- Passwords are valid at 8 to 128 characters with no composition rule. The plaintext is neither normalized nor trimmed before hashing, and is never persisted, logged, audited, or returned.
- The password service uses the platform standard salted password-hash format with versioned verification and rehash support. It is an Application port implemented in Infrastructure.
- `OtpChallenges` receives a purpose discriminator: `SignIn` or `PasswordReset`. Purpose is enforced in every start and completion use case.
- A schema migration is additive. The deployment provisioner updates the existing administrator record with private username/password input before the new UI is used.
- Password verification failures, password changes, resets, reset OTP sends, and rate-limit denials are audit events with safe metadata only. Successful change/reset revokes every unrevoked session owned by that user.

## API Design

```text
POST /api/v1/auth/sign-in/challenges
  { username, password }

POST /api/v1/auth/sign-in/challenges/{token}/resend
POST /api/v1/auth/sign-in/challenges/{token}/verify
  { code }                       # SignIn purpose only; returns session tokens

POST /api/v1/auth/password-reset/challenges
  { username }

POST /api/v1/auth/password-reset/challenges/{token}/resend
POST /api/v1/auth/password-reset/challenges/{token}/complete
  { code, newPassword }          # PasswordReset purpose only; creates no session

POST /api/v1/auth/password
  { currentPassword, newPassword } # ActiveUser required; expires current session

POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

The existing exact-origin CORS policy, `no-store` behavior, refresh-cookie settings, anti-forgery protection for cookie-changing refresh/logout operations, JWT authorization, safe problem details, and sensitive-endpoint rate limiting remain in effect. IIS Anonymous Authentication enables API transport access; endpoint authorization protects application resources.

## Visual and Interaction Design

The sign-in experience uses a composed desktop layout rather than an isolated generic card:

- On wide screens, a deep brand panel and a quiet form panel share the viewport. The brand panel contains the unmodified transparent EOS logo, `علم و صنعت`, a concise product statement, and a restrained geometric accent.
- The form panel has a carefully spaced, medium-width surface with a clear Persian heading, a short contextual subheading, username and password fields, password visibility control, a prominent teal primary action, and a visible but secondary forgotten-password path.
- The OTP state preserves the same surrounding composition and uses a readable step cue, large six-digit accessible entry, masked mobile, a five-minute validity notice, an independent 60-second resend countdown/state, and back action. Password recovery has the same visual family and asks for a new password before completion.
- The authenticated shell exposes password change from the current-user menu. Its dialog asks for the current and new password with immediate clear validation.
- Loading, disabled, success, and error states use text and icons in addition to color. Error copy is helpful without disclosing account existence or authentication details.
- Vazirmatn, RTL ordering, keyboard navigation, visible focus, WCAG AA contrast, reduced motion, appropriate autocomplete hints, and responsive mobile collapse are mandatory. On narrow screens the brand panel becomes a compact header so the form remains the priority.
- Motion is short and functional: state transitions may fade or shift subtly, but do not delay input or distract from authentication.

## Deployment

The focused local deployment sequence is:

1. Apply the reviewed additive EF migration to the development SQL Server database.
2. Run the private deployment provisioner to set the existing administrator username and password without printing either value.
3. Publish versioned backend and frontend artifacts using the existing deployment procedure.
4. Keep IIS Anonymous Authentication enabled for `/EosDashboardsApi`; preserve the separate application pool and HTTPS configuration.
5. Run limited health and critical-flow smoke checks. Do not modify company domain, DNS, SPN, or production infrastructure.

## Focused Verification

- Domain/Application tests: password bounds, hash verification, generic denial, distinct OTP purposes, reset/change session revocation, and rejection of wrong-purpose or expired OTPs.
- Integration tests: migration mapping and uniqueness, credential/OTP API contracts, authorization boundary, and safe password handling.
- Frontend tests: sign-in, recovery, and change-password form validation/state transitions; OTP remains covered through its adapted purpose-aware flow.
- One local browser smoke flow verifies successful password sign-in, OTP, refresh, logout, password change/logout behavior, and recovery. Real SMS is used only when expressly authorized for that single smoke flow; otherwise a safe fake is used.

No broad repeated suites, unneeded browser passes, or redundant real-SMS tests are part of this work.
