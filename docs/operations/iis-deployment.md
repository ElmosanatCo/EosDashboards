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

## UI site

- Serve the static Vite output over HTTPS.
- Install IIS URL Rewrite for the included SPA `web.config`.
- Configure the API origin exactly and do not use wildcard credentialed CORS.

## Deployment and rollback

Stop or drain the target application pool, switch the site path to the inspected versioned directory, start it, then check `/health/live`, `/health/ready`, UI loading, one login, refresh, and logout. On failure, switch the path back to the previous version and preserve logs plus trace IDs.

Automated deployment was not performed during implementation because this process could not read or modify IIS configuration without an elevated operator session, and the exact target site names/paths were unavailable. An administrator must execute the local switch after confirming those targets.
