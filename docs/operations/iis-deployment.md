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

- Use **No Managed Code**, Integrated pipeline mode, and a dedicated identity.
- Install the matching ASP.NET Core Hosting Bundle.
- Grant the identity read/execute access to the API files, write access only to the configured key-ring directory, and the least SQL permission required.
- Keep development-only database, SMS, and authentication settings in the tracked API `appsettings.Development.json`, as approved for this private repository. They are server-side API artifact settings; never place them in browser-delivered frontend configuration or documentation.
- Enable Anonymous Authentication and disable Windows Authentication for the API application. Application endpoints enforce credential, OTP, JWT, refresh-cookie, origin/anti-forgery, rate-limit, and authorization boundaries.
- Health probes do not require an operating-system identity; successful `/health/live` and `/health/ready` responses return `200`.
- Keep the IIS application configuration when switching versioned release directories. The development API artifact carries its tracked `appsettings.Development.json` values.

## Local runtime conditions

- Serve both child applications through the `Default Web Site` HTTPS binding. The local UI origin configured for credentialed CORS is exactly `https://localhost`; paths do not form part of an origin.
- Keep UI and API in separate child applications and pools. The UI build must retain `VITE_PUBLIC_BASE=/EosDashboards/`; the API base remains `/EosDashboardsApi`.
- Keep the persistent Data Protection key ring outside the web root, at a path writable by the API pool identity and not by the UI pool. The installed local path is `C:\ProgramData\EosDashboards\keys`.
- The normal release publisher does not accept a private-data file and does not reconfigure the API or provision users. It copies inspected artifacts, including the tracked development API configuration, switches the versioned paths, and checks HTTPS readiness.

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
IIS custom 404 execution for SPA fallback instead of a Rewrite rule. Its
fallback target is `/EosDashboards/`, not the parent site root, so a refreshed
internal route remains in the UI application and pool.

## UI application

- Serve the static Vite output over HTTPS.
- Configure the API origin exactly and do not use wildcard credentialed CORS.

## Deployment and rollback

Stop or drain the target application pool, switch the site path to the inspected versioned directory, start it, then check `/health/live`, `/health/ready`, UI loading, one login, refresh, and logout. On failure, switch the path back to the previous version and preserve logs plus trace IDs.

Before switching an API release that contains EF Core migrations, take a
verified local database backup and list/apply migrations from the same Release
build configuration. Explicitly pass `--configuration Release` to the EF
tools: their default Debug output can be stale and omit a newly built Release
migration. The normal IIS publisher intentionally does not apply migrations,
so a pending migration must never be deferred until after the API switch.

The current local installation is described in `docs/project/current-state.md`.
Both UI loading, a synthetic refreshed UI route, and API liveness/readiness
must return HTTPS HTTP 200 after a deployment. The API uses anonymous IIS
access and reads its local settings from the tracked development API
configuration. Do not put such values in the published frontend artifact or in
documentation.

## Release-source verification

### Incident and cause

On 2026-09-03, a release that added linked Google sign-in was published from
an isolated feature worktree while the newer UI corrections existed only as
uncommitted changes in the main worktree. The published artifact therefore
contained the Google feature on an older application shell. IIS served the
artifact correctly; the error was selecting an incomplete source worktree for
the build.

### Required prevention procedure

Before building or publishing a local IIS release:

1. Identify the exact source worktree and branch that must contain every
   intended change. Never assume that a feature worktree includes uncommitted
   work from another worktree.
2. Inspect `git status --short` in the intended source worktree and every
   active worktree. If another worktree contains related UI, API, or
   configuration changes, integrate those changes into the release source
   first; do not publish the feature branch independently.
3. Commit the integrated release source before producing artifacts. A
   versioned commit is the release provenance record; do not publish an
   accidental mixture of one branch and another worktree's uncommitted files.
4. Build both API and UI from that same worktree, then inspect the generated
   UI artifact for the expected current title, branding, and changed feature.
   Do not reuse an artifact built before integration.
5. Run the focused automated checks for the integrated authentication and UI
   flows, then publish only those inspected artifacts.
6. After the IIS switch, smoke-test the actual HTTPS application: current UI
   appearance, ordinary sign-in, linked external sign-in when enabled,
   refresh of an authenticated session, logout, and a refreshed internal SPA
   route. If the displayed UI is not the expected version, roll back to the
   prior versioned artifact and stop further release work until source
   provenance is reconciled.

Record the source commit, artifact release identifier, operator, and smoke-test
outcome with the release. Do not record credentials, personal data, OAuth
secrets, tokens, or private endpoints.

### Remote push-protection lesson

On 2026-09-03, GitHub Push Protection correctly blocked the merged local
release because it detected the approved local server-side Google OAuth
configuration. The release was pushed only after the repository owner gave
action-time approval through GitHub for this private-repository exception.

If a source-control protection rule blocks a server-side local setting:

1. Do not mislabel a real credential as a false positive or a test value.
2. Stop the push and obtain explicit, contemporaneous owner approval for the
   exact repository and setting category before using a provider bypass.
3. Keep the setting server-side; never solve the block by moving it into
   frontend build settings, browser storage, documentation, or logs.
4. Record the protection event and approval outcome without copying the value.
   Reassess and rotate the affected credential when repository access or its
   exposure risk changes.

## Initial-machine provisioning only

`scripts/Configure-LocalIisFromPrivateData.ps1` remains available only for a
deprecated, deliberately requested first-machine provisioning or repair. It accepts a
developer-owned private text file outside this repository. It expects `Server`,
`User`, `Pass`, `DataBase`, and an HTTPS endpoint following `Sms Web Servise`.
It validates the file before use, generates independent local security keys,
applies the development migration and can provision the first administrator.
It is not used to set ordinary API runtime configuration. The file path and
every supplied value remain outside source control and are never printed by the
helper.

When explicitly requested by its `-ProvisionAdministratorFromPrivateData`
switch, the helper reads the labelled administrator username, password, first
name, last name, and mobile values from that same private file. It sends them
only through the provisioner's redirected standard input and does not echo
them. This is not part of an ordinary publication; the normal publisher must
not be given a private-data-file argument.
