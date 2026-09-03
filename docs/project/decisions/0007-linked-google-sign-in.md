# 0007 — Pre-linked Google Sign-in

**Status:** Accepted

**Date:** 2026-09-03

## Context

Some approved users need a convenient sign-in method that does not require a
local password or an SMS message for every new application session. The
application must not turn a Google account into an unapproved user or weaken
the existing local credential and OTP route.

## Decision

An active, pre-provisioned EosDashboards user may be explicitly linked to one
Google account. Google sign-in uses a server-owned OpenID Connect Authorization
Code flow with PKCE. It issues the existing eight-hour application session
directly after Google validates the identity; it does not request a local
password or SMS OTP.

The first successful Google sign-in requires a Google-verified email that
matches a pending, administrator-provisioned link. It then binds Google's
stable subject identifier. Later sign-ins use that subject, while still
requiring the linked EosDashboards user to be active. Google never creates a
user, role, permission, or link automatically.

The local development callback is exactly
`https://localhost/EosDashboardsApi/api/v1/auth/google/callback`. Client ID,
client secret, and redirect URI are server-side configuration only. Google
remains disabled until a Web OAuth client has been created and all three values
are configured.

## Rationale

The server-side code flow keeps the client secret and identity tokens out of
the React application. Explicit pre-linking retains the existing approval and
authorization boundary, while stable-subject binding avoids reliance on a
mutable email address after the first verified sign-in.

## Consequences

- Local username/password plus SMS OTP remains available and unchanged.
- A Google Cloud project, consent configuration, Web OAuth client, exact
  redirect URI, and server-only configuration are required before activation.
- Google cancellation, denial, and unknown identities use generic feedback and
  never fall back automatically to OTP.
- Production requires its own approved HTTPS hostname and OAuth client.

