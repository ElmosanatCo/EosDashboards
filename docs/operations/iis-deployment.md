# Local development IIS deployment

The React UI and ASP.NET Core API must use separate IIS sites/applications and separate application pools. This document applies to the developer workstation only; company production deployment is out of scope.

## Publish

```powershell
dotnet publish backend/src/EosDashboards.Api/EosDashboards.Api.csproj -c Release -o <versioned-api-directory>
npm ci --prefix frontend
npm --prefix frontend run build
```

Copy `frontend/dist/` to a versioned UI directory. Point IIS to the versioned directories only after inspection; keep the previous directories for rollback.

## API application pool

- Use **No Managed Code** and a dedicated identity.
- Install the matching ASP.NET Core Hosting Bundle.
- Grant the identity read/execute access to the API files, write access only to the configured key-ring directory, and the least SQL permission required.
- Configure secrets and connection strings outside the artifact.
- Enable Windows Authentication for the challenge endpoint and disable anonymous access only if the final IIS topology can still serve JWT endpoints correctly. Validate this boundary with the organization's IIS policy before production.

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

## Local private-data helper

`scripts/Configure-LocalIisFromPrivateData.ps1` accepts the path to a
developer-owned private text file outside this repository. It expects `Server`,
`User`, `Pass`, `DataBase`, and an HTTPS endpoint following `Sms Web Servise`.
It validates the file before use, generates independent local security keys,
stores runtime settings in the API IIS application configuration, applies the
development migration, and interactively provisions the first administrator.
The file path and every supplied value remain outside source control and are
never printed by the helper.

When explicitly requested by its `-ProvisionAdministratorFromPrivateData`
switch, the helper takes the three administrator profile values placed after
`Method` in that same private file. It obtains the organizational stable ID and
account name from the current Windows identity, pipes the values only to the
deployment-only provisioner, and does not echo them. This is for the developer
workstation only; keep the private file access-restricted and delete it when it
is no longer needed.
