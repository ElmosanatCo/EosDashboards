# Authentication runbook

## Normal flow

The browser creates a challenge using Windows/AD identity, verifies the SMS OTP, receives a ten-minute access token in memory, and receives a rotated refresh credential in a Secure, HttpOnly, SameSite Strict cookie. The absolute application session is eight hours.

## Checks by symptom

- **Organizational sign-in unavailable:** verify IIS Windows Authentication, SPN/site-zone settings, and that the challenge endpoint uses Negotiate while normal protected endpoints use JWT.
- **Account denied:** verify the pre-provisioned user is active and the stored organizational stable ID matches. Do not disclose whether an account exists to the caller.
- **SMS unavailable:** verify the HTTPS SOAP endpoint and timeout, then use the trace ID to correlate safe audit events. Do not log the OTP or complete mobile.
- **OTP rejected:** check expiry, attempt exhaustion, resend cooldown, clock synchronization, and whether a newer challenge superseded it.
- **Refresh/logout rejected:** verify exact Origin, anti-forgery cookie/header match, Secure cookie delivery, and the active database session.
- **Readiness failed:** check SQL connectivity and SMS configuration. Readiness never sends a message.

Revoke or deactivate compromised sessions/users in the database through an approved administrative procedure. Preserve audit logs and trace IDs; never copy tokens, OTP values, keys, cookies, or full personal data into tickets.
