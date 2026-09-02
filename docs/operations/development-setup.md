# Development setup

## Prerequisites

- .NET SDK 10.0.400 or the compatible SDK selected by `global.json`.
- Node.js 24 and `npm ci` in `frontend/`.
- SQL Server or LocalDB for development and the isolated integration-test database.
- HTTPS endpoints for the React UI, API, and company SMS SOAP service.

## Configuration

Tracked JSON files contain shapes and non-secret defaults only. Supply these values through .NET user secrets for command-line development or environment variables/IIS configuration:

- `ConnectionStrings:EosDashboard`
- `ApiSecurity:AllowedOrigins:0` (the exact UI origin)
- `AuthSecurity:HashingKey` and `AuthSecurity:SigningKey` (independent base64 keys of at least 32 random bytes)
- `AuthSecurity:KeyRingPath` (persistent and writable only by the API identity)
- `Sms:Endpoint` (HTTPS)

Do not paste values into tracked files or terminal history. Enter them using the local secret manager or the IIS configuration editor. Never reuse test keys.

## Local commands

```powershell
dotnet restore backend/EosDashboards.sln --locked-mode
dotnet build backend/EosDashboards.sln -c Release --no-restore
npm ci --prefix frontend
npm --prefix frontend run dev
```

Run the API after its local secret store is complete. The API intentionally does not migrate the database on startup.

For database integration tests, set `ConnectionStrings__EosDashboardTests` only in the current process. The fixture rejects unsafe names and requires an isolated database whose name identifies it as a test database.
