# Local development IIS deployment

The React UI and ASP.NET Core API must use separate IIS sites/applications and separate application pools. This document applies to the developer workstation only; company production deployment is out of scope.

## Publish

```powershell
dotnet publish backend/src/EosDashboards.Api/EosDashboards.Api.csproj -c Release -o <versioned-api-directory>
npm ci --prefix frontend
npm --prefix frontend run build:iis
```

Copy `frontend/dist/` to a versioned UI directory. Point IIS to the versioned directories only after inspection; keep the previous directories for rollback.

## API application pool

- Use **No Managed Code**, Integrated pipeline mode, and a dedicated identity.
- Install the matching ASP.NET Core Hosting Bundle.
- Grant the identity read/execute access to the API files, write access only to the configured key-ring directory, and the least SQL permission required.
- Keep local server-only settings in the private repository's API configuration
  or the API IIS application configuration under decision 0006. Never place
  them in frontend build settings.
- Enable Anonymous Authentication and disable Windows Authentication for the API application. This prevents IIS from injecting the retired Negotiate handler into local credential, OTP, and Google sign-in flows.
- Successful `/health/live` and `/health/ready` responses return `200` without Windows credentials.
- Keep the API application configuration when switching versioned release directories. It contains the environment-specific values outside the artifact.

## Local runtime conditions

- Serve both child applications through the `Default Web Site` HTTPS binding. The local UI origin configured for credentialed CORS is exactly `https://localhost`; paths do not form part of an origin.
- Keep UI and API in separate child applications and pools. The UI build must retain `VITE_PUBLIC_BASE=/EosDashboards/`; the API base remains `/EosDashboardsApi`.
- The fixed home workspace tab must preserve `/EosDashboards/` as its browser path. A successful sign-in followed by refresh must never navigate to the IIS site root, which serves the default IIS page.
- Keep the persistent Data Protection key ring outside the web root, at a path writable by the API pool identity and not by the UI pool. The installed local path is `C:\ProgramData\EosDashboards\keys`.
- The normal local publisher reuses the existing IIS runtime configuration and
  does not require a private-data file. It must not move server credentials or
  identity-provider secrets into frontend settings or terminal history.

## Local installed applications

The current local installation uses separate applications beneath `Default Web Site`:

- UI: `/EosDashboards`, application pool `EosDashboardsUiPool`, deployed under
  `C:\inetpub\wwwroot\EosDashboards\Ui\releases\`.
- API: `/EosDashboardsApi`, application pool `EosDashboardsApiPool`, deployed
  under `C:\inetpub\wwwroot\EosDashboards\Api\releases\`.

The UI build must use `VITE_PUBLIC_BASE=/EosDashboards/` and
`VITE_API_BASE_URL=/EosDashboardsApi`. Keep the pools separate and switch only
the appropriate application physical path during a versioned update.

The IIS URL Rewrite module is not installed on this workstation. The UI uses
IIS custom 404 execution for SPA fallback instead of a Rewrite rule.

## UI application

- Serve the static Vite output over HTTPS.
- Configure the API origin exactly and do not use wildcard credentialed CORS.

## Deployment and rollback

Stop or drain the target application pool, switch the site path to the inspected versioned directory, start it, then check `/health/live`, `/health/ready`, UI loading, one login, refresh, and logout. On failure, switch the path back to the previous version and preserve logs plus trace IDs.

The UI release `20260902-202534` and paired API release `20260902-202354`
were installed on 2026-09-02. Both UI loading and API liveness/readiness
returned HTTPS HTTP 200 using the local Windows identity. The API has Windows
Authentication enabled and anonymous access disabled. Its required runtime
configuration remains outside the artifact; do not record those values in this
repository or its published artifact.

## Legacy private-data helper

`scripts/Configure-LocalIisFromPrivateData.ps1` is a legacy recovery and
first-provisioning helper. It accepts the path to a
developer-owned private text file outside this repository. It expects `Server`,
`User`, `Pass`, `DataBase`, and an HTTPS endpoint following `Sms Web Servise`.
It validates the file before use, generates independent local security keys,
stores runtime settings in the API IIS application configuration, applies the
development migration, and provisions the first administrator. The file path
and every supplied value remain outside source control and are never printed by
the helper.

Do not use this helper for an ordinary paired release. When explicitly
requested by its `-ProvisionAdministratorFromPrivateData`
switch, the helper takes the three administrator profile values placed after
`Method` in that same private file. It obtains the organizational stable ID and
account name from the current Windows identity, sends the values only through
the provisioner's redirected standard input, and does not echo them. This is
for the developer workstation only; keep the private file access-restricted and
delete it when it is no longer needed.
