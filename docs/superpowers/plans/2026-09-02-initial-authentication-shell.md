# Initial Authentication and Tabbed Application Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first production-shaped EosDashboards vertical slice with separate backend/frontend foundations, controlled administrator provisioning, Windows organizational sign-in, SMS OTP, secure sessions, and the Persian RTL tabbed shell.

**Architecture:** A .NET 10 lightweight clean backend keeps Domain and Application independent while Infrastructure owns SQL Server, cryptography, Windows identity adaptation, and SOAP SMS access. A separate React 19 SPA holds access tokens only in memory, uses Secure HttpOnly refresh cookies, and renders an accessible Material UI RTL workspace whose route-aware tabs preserve only serializable state.

**Tech Stack:** .NET 10 LTS, ASP.NET Core 10, EF Core 10, SQL Server, xUnit; React 19.2, TypeScript, Vite, Material UI 9, TanStack Query, React Router, Vitest, Testing Library, Playwright; IIS production hosting.

**Spec:** `docs/superpowers/specs/2026-09-02-initial-authentication-shell-design.md`

## Global Constraints

- Keep `backend/` independently openable in Visual Studio and `frontend/` independently openable in VS Code.
- Execute implementation in an isolated worktree on branch `feature/initial-authentication-shell`; keep the primary `main` checkout clean until final verified integration.
- Preserve dependency direction API -> Application -> Domain; only Infrastructure accesses EF Core, SQL Server, Windows identity details, cryptography storage, or SMS.
- Use auto-incrementing SQL Server `bigint` primary keys named `Id` for all principal tables; `UserRoles` uses the approved composite key.
- Use database name `EosDashboard` for development and a dedicated name ending in `_IntegrationTests` for destructive integration tests.
- Never commit database hosts, usernames, passwords, administrator personal data, mobile numbers, signing/hashing/encryption keys, or environment-specific SMS values.
- Use Windows/AD identity plus a six-digit OTP valid for five minutes, five attempts, and a 60-second resend cooldown.
- Use ten-minute in-memory JWT access tokens and one absolute eight-hour revocable application session.
- Never send a real SMS from automated tests or retry an ambiguous SOAP timeout automatically.
- Use Persian RTL, Vazirmatn, Material UI, navy/teal default palette, light/dark/system modes, WCAG 2.2 AA, and the approved fixed shell.
- Use internal route-aware tabs: fixed home, logical deduplication, parameter-aware keys, dirty-close protection, session restoration, logout clearing, and active-page-only mounting.
- Update canonical and formal documentation before every push. Every local merge must be verified and pushed immediately.

---

## File Map

### Repository foundation

- `global.json`: select .NET 10 with safe feature-band roll-forward.
- `.editorconfig`: shared C# and text formatting rules.
- `backend/Directory.Build.props`: nullable, analyzers, warnings, deterministic builds, lock-file restore.
- `backend/Directory.Packages.props`: central backend dependency versions.
- `backend/EosDashboards.sln`: Visual Studio entry point.
- `frontend/package.json`, `frontend/package-lock.json`: independent frontend scripts and locked dependencies.
- `frontend/.nvmrc`: Node.js 24 line.

### Backend production projects

- `backend/src/EosDashboards.Domain/Entities/*.cs`: user, role, OTP, session, preferences, audit state.
- `backend/src/EosDashboards.Domain/Enums/*.cs`: explicit OTP/session/audit states.
- `backend/src/EosDashboards.Application/Abstractions/*.cs`: persistence, clock, SMS, protection, hashing, and token ports.
- `backend/src/EosDashboards.Application/Auth/*.cs`: start, verify, refresh, logout, current-user use cases.
- `backend/src/EosDashboards.Application/Provisioning/*.cs`: idempotent administrator provisioning use case.
- `backend/src/EosDashboards.Application/Preferences/*.cs`: current-user preference read/update.
- `backend/src/EosDashboards.Infrastructure/Persistence/*.cs`: EF context, mappings, repositories, migrations.
- `backend/src/EosDashboards.Infrastructure/Security/*.cs`: OTP/random tokens, HMAC hashing, JWT, mobile protection.
- `backend/src/EosDashboards.Infrastructure/Sms/*.cs`: SOAP SMS adapter and typed options.
- `backend/src/EosDashboards.Infrastructure/DependencyInjection.cs`: Infrastructure registration.
- `backend/src/EosDashboards.Api/Auth/*.cs`: API request/response contracts and endpoints.
- `backend/src/EosDashboards.Api/Preferences/*.cs`: preference contracts/endpoints.
- `backend/src/EosDashboards.Api/Security/*.cs`: Windows identity mapping, cookies, origin/anti-forgery checks.
- `backend/src/EosDashboards.Api/Errors/*.cs`: central problem-details mapping.
- `backend/src/EosDashboards.Api/Program.cs`: composition, schemes, CORS, rate limits, health, OpenAPI.
- `backend/tools/EosDashboards.AdminProvisioner/Program.cs`: secure interactive provisioning entry point.

### Frontend production files

- `frontend/src/app/providers/*`: theme, query, auth, router, error boundary.
- `frontend/src/features/auth/*`: organizational sign-in and OTP experience.
- `frontend/src/features/preferences/*`: server-backed appearance/menu settings.
- `frontend/src/layout/*`: fixed shell, header, sidebar, status bar, tab strip.
- `frontend/src/navigation/*`: typed tab descriptors, reducer/store, route registry, dirty-state guard.
- `frontend/src/lib/api/*`: credentialed fetch client, memory token handling, refresh serialization.
- `frontend/src/lib/date/*`: Persian date and Tehran clock formatting.
- `frontend/scripts/sync-resources.mjs`: copy approved root resources into generated public assets.
- `frontend/vite.config.ts`: version injection, resource sync, development API configuration.

### Test projects

- `backend/tests/EosDashboards.ArchitectureTests/*`: layer-reference rules.
- `backend/tests/EosDashboards.Domain.Tests/*`: entity invariants.
- `backend/tests/EosDashboards.Application.Tests/*`: use cases with handwritten fakes.
- `backend/tests/EosDashboards.IntegrationTests/*`: SQL Server migrations/repositories and in-memory HTTP host behavior.
- `frontend/src/**/*.test.tsx`: component and store tests adjacent to features.
- `frontend/tests/e2e/auth-shell.spec.ts`: fake-auth/fake-SMS browser flow.

---

### Task 1: Prepare supported toolchains, isolated worktree, and independent applications

**Files:**
- Create: `global.json`
- Create: `.editorconfig`
- Create: `backend/Directory.Build.props`
- Create: `backend/Directory.Packages.props`
- Create: `backend/EosDashboards.sln`
- Create: backend project files under `backend/src`, `backend/tools`, and `backend/tests`
- Create: `frontend/.nvmrc`
- Create: initial Vite application under `frontend/`
- Modify: `.gitignore`

**Interfaces:**
- Produces: buildable `backend/EosDashboards.sln` and independently buildable `frontend/package.json`.
- Produces: Domain <- Application <- Infrastructure/API project-reference graph used by all later tasks.

- [ ] **Step 1: Verify/install supported SDKs**

Run:

```powershell
dotnet --list-sdks
node --version
winget install --id Microsoft.DotNet.SDK.10 --exact --source winget
winget install --id OpenJS.NodeJS.LTS --exact --source winget
```

Expected: a .NET 10 SDK and Node.js 24 LTS are available. Restart the terminal if the PATH does not refresh, then verify `dotnet --version` begins with `10.` and `node --version` begins with `v24.`.

- [ ] **Step 2: Create the isolated implementation worktree**

Invoke `superpowers:using-git-worktrees`, create branch `feature/initial-authentication-shell` from the current synchronized `main`, verify the worktree starts clean, and run every implementation command below inside that worktree until Task 13's integration step.

- [ ] **Step 3: Create the backend solution and projects**

Run:

```powershell
dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature
New-Item -ItemType Directory -Force backend/src,backend/tools,backend/tests | Out-Null
dotnet new sln -n EosDashboards -o backend
dotnet new classlib -n EosDashboards.Domain -o backend/src/EosDashboards.Domain -f net10.0
dotnet new classlib -n EosDashboards.Application -o backend/src/EosDashboards.Application -f net10.0
dotnet new classlib -n EosDashboards.Infrastructure -o backend/src/EosDashboards.Infrastructure -f net10.0
dotnet new webapi -n EosDashboards.Api -o backend/src/EosDashboards.Api -f net10.0 --use-controllers false
dotnet new console -n EosDashboards.AdminProvisioner -o backend/tools/EosDashboards.AdminProvisioner -f net10.0
dotnet new xunit -n EosDashboards.ArchitectureTests -o backend/tests/EosDashboards.ArchitectureTests -f net10.0
dotnet new xunit -n EosDashboards.Domain.Tests -o backend/tests/EosDashboards.Domain.Tests -f net10.0
dotnet new xunit -n EosDashboards.Application.Tests -o backend/tests/EosDashboards.Application.Tests -f net10.0
dotnet new xunit -n EosDashboards.IntegrationTests -o backend/tests/EosDashboards.IntegrationTests -f net10.0
dotnet sln backend/EosDashboards.sln add (Get-ChildItem backend -Recurse -Filter *.csproj | ForEach-Object FullName)
dotnet add backend/src/EosDashboards.Application reference backend/src/EosDashboards.Domain
dotnet add backend/src/EosDashboards.Infrastructure reference backend/src/EosDashboards.Domain backend/src/EosDashboards.Application
dotnet add backend/src/EosDashboards.Api reference backend/src/EosDashboards.Application backend/src/EosDashboards.Infrastructure
dotnet add backend/tools/EosDashboards.AdminProvisioner reference backend/src/EosDashboards.Application backend/src/EosDashboards.Infrastructure
dotnet add backend/tests/EosDashboards.Domain.Tests reference backend/src/EosDashboards.Domain
dotnet add backend/tests/EosDashboards.Application.Tests reference backend/src/EosDashboards.Application backend/src/EosDashboards.Domain
dotnet add backend/tests/EosDashboards.IntegrationTests reference backend/src/EosDashboards.Api backend/src/EosDashboards.Infrastructure
```

Expected: solution restore succeeds with only the intended references.

- [ ] **Step 4: Write the failing architecture test**

Create `backend/tests/EosDashboards.ArchitectureTests/LayerDependencyTests.cs` that loads project assemblies and asserts:

```csharp
[Fact]
public void Domain_and_application_do_not_reference_outer_layers()
{
    Assert.DoesNotContain("EosDashboards.Infrastructure", ReferencedBy("EosDashboards.Domain"));
    Assert.DoesNotContain("EosDashboards.Api", ReferencedBy("EosDashboards.Domain"));
    Assert.DoesNotContain("EosDashboards.Infrastructure", ReferencedBy("EosDashboards.Application"));
    Assert.DoesNotContain("EosDashboards.Api", ReferencedBy("EosDashboards.Application"));
}
```

Run `dotnet test backend/tests/EosDashboards.ArchitectureTests` and expect failure until `ReferencedBy` resolves the two assemblies and returns their referenced assembly names.

- [ ] **Step 5: Implement architecture-test loading and shared build rules**

Implement `ReferencedBy` with `Assembly.Load(assemblyName).GetReferencedAssemblies().Select(x => x.Name!)`. Add project references from ArchitectureTests to Domain and Application so assemblies copy to output. Configure `Directory.Build.props` with nullable enabled, implicit usings enabled, deterministic builds, `TreatWarningsAsErrors`, .NET analyzers, and NuGet lock-file generation. Add `.editorconfig` rules for UTF-8, final newline, four-space C# indentation, and two-space JSON/TypeScript indentation.

- [ ] **Step 6: Scaffold and lock the frontend**

Run:

```powershell
npm create vite@latest frontend -- --template react-ts
Set-Content frontend/.nvmrc '24'
Set-Location frontend
npm install
npm install @mui/material@^9 @mui/icons-material@^9 @emotion/react @emotion/styled @emotion/cache stylis stylis-plugin-rtl @tanstack/react-query react-router-dom
npm install -D vitest jsdom @testing-library/react @testing-library/jest-dom @testing-library/user-event @playwright/test eslint prettier
npm pkg set scripts.test="vitest run" scripts.test:watch="vitest" scripts.typecheck="tsc --noEmit" scripts.format:check="prettier --check ." scripts.e2e="playwright test"
Set-Location ..
```

Expected: `package-lock.json` exists and `npm --prefix frontend run build` succeeds.

- [ ] **Step 7: Verify and commit foundation**

Run:

```powershell
dotnet test backend/EosDashboards.sln
npm --prefix frontend run typecheck
npm --prefix frontend test
npm --prefix frontend run build
git add global.json .editorconfig .gitignore backend frontend
git commit -m "build: scaffold backend and frontend foundations"
```

Expected: all commands exit successfully and the commit contains no generated secrets or build output.

---

### Task 2: Implement domain authentication and preference state

**Files:**
- Create: `backend/src/EosDashboards.Domain/Entities/User.cs`
- Create: `backend/src/EosDashboards.Domain/Entities/Role.cs`
- Create: `backend/src/EosDashboards.Domain/Entities/UserRole.cs`
- Create: `backend/src/EosDashboards.Domain/Entities/OtpChallenge.cs`
- Create: `backend/src/EosDashboards.Domain/Entities/UserSession.cs`
- Create: `backend/src/EosDashboards.Domain/Entities/UserPreference.cs`
- Create: `backend/src/EosDashboards.Domain/Entities/AuditLog.cs`
- Create: `backend/src/EosDashboards.Domain/Enums/OtpChallengeStatus.cs`
- Create: `backend/src/EosDashboards.Domain/Enums/SessionRevocationReason.cs`
- Test: `backend/tests/EosDashboards.Domain.Tests/OtpChallengeTests.cs`
- Test: `backend/tests/EosDashboards.Domain.Tests/UserSessionTests.cs`
- Test: `backend/tests/EosDashboards.Domain.Tests/UserTests.cs`

**Interfaces:**
- Produces: `OtpChallenge.Create`, `OtpChallenge.Verify`, `OtpChallenge.MarkSent`, `OtpChallenge.MarkSendFailed`, `OtpChallenge.Supersede`.
- Produces: `UserSession.Create`, `UserSession.Rotate`, `UserSession.Revoke`, `UserSession.IsActive`.
- Produces: `User.AssignRole`, `User.UpdateProfile`, `User.Deactivate`.

- [ ] **Step 1: Write failing OTP lifecycle tests**

Cover exact boundaries:

```csharp
[Fact]
public void Fifth_wrong_code_exhausts_challenge()
{
    var challenge = OtpChallenge.Create(1, "public-token", "stored-hash", Now, Now.AddMinutes(5));
    challenge.MarkSent();
    for (var attempt = 1; attempt <= 5; attempt++)
        Assert.False(challenge.Verify("wrong-hash", Now.AddMinutes(1)));
    Assert.Equal(OtpChallengeStatus.Exhausted, challenge.Status);
}
```

Also test success consumes once, exact expiry rejects, superseded/send-failed challenges reject, and codes/hashes are never exposed by `ToString`.

- [ ] **Step 2: Run tests and confirm failure**

Run `dotnet test backend/tests/EosDashboards.Domain.Tests --filter FullyQualifiedName~OtpChallengeTests`.

Expected: compile failure because domain types do not exist.

- [ ] **Step 3: Implement minimal entities and enums**

Use private setters, collection encapsulation, UTC `DateTimeOffset`, and constructor/factory validation. `Verify` compares already-computed hashes with `CryptographicOperations.FixedTimeEquals(Convert.FromHexString(...))`, increments only valid active attempts, and atomically transitions to `Consumed`, `Expired`, or `Exhausted`.

- [ ] **Step 4: Add failing user/session tests and implement**

Test role assignment idempotency, stable organizational ID requirement, active-user behavior, eight-hour absolute expiry, refresh rotation replacement, and revocation idempotency. Implement only the methods named in Interfaces. Keep persistence annotations out of Domain.

- [ ] **Step 5: Verify and commit**

Run:

```powershell
dotnet test backend/tests/EosDashboards.Domain.Tests
dotnet test backend/tests/EosDashboards.ArchitectureTests
git add backend/src/EosDashboards.Domain backend/tests/EosDashboards.Domain.Tests
git commit -m "feat: add authentication domain model"
```

---

### Task 3: Define application contracts and authentication use cases

**Files:**
- Create: `backend/src/EosDashboards.Application/Abstractions/IClock.cs`
- Create: `backend/src/EosDashboards.Application/Abstractions/IUserRepository.cs`
- Create: `backend/src/EosDashboards.Application/Abstractions/IOtpChallengeRepository.cs`
- Create: `backend/src/EosDashboards.Application/Abstractions/IUserSessionRepository.cs`
- Create: `backend/src/EosDashboards.Application/Abstractions/IAuditWriter.cs`
- Create: `backend/src/EosDashboards.Application/Abstractions/ISmsSender.cs`
- Create: `backend/src/EosDashboards.Application/Abstractions/ISecretHasher.cs`
- Create: `backend/src/EosDashboards.Application/Abstractions/ISecureTokenGenerator.cs`
- Create: `backend/src/EosDashboards.Application/Abstractions/IAccessTokenIssuer.cs`
- Create: `backend/src/EosDashboards.Application/Abstractions/IMobileProtector.cs`
- Create: `backend/src/EosDashboards.Application/Abstractions/IRoleRepository.cs`
- Create: `backend/src/EosDashboards.Application/Abstractions/IUserPreferenceRepository.cs`
- Create: `backend/src/EosDashboards.Application/Abstractions/IUnitOfWork.cs`
- Create: `backend/src/EosDashboards.Application/Auth/AuthContracts.cs`
- Create: `backend/src/EosDashboards.Application/Auth/StartSignIn.cs`
- Create: `backend/src/EosDashboards.Application/Auth/VerifyOtp.cs`
- Create: `backend/src/EosDashboards.Application/Auth/RefreshSession.cs`
- Create: `backend/src/EosDashboards.Application/Auth/Logout.cs`
- Test: `backend/tests/EosDashboards.Application.Tests/Auth/*.cs`

**Interfaces:**
- Consumes: Domain entities from Task 2.
- Produces: `StartSignIn.HandleAsync(StartSignInCommand, CancellationToken)` returning masked mobile, public challenge token, expiry, resend time.
- Produces: `VerifyOtp.HandleAsync(VerifyOtpCommand, CancellationToken)` returning access token, refresh credential, user projection, and expiries.
- Produces: `RefreshSession.HandleAsync(RefreshSessionCommand, CancellationToken)` and `Logout.HandleAsync(LogoutCommand, CancellationToken)`.

- [ ] **Step 1: Define exact records and ports**

Add these public records before writing implementations:

```csharp
public sealed record OrganizationalIdentity(string StableId, string AccountName);
public sealed record StartSignInCommand(OrganizationalIdentity Identity, string? NetworkKey);
public sealed record VerifyOtpCommand(string ChallengeToken, string Code, string? NetworkKey);
public sealed record RefreshSessionCommand(string RefreshCredential);
public sealed record LogoutCommand(long SessionId);
public sealed record SmsMessage(string Mobile, string Text);
public sealed record SmsSendResult(bool Succeeded, string? SafeErrorCode);
public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAtUtc);
```

Repository methods use `Task<T?>`, accept `CancellationToken`, and never return `IQueryable` outside Infrastructure.

Define the neighboring contracts exactly as:

```csharp
public interface IClock { DateTimeOffset UtcNow { get; } }
public interface IUserRepository
{
    Task<User?> FindByOrganizationalIdAsync(string stableId, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken);
    void Add(User user);
}
public interface IRoleRepository
{
    Task<Role?> FindByCodeAsync(string code, CancellationToken cancellationToken);
    void Add(Role role);
}
public interface IOtpChallengeRepository
{
    Task<OtpChallenge?> FindByPublicTokenAsync(string token, CancellationToken cancellationToken);
    Task<OtpChallenge?> FindLatestActiveAsync(long userId, CancellationToken cancellationToken);
    void Add(OtpChallenge challenge);
}
public interface IUserSessionRepository
{
    Task<UserSession?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<UserSession?> FindByRefreshHashAsync(string refreshHash, CancellationToken cancellationToken);
    void Add(UserSession session);
}
public interface IUserPreferenceRepository
{
    Task<UserPreference?> FindByUserIdAsync(long userId, CancellationToken cancellationToken);
    void Add(UserPreference preference);
}
public interface ISmsSender
{
    Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken);
}
public interface ISecretHasher
{
    string Hash(string value);
    bool Verify(string value, string expectedHash);
}
public interface ISecureTokenGenerator
{
    string CreateSixDigitCode();
    string CreateOpaqueToken(int byteCount);
}
public interface IMobileProtector
{
    string Protect(string normalizedMobile);
    string Unprotect(string protectedMobile);
    string Mask(string normalizedMobile);
}
public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(User user, long sessionId, DateTimeOffset issuedAtUtc);
}
public interface IAuditWriter
{
    Task WriteAsync(AuditRecord record, CancellationToken cancellationToken);
}
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
public sealed record AuditRecord(
    long? ActorUserId,
    long? SubjectUserId,
    string EventCode,
    bool Succeeded,
    string TraceId,
    IReadOnlyDictionary<string, string>? SafeMetadata);
```

- [ ] **Step 2: Write failing start-sign-in tests**

Use handwritten in-memory fakes. Test known active user sends one SMS and persists an active sent challenge; unknown/inactive user returns the same public denial; SMS false/timeout records failure and returns dependency-unavailable; a recent challenge enforces the 60-second cooldown.

Run `dotnet test backend/tests/EosDashboards.Application.Tests --filter FullyQualifiedName~StartSignInTests` and expect compile failure.

- [ ] **Step 3: Implement StartSignIn**

Generate code through `ISecureTokenGenerator.CreateSixDigitCode()`, public token through `CreateOpaqueToken(32)`, and hash through `ISecretHasher.Hash`. Use the decrypted mobile only inside the SMS port call, persist/send/audit in explicit stages, and never include complete mobile or code in an outcome or loggable exception.

- [ ] **Step 4: Write failing verification/session tests**

Cover success, wrong code, exact expiry, five-attempt exhaustion, consumed challenge, refresh rotation, eight-hour absolute expiry, revoked session, and logout idempotency. Assert no session is created before successful OTP consumption and the same transaction saves challenge consumption plus session creation.

- [ ] **Step 5: Implement verify, refresh, and logout use cases**

Issue a ten-minute access token. Create an eight-hour session whose refresh credential is returned once and persisted only as its keyed hash. Refresh rotates the raw credential and hash but never extends absolute expiry. Logout revokes the session with `UserLogout`.

- [ ] **Step 6: Verify and commit**

Run all Application and Architecture tests, then commit:

```powershell
git add backend/src/EosDashboards.Application backend/tests/EosDashboards.Application.Tests
git commit -m "feat: add authentication application use cases"
```

---

### Task 4: Add EF Core persistence and the initial migration

**Files:**
- Create: `backend/src/EosDashboards.Infrastructure/Persistence/EosDashboardDbContext.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Persistence/Configurations/*.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/*.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Persistence/EfUnitOfWork.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Persistence/Migrations/*`
- Create: `backend/tests/EosDashboards.IntegrationTests/Database/SqlServerDatabaseFixture.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Database/ModelMappingTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Database/RepositoryTests.cs`

**Interfaces:**
- Consumes: Domain entities and Application repository/unit-of-work ports.
- Produces: SQL Server schema and concrete repositories registered by `AddInfrastructure`.

- [ ] **Step 1: Add locked EF dependencies**

Add EF Core SQL Server, Design, and Tools packages on the approved 10.0 release line. Enable central package management in `Directory.Packages.props`, restore, and commit generated `packages.lock.json` files.

- [ ] **Step 2: Write failing SQL Server safety and mapping tests**

The fixture reads `ConnectionStrings:EosDashboardTests`, parses with `SqlConnectionStringBuilder`, and throws unless `InitialCatalog.EndsWith("_IntegrationTests", StringComparison.OrdinalIgnoreCase)`. It may call `EnsureDeletedAsync` only after that guard, then `MigrateAsync`.

Assert every principal entity key maps to `bigint` identity, organizational ID and role code are unique, `UserRoles` has the composite key, mobile ciphertext sizes are bounded, public challenge token and refresh hash are unique, and OTP/session concurrency tokens exist.

- [ ] **Step 3: Run tests and confirm safe failure**

Run the mapping test with the isolated test connection configured through user secrets. Expected first failure: missing context/migration, never deletion of `EosDashboard`.

- [ ] **Step 4: Implement context, mappings, and repositories**

Use separate `IEntityTypeConfiguration<T>` classes. Add UTC audit columns, filtered/unique indexes required by lookups, SQL `rowversion` on OTP challenges and sessions, no lazy-loading proxies, no generic repository, and explicit projection methods for current-user/profile reads.

- [ ] **Step 5: Create and inspect migration**

Run:

```powershell
dotnet ef migrations add InitialIdentity --project backend/src/EosDashboards.Infrastructure --startup-project backend/src/EosDashboards.Api --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project backend/src/EosDashboards.Infrastructure --startup-project backend/src/EosDashboards.Api --output backend/artifacts/sql/InitialIdentity.sql
```

Inspect the migration and script for seven approved tables, `bigint IDENTITY` keys, composite `UserRoles`, indexes, foreign keys, and absence of personal seed data.

- [ ] **Step 6: Run integration tests and commit**

Run Database integration tests twice to prove repeatable setup, then commit EF code, migration, reviewed script, and tests.

```powershell
git commit -m "feat: add initial identity persistence"
```

---

### Task 5: Implement protected values, secure tokens, and JWT issuance

**Files:**
- Create: `backend/src/EosDashboards.Infrastructure/Security/SystemClock.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Security/HmacSecretHasher.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Security/SecureTokenGenerator.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Security/DataProtectionMobileProtector.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Security/JwtAccessTokenIssuer.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Security/AuthSecurityOptions.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Security/SecurityPrimitiveTests.cs`

**Interfaces:**
- Consumes: Application security ports.
- Produces: cryptographically secure six-digit codes, opaque tokens, deterministic keyed hashes, protected mobile values, and ten-minute JWTs containing subject/session/role claims.

- [ ] **Step 1: Write failing security tests**

Generate 10,000 codes and assert all match `^[0-9]{6}$`; assert opaque tokens contain at least 32 random bytes; same keyed input hashes identically and different input does not; mobile protect/unprotect round-trips while ciphertext excludes plaintext; JWT validates approved issuer/audience/signature/lifetime and contains `sub`, `sid`, and role claims.

- [ ] **Step 2: Implement secure primitives**

Use `RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture)`, `RandomNumberGenerator.GetBytes`, Base64Url encoding, `HMACSHA256`, `CryptographicOperations.FixedTimeEquals`, ASP.NET Core Data Protection with purpose `EosDashboards.Mobile.v1`, and `JwtSecurityTokenHandler`/approved token APIs with `ClockSkew = TimeSpan.Zero`.

- [ ] **Step 3: Validate options at startup**

Require Base64-decoded hashing/signing keys of at least 32 bytes, nonempty issuer/audience, ten-minute access lifetime, eight-hour session lifetime, and a writable protected key-ring path. Fail startup with option names but never values.

- [ ] **Step 4: Verify and commit**

Run SecurityPrimitiveTests and the full backend test suite, then commit with `feat: add authentication security primitives`.

---

### Task 6: Implement the replaceable SOAP SMS adapter

**Files:**
- Create: `backend/src/EosDashboards.Infrastructure/Sms/SmsOptions.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Sms/SoapSmsSender.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Sms/SoapSmsSenderTests.cs`

**Interfaces:**
- Consumes: `ISmsSender.SendAsync(SmsMessage, CancellationToken)`.
- Produces: SOAP 1.1 call to configured `SendSmsMessage(message, mobile)` and maps boolean result to `SmsSendResult`.

- [ ] **Step 1: Write failing adapter tests with a recording HttpMessageHandler**

Assert POST method, `text/xml; charset=utf-8`, SOAPAction `http://tempuri.org/SendSmsMessage`, XML-escaped message/mobile elements, successful boolean parsing, false mapping, malformed XML mapping, timeout mapping, cancellation propagation, and exactly one HTTP attempt.

- [ ] **Step 2: Implement the SOAP request safely**

Build XML with `XmlWriter`, never string concatenation. Parse with secure `XmlReaderSettings` where DTD processing is prohibited and locate `SendSmsMessageResult` by local name. Use one named `HttpClient` with configured absolute HTTPS endpoint and timeout. Do not log body content or retry.

- [ ] **Step 3: Verify and commit**

Run the SMS adapter tests plus all backend tests, inspect test output for absence of phone/message values, and commit `feat: add company sms adapter`.

---

### Task 7: Build the deployment-only administrator provisioner

**Files:**
- Create: `backend/src/EosDashboards.Application/Provisioning/ProvisionSystemAdministrator.cs`
- Create: `backend/tools/EosDashboards.AdminProvisioner/InteractiveInput.cs`
- Modify: `backend/tools/EosDashboards.AdminProvisioner/Program.cs`
- Test: `backend/tests/EosDashboards.Application.Tests/Provisioning/ProvisionSystemAdministratorTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Provisioning/ProvisionerTests.cs`

**Interfaces:**
- Consumes: secure interactive organizational ID, account name, first name, last name, mobile, and configured connection/protection values.
- Produces: idempotently active user + `SystemAdministrator` role assignment and safe masked result.

- [ ] **Step 1: Write failing idempotency tests**

Call the use case twice with the same stable organizational ID and assert one user, one role, one assignment, updated profile values, encrypted mobile, two safe audit events, and no raw input in returned text.

- [ ] **Step 2: Implement provisioning use case**

Normalize organizational ID/account case, validate nonempty names and Iranian mobile shape, create the system role with Persian display name `مدیر سامانه`, protect mobile before persistence, and assign the role idempotently in one transaction.

- [ ] **Step 3: Implement secure interactive console behavior**

Read personal values interactively rather than command-line arguments, show masked confirmation, require an explicit Persian/English confirmation response, and never echo the complete mobile after entry. Read connection/protection secrets from user secrets or environment configuration shared with Infrastructure.

- [ ] **Step 4: Run against the isolated integration database**

Run the provisioner twice against the `_IntegrationTests` database with synthetic data and query through the repository to prove idempotency and role assignment. Do not use the real administrator details in tests.

- [ ] **Step 5: Verify and commit**

Run all provisioning and backend tests, then commit `feat: add system administrator provisioner`.

---

### Task 8: Expose secure API authentication, preferences, errors, and health

**Files:**
- Modify: `backend/src/EosDashboards.Api/Program.cs`
- Create: `backend/src/EosDashboards.Api/Auth/AuthEndpoints.cs`
- Create: `backend/src/EosDashboards.Api/Auth/AuthContracts.cs`
- Create: `backend/src/EosDashboards.Api/Preferences/PreferenceEndpoints.cs`
- Create: `backend/src/EosDashboards.Api/Security/WindowsIdentityReader.cs`
- Create: `backend/src/EosDashboards.Api/Security/RefreshCookieService.cs`
- Create: `backend/src/EosDashboards.Api/Security/TrustedOriginFilter.cs`
- Create: `backend/src/EosDashboards.Api/Errors/ExceptionHandler.cs`
- Create: `backend/src/EosDashboards.Api/appsettings.json`
- Create: `backend/src/EosDashboards.Api/appsettings.Development.json`
- Test: `backend/tests/EosDashboards.IntegrationTests/Api/AuthEndpointTests.cs`

**Interfaces:**
- Consumes: Task 3 use cases, Negotiate identity, typed options, repositories.
- Produces: approved `/api/v1/auth/*`, `/api/v1/users/me/preferences`, `/health/live`, `/health/ready`, and OpenAPI endpoints.

- [ ] **Step 1: Write failing endpoint tests**

Use `WebApplicationFactory<Program>` with replaceable test Windows authentication and fake SMS. Cover exact status/problem codes for known/unknown identity, challenge creation, invalid/expired OTP, success cookies/token, authorized `me`, refresh rotation, origin rejection, logout, preference round-trip, liveness, and database-aware readiness.

- [ ] **Step 2: Configure authentication and authorization schemes**

Register Negotiate only for challenge creation and JWT bearer as the normal protected scheme. Define policies `ActiveUser` and `SystemAdministrator`. Configure exact-origin credentialed CORS, endpoint rate-limit partitions by stable user/network, HTTPS/HSTS outside development, and no schema migration at startup.

- [ ] **Step 3: Implement endpoint contracts and cookies**

Return access token only in JSON. Set refresh cookie HttpOnly/Secure/SameSite Strict with an eight-hour maximum and a separate readable anti-forgery token cookie; require matching header and allowed Origin on refresh/logout. Expire both cookies on logout or failed refresh. Mark response and auth routes `Cache-Control: no-store`.

- [ ] **Step 4: Add problem details, audit context, OpenAPI, and health**

Map known outcomes to stable error codes and Persian-capable messages; include trace ID, never user enumeration details. Add structured correlation scope. Liveness tests process response only; readiness checks SQL connectivity and reports SMS configuration state without contacting the service.

- [ ] **Step 5: Add safe configuration templates**

Tracked JSON contains section shapes and non-secret lifetimes only. Connection string, CORS origins, endpoint URL, signing/hashing keys, and key-ring path remain blank or absent and are required through user secrets/environment. Add a README command that prompts locally for values without printing or committing them.

- [ ] **Step 6: Verify and commit**

Run integration tests, inspect OpenAPI for all approved operations, run `dotnet publish` to a temporary artifact directory, and commit `feat: expose secure authentication api`.

---

### Task 9: Establish frontend RTL theme, resources, providers, and test harness

**Files:**
- Move approved asset into: `resources/branding/eos.svg`
- Add licensed font files to: `resources/fonts/vazirmatn/`
- Modify: `resources/README.md`
- Create: `frontend/scripts/sync-resources.mjs`
- Create: `frontend/src/app/providers/AppProviders.tsx`
- Create: `frontend/src/app/providers/AppThemeProvider.tsx`
- Create: `frontend/src/theme/createAppTheme.ts`
- Create: `frontend/src/theme/palettes.ts`
- Create: `frontend/src/test/setup.ts`
- Modify: `frontend/vite.config.ts`
- Test: `frontend/src/theme/AppThemeProvider.test.tsx`

**Interfaces:**
- Produces: `AppThemeProvider`, `useAppearance()`, navy/teal palette, light/dark/system modes, RTL Emotion cache, and generated `/assets/brand/eos.svg` plus Vazirmatn web assets.

- [ ] **Step 1: Import and document approved assets**

Copy the supplied SVG byte-for-byte into `resources/branding/eos.svg`. Acquire the official Vazirmatn WOFF2 variable font and OFL license from the approved upstream release, record source/version/license/SHA-256 in `resources/README.md`, and do not recolor the SVG.

- [ ] **Step 2: Write failing resource/theme tests**

Assert document direction is `rtl`, body uses Vazirmatn, default mode/palette are system + navy/teal, explicit modes override system preference, dark mode wraps the black/red logo in the neutral contrast surface, and reduced-motion media preference disables nonessential transitions.

- [ ] **Step 3: Implement deterministic resource sync**

`sync-resources.mjs` clears only `frontend/public/generated-assets`, copies approved logo/font/license files from root resources, and writes no other path. Hook it to `predev`, `prebuild`, and `pretest`; ignore the generated destination in Git.

- [ ] **Step 4: Implement RTL theme providers**

Create an Emotion cache with Stylis RTL plugin, MUI locale/direction configuration, Vazirmatn `@font-face`, semantic focus/contrast/motion tokens, and appearance storage keys scoped to the current user when authenticated and anonymous login otherwise.

- [ ] **Step 5: Verify and commit**

Run frontend tests/typecheck/build, inspect generated assets and license, verify the source SVG hash matches the attachment, then commit `feat: establish rtl design foundation`.

---

### Task 10: Implement the in-memory auth client and sign-in/OTP UI

**Files:**
- Create: `frontend/src/lib/api/authTokenStore.ts`
- Create: `frontend/src/lib/api/apiClient.ts`
- Create: `frontend/src/app/providers/AuthProvider.tsx`
- Create: `frontend/src/features/auth/SignInPage.tsx`
- Create: `frontend/src/features/auth/OtpForm.tsx`
- Create: `frontend/src/features/auth/authApi.ts`
- Create: `frontend/src/features/auth/authTypes.ts`
- Test: adjacent `*.test.tsx` and `*.test.ts` files

**Interfaces:**
- Produces: `useAuth()` with `status`, `user`, `startSignIn`, `verifyOtp`, `refresh`, and `logout`.
- Produces: one credentialed fetch client that serializes refresh attempts and retries an eligible request once.

- [ ] **Step 1: Write failing token-store/client tests**

Assert access token exists only in module memory, never local/session storage; requests attach Bearer token; one 401 starts exactly one refresh for concurrent calls; failed refresh clears auth; refresh/logout include credentials and anti-forgery header; errors preserve trace ID.

- [ ] **Step 2: Implement the minimal API client**

Use `fetch`, `credentials: 'include'`, an in-memory closure for access token, one shared refresh promise, JSON/problem parsing, abort propagation, and exactly one post-refresh retry. Do not log bodies, tokens, or complete user data.

- [ ] **Step 3: Write failing sign-in and OTP component tests**

Cover single organizational button, loading lock, name/masked-mobile display, six digits including paste, Persian error/status text, five-minute countdown from server expiry, 60-second resend gate, cancel/back, keyboard/focus behavior, and successful transition.

- [ ] **Step 4: Implement AuthProvider and pages**

On application bootstrap call refresh once; success restores the session without OTP, failure shows sign-in. Start sign-in requests Windows credentials, OTP verify stores only access token in memory, and logout clears auth plus invokes the tab-store clear hook supplied by Task 11.

- [ ] **Step 5: Verify and commit**

Run focused tests, all frontend tests, typecheck, accessibility assertions, and build; commit `feat: add organizational otp sign in`.

---

### Task 11: Implement the fixed shell and route-aware internal tab workspace

**Files:**
- Create: `frontend/src/navigation/tabTypes.ts`
- Create: `frontend/src/navigation/tabReducer.ts`
- Create: `frontend/src/navigation/TabWorkspaceProvider.tsx`
- Create: `frontend/src/navigation/routeRegistry.tsx`
- Create: `frontend/src/navigation/DirtyPageGuard.tsx`
- Create: `frontend/src/layout/AppShell.tsx`
- Create: `frontend/src/layout/AppHeader.tsx`
- Create: `frontend/src/layout/AppSidebar.tsx`
- Create: `frontend/src/layout/WorkspaceTabs.tsx`
- Create: `frontend/src/layout/StatusBar.tsx`
- Create: `frontend/src/pages/HomePage.tsx`
- Create: `frontend/src/lib/date/persianDateTime.ts`
- Test: reducer/component/date tests adjacent to files

**Interfaces:**
- Produces: `TabDescriptor { key, routeId, pathname, search, title, closable, state }`.
- Produces: `openTab`, `activateTab`, `closeTab`, `closeOthers`, `closeAll`, `setSerializableState`, `markDirty`, `clearSessionTabs`.

- [ ] **Step 1: Write failing pure tab-reducer tests**

Cover fixed home, logical deduplication, parameter-distinct keys, active fallback after close, close-other/all behavior, dirty close rejection until confirmed, valid session-storage serialization, corrupt-storage recovery, and full clear on logout.

- [ ] **Step 2: Implement reducer/store and route registry**

Derive keys only from registered route ID plus allowlisted relevant parameters. Store descriptors and approved serializable state in `sessionStorage`, never mounted components/functions. Render exactly one route element for the active descriptor and synchronize browser navigation without update loops.

- [ ] **Step 3: Write failing shell/status tests**

Assert fixed header/footer/sidebar, central-only scrolling, menu collapse persistence, user/role/branding, logout, keyboard-accessible tabs, overflow selector, build version, Persian-calendar date, Persian digits, and Asia/Tehran clock advancing without rerendering unrelated shell regions.

- [ ] **Step 4: Implement the shell**

Use MUI semantic landmarks and responsive breakpoints. Display the exact company name `علم و صنعت` with the approved EOS logo, make home nonclosable, show `داشبوردها به‌زودی اضافه می‌شوند`, keep logo contrast in dark mode, update only the clock text each second, and source version from a Vite compile-time constant derived from `package.json`.

- [ ] **Step 5: Verify and commit**

Run frontend tests/typecheck/build and Playwright component-visible checks at desktop, tablet, and phone widths; commit `feat: add tabbed application shell`.

---

### Task 12: Persist current-user preferences end to end

**Files:**
- Create: `backend/src/EosDashboards.Application/Preferences/GetMyPreferences.cs`
- Create: `backend/src/EosDashboards.Application/Preferences/UpdateMyPreferences.cs`
- Modify: `backend/src/EosDashboards.Api/Preferences/PreferenceEndpoints.cs`
- Create: `frontend/src/features/preferences/preferencesApi.ts`
- Modify: theme/sidebar providers
- Test: backend Application/API and frontend provider tests

**Interfaces:**
- Produces: `UserPreferenceDto(string AppearanceMode, string Palette, bool SidebarCollapsed)`.
- Consumes: authenticated user ID and allowlisted values `light|dark|system`, initially supported palette `navyTeal`.

- [ ] **Step 1: Write failing backend tests**

Assert first read returns approved defaults, update validates allowlists, upserts one row per user, cannot update another user, and returns the saved value.

- [ ] **Step 2: Implement backend use cases/endpoints**

Use JWT subject only, no request user ID. Project no-tracking for reads and save one user-scoped row for updates. Audit meaningful preference changes without recording unnecessary personal data.

- [ ] **Step 3: Write failing frontend synchronization tests**

Assert local cached appearance prevents initial flash, authenticated server values reconcile after load, changes update UI immediately and persist, failure rolls back with feedback, and anonymous preference keys migrate to user-scoped keys only after successful login.

- [ ] **Step 4: Implement preference synchronization**

Use TanStack Query for server state and local cache only for prepaint appearance. Debounce sidebar preference updates, cancel stale mutations, and keep tab descriptors in session storage rather than preference records.

- [ ] **Step 5: Verify and commit**

Run all backend/frontend tests and builds, then commit `feat: persist user appearance preferences`.

---

### Task 13: Complete integration, browser flow, deployment artifacts, and documentation

**Files:**
- Create: `frontend/tests/e2e/auth-shell.spec.ts`
- Create: `frontend/playwright.config.ts`
- Create: `backend/src/EosDashboards.Api/appsettings.Testing.json`
- Create: `docs/operations/development-setup.md`
- Create: `docs/operations/administrator-provisioning.md`
- Create: `docs/operations/iis-deployment.md`
- Create: `docs/operations/authentication-runbook.md`
- Modify: `docs/project/current-state.md`
- Modify: `docs/project/roadmap.md`
- Modify: `resources/README.md`
- Modify: `AGENTS.md` if implementation changes durable startup facts

**Interfaces:**
- Consumes: the complete backend and frontend slice.
- Produces: repeatable local startup, fake-service E2E flow, IIS publish artifacts, migration bundle/script, provisioning runbook, and verified project memory.

- [ ] **Step 1: Write the failing end-to-end flow**

In Playwright, use only the test API host with fake Windows identity/SMS endpoints enabled exclusively by the `Testing` environment. Cover organizational button -> OTP challenge -> known synthetic code -> home shell -> open/deduplicate/close tabs -> change theme/menu -> verify footer -> logout -> tabs cleared -> OTP required again. Add denial cases for unknown identity and invalid/exhausted code.

- [ ] **Step 2: Add safe test-host controls**

Compile fake identity/SMS registration in the IntegrationTests host factory, not production API configuration. Make API startup reject fake-provider settings outside `Testing`. Playwright receives the synthetic OTP from the test harness, never from logs or a real service.

- [ ] **Step 3: Run full verification**

Run:

```powershell
dotnet restore backend/EosDashboards.sln --locked-mode
dotnet build backend/EosDashboards.sln --no-restore
dotnet test backend/EosDashboards.sln --no-build
npm ci --prefix frontend
npm --prefix frontend run format:check
npm --prefix frontend run typecheck
npm --prefix frontend test
npm --prefix frontend run build
npm --prefix frontend run e2e
```

Expected: zero failed tests, zero analyzer/type/format errors, and no real SMS/network dependency beyond the isolated SQL Server test database.

- [ ] **Step 4: Build, inspect, and deploy to the local development IIS targets**

Publish API and frontend into separate temporary/versioned directories, generate an idempotent EF migration script or migration bundle, and inspect outputs for secrets, development files, source maps policy, correct static caching, `web.config`, health endpoints, and independent IIS deployability. After inspection, deploy the two artifacts to the already configured IIS targets on the developer's machine, using separate sites/applications and application pools, connect the API only to the local development database, and run local smoke tests. Do not deploy to company production servers in this task.

- [ ] **Step 5: Perform an explicitly authorized real-development smoke test**

Only after the user confirms receipt testing, configure the real development SQL/SMS values through user secrets, apply migration to `EosDashboard`, run the provisioner interactively with the real administrator details, and perform one real sign-in/SMS/OTP/logout cycle. Record only pass/fail, timestamps, masked identity, and trace IDs; never record the complete mobile, OTP, or credentials.

- [ ] **Step 6: Update durable and formal documentation before publication**

Document exact supported local setup, safe secret entry, isolated test database guard, provisioning procedure, IIS separation, deployment/migration/rollback procedure, authentication failure runbook, implemented versions, and verified commands. Update `current-state.md` to the actual outcome and keep unresolved dashboard/IT decisions explicit.

- [ ] **Step 7: Final review, commit, merge, verify, and push**

Run `git diff --check`, secret/PII scans, and full verification again after documentation changes in the implementation worktree. Commit the finished feature there, then integrate from the primary checkout only after both checkouts are clean:

```powershell
git add AGENTS.md backend frontend docs resources .gitignore global.json .editorconfig
git commit -m "feat: deliver authenticated dashboard foundation"
git status --short
git -C D:\Workspaces\ChatGpt\EosDashboards fetch origin main
git -C D:\Workspaces\ChatGpt\EosDashboards pull --ff-only origin main
git -C D:\Workspaces\ChatGpt\EosDashboards merge --no-ff feature/initial-authentication-shell
dotnet test D:\Workspaces\ChatGpt\EosDashboards\backend\EosDashboards.sln
npm --prefix D:\Workspaces\ChatGpt\EosDashboards\frontend test
npm --prefix D:\Workspaces\ChatGpt\EosDashboards\frontend run build
git -C D:\Workspaces\ChatGpt\EosDashboards push origin main
git -C D:\Workspaces\ChatGpt\EosDashboards fetch origin main
git -C D:\Workspaces\ChatGpt\EosDashboards rev-parse HEAD
git -C D:\Workspaces\ChatGpt\EosDashboards rev-parse origin/main
git -C D:\Workspaces\ChatGpt\EosDashboards status --short
```

Expected: the merge result passes fresh tests, local and remote `main` hashes match, and the primary working tree is clean. If merge verification or push fails, integration remains incomplete and must be reported without claiming completion.
