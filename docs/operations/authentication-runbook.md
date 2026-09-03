# Authentication runbook

## Normal flows

The visitor submits a pre-provisioned username and password, verifies the SMS OTP, receives a ten-minute access token in memory, and receives a rotated refresh credential in a Secure, HttpOnly, SameSite Strict cookie. The absolute application session is eight hours.

An active user that has been explicitly linked to one verified Google identity
may instead choose Google sign-in. The server validates that identity and then
issues the same application session directly; it never creates or authorizes a
user automatically. See `docs/operations/google-sign-in.md` for activation and
safe troubleshooting.

## Checks by symptom

- **Sign-in unavailable:** verify API availability, the credential challenge endpoint, and rate-limit configuration. The API transport boundary uses anonymous IIS access; normal protected endpoints use JWT.
- **Account denied:** verify the pre-provisioned user is active and that its configured username is correct. Do not disclose whether an account exists to the caller.
- **SMS unavailable:** verify the HTTPS SOAP endpoint and timeout, then use the trace ID to correlate safe audit events. Do not log the OTP or complete mobile.
- **OTP rejected:** check expiry, attempt exhaustion, resend cooldown, clock synchronization, and whether a newer challenge superseded it.
- **Refresh/logout rejected:** verify exact Origin, anti-forgery cookie/header match, Secure cookie delivery, and the active database session.
- **Google sign-in unavailable or rejected:** follow the safe checks in
  `docs/operations/google-sign-in.md`; do not disclose whether a Google email
  address is linked to an account.
- **Readiness failed:** check SQL connectivity and SMS configuration. Readiness never sends a message.

Revoke or deactivate compromised sessions/users in the database through an approved administrative procedure. Preserve audit logs and trace IDs; never copy tokens, OTP values, keys, cookies, or full personal data into tickets.
