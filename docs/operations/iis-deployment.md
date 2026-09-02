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

The UI release `20260902-202534` was installed and returned HTTP 200 on
2026-09-02. The paired API release `20260902-202354` is installed but cannot
start until its required local configuration is provided. Do not record those
values in this repository or its published artifact.
