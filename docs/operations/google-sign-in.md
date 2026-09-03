# Local Google sign-in activation

This runbook activates linked Google sign-in on the developer workstation. It
does not authorize production setup or user self-registration.

## Prerequisites

- An active EosDashboards user has an administrator-provisioned pending Google
  email link. The email is not written into this document or console output.
- The local UI and API are available through IIS HTTPS, not the Vite HTTP
  preview.
- The operator can administer the chosen Google Cloud project.
- The IIS API application-pool identity has outbound TLS access to Google's
  OpenID metadata, public signing-key, authorization, and token endpoints.
  In particular, it must be able to read
  `https://accounts.google.com/.well-known/openid-configuration` and
  `https://www.googleapis.com/oauth2/v3/certs`. Do not bypass signing-key
  validation when network policy blocks either endpoint.

## Create the Google client

1. In Google Cloud, select or create the development project and configure the
   OAuth consent/branding information required for its test users.
2. Create an OAuth client of type **Web application**.
3. Add this single authorized redirect URI, exactly as written:

   ```text
   https://localhost/EosDashboardsApi/api/v1/auth/google/callback
   ```

4. Record the generated Client ID and Client Secret in the server-side API/IIS
   configuration. Do not put either value in React, `VITE_*` settings, browser
   storage, screenshots, tickets, logs, or this runbook.

Google requires the redirect URI sent by the application to exactly match one
registered on the OAuth client, including scheme, host, port, path, case, and
trailing slash behavior.

## Configure the API

Set the API's server-side `GoogleAuthentication` settings:

```text
Enabled = true
ClientId = <Google Web OAuth client ID>
ClientSecret = <Google Web OAuth client secret>
RedirectUri = https://localhost/EosDashboardsApi/api/v1/auth/google/callback
```

The settings may be held in the private repository's API configuration or its
local IIS application configuration under decision 0006. They must be
available to the API process after publication. The local development release
uses its approved server-only values; no client value is present in frontend
settings.

### Optional local backchannel proxy

If an IIS application pool cannot reach Google's public signing-key endpoint
directly while the workstation uses an approved local proxy, set the optional
server-side `BackchannelProxyUri` to that proxy's HTTP or HTTPS URI. It is used
only by the Google OpenID Connect backchannel. The validator rejects embedded
proxy credentials; configure any required proxy authentication outside this
setting. Do not record the proxy address or credentials in this runbook.

## Publish and smoke-check

1. Publish the paired API and UI releases, retaining the prior versioned
   directories for rollback.
2. Confirm API readiness at
   `https://localhost/EosDashboardsApi/health/ready` and open only
   `https://localhost/EosDashboards/`.
3. Confirm the sign-in page shows `ورود با Google`.
4. With the already linked test account, select that action, finish Google
   authentication, and verify dashboard entry.
5. Refresh once, verify the existing session remains active, then log out.

Do not inspect or record cookies, tokens, codes, passwords, the client secret,
or personal account data. If the button is absent, leave Google disabled and
check only the server-side configuration and API startup logs for a safe
configuration error.

## Troubleshooting

- **`redirect_uri_mismatch`:** compare the registered value and API setting
  character-for-character against the callback shown above.
- **Google button absent:** `Enabled` is false, a required setting is blank, or
  the API did not start with its intended configuration.
- **Google action returns immediately to local sign-in:** confirm that the IIS
  API application-pool identity can retrieve the public signing-key endpoint
  above. A 403 or other outbound-network failure must be corrected in network
  policy or through the approved `BackchannelProxyUri`; never accept Google
  tokens without signature validation.
- **Returned to local sign-in with a generic error:** the account was
  cancelled, not Google-verified, not pre-linked, inactive, or the callback
  could not be validated. Do not reveal which case applies to a user.
