# Authentication runbook

## Normal flow

The browser either completes local username/password sign-in followed by SMS
OTP, or completes the enabled pre-linked Google route. Both routes create a
ten-minute access token in memory and a rotated refresh credential in a Secure,
HttpOnly, SameSite Strict cookie. The absolute application session is eight
hours.

## Checks by symptom

- **Local sign-in unavailable:** verify Anonymous Authentication is enabled and
  Windows Authentication is disabled for the API application. Verify the
  provisioned local username/password route; do not disclose account existence.
- **SMS unavailable:** verify the HTTPS SOAP endpoint and timeout, then use the trace ID to correlate safe audit events. Do not log the OTP or complete mobile.
- **OTP rejected:** check expiry, attempt exhaustion, resend cooldown, clock synchronization, and whether a newer challenge superseded it.
- **Google sign-in unavailable:** check the server-only
  `GoogleAuthentication` settings and the exact registered callback URI. Follow
  `google-sign-in.md`; do not expose the client secret or linked email.
- **Google identity denied:** verify through the approved administrative
  procedure that an active application user has the intended pre-linked Google
  account. Do not disclose which identity validation failed to the caller.
- **Refresh/logout rejected:** verify exact Origin, anti-forgery cookie/header match, Secure cookie delivery, and the active database session.
- **Readiness failed:** check SQL connectivity and SMS configuration. Readiness never sends a message.

Revoke or deactivate compromised sessions/users in the database through an approved administrative procedure. Preserve audit logs and trace IDs; never copy tokens, OTP values, keys, cookies, or full personal data into tickets.
