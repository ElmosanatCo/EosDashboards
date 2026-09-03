# Server-local Time and Navigation Corrections Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Store all application timestamps as local server time with millisecond precision and correct fixed-home and mobile-drawer shell behavior.

**Status:** Implemented and integrated into `main` as merge commit `98a457d`.

**Architecture:** Replace `DateTimeOffset` persistence with local `DateTime` values supplied by a millisecond-truncating `IClock`. Rename every timestamp member and contract from `…Utc` to `…At`, then use an EF Core migration to preserve legacy timestamp meaning while changing storage to `datetime2(3)`. Keep JWT numeric-date conversion contained in the issuer. Give the sidebar a separate home-activation action and constrain the temporary Drawer and its modal to the space between header and status bar.

**Tech Stack:** .NET 10, EF Core 10, SQL Server, ASP.NET Core, React 19, TypeScript, Material UI 9, Vitest, Playwright.

**Spec:** `docs/superpowers/specs/2026-09-03-server-local-time-and-navigation-corrections-design.md`

## Global Constraints

- Persist current application-server local date/time only as SQL Server `datetime2(3)`; values contain year through millisecond, with no UTC value, offset, timezone, or finer precision.
- Remove every `Utc` timestamp name without introducing underscore-based time suffixes; use PascalCase names such as `CreatedAt`, `UpdatedAt`, `ExpiresAt`, `OccurredAt`, and `RevokedAt`.
- Ordinary OTP, session, audit, preference, and persistence logic compares local server values directly and does not use an Asia/Tehran conversion.
- Legacy UTC `datetimeoffset` database values must be converted once to equivalent server-local wall-clock values before their type/precision changes.
- Do not persist an external protocol representation. JWT numeric-date conversion is allowed only inside the issuer boundary.
- A fixed `خانه` tab activates without collapsing the persistent desktop menu. A temporary mobile drawer, including its backdrop, stops above the fixed status bar.
- Preserve the unrelated untracked `test-results/` directory.

---

## File structure

| File group | Responsibility |
| --- | --- |
| `backend/src/EosDashboards.Application/Abstractions/IClock.cs`, `backend/src/EosDashboards.Infrastructure/Security/SystemClock.cs` | Expose a local, millisecond-precision application clock. |
| `backend/src/EosDashboards.Domain/Entities/*.cs` and Application/Auth/Provisioning services | Rename and compare timestamp properties directly in local time. |
| `backend/src/EosDashboards.Infrastructure/Persistence/Configurations/*.cs`, `Migrations/*`, `EosDashboardDbContextModelSnapshot.cs` | Map renamed values to `datetime2(3)` and migrate legacy rows. |
| `backend/src/EosDashboards.Api/Auth/*.cs`, `Security/*.cs`, frontend auth contracts/components | Rename HTTP fields and retain only external JWT conversion at the issuer. |
| `frontend/src/layout/AppShell.tsx`, `AppSidebar.tsx`, `lib/date/persianDateTime.ts` | Activate home independently, preserve desktop navigation, and bound mobile overlay geometry. |
| `backend/tests/**`, `frontend/src/**/*.test.tsx`, `frontend/tests/e2e/auth-shell.spec.ts` | Lock local-time behavior, API naming, home action, and status-bar visibility. |

### Task 1: Establish local millisecond time in the domain and application layer

**Files:**
- Modify: `backend/src/EosDashboards.Application/Abstractions/IClock.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Security/SystemClock.cs`
- Modify: `backend/src/EosDashboards.Domain/Entities/AuditLog.cs`, `Department.cs`, `ExternalIdentityLink.cs`, `OtpChallenge.cs`, `Role.cs`, `User.cs`, `UserPreference.cs`, `UserSession.cs`
- Modify: `backend/src/EosDashboards.Application/Auth/ChangePassword.cs`, `CompletePasswordReset.cs`, `GoogleSignIn.cs`, `Logout.cs`, `RefreshSession.cs`, `StartPasswordReset.cs`, `StartSignIn.cs`, `VerifyOtp.cs`, `Preferences/UpdateMyPreferences.cs`, `Provisioning/ProvisionSystemAdministrator.cs`
- Modify: `backend/tests/EosDashboards.Domain.Tests/OtpChallengeTests.cs`, `UserSessionTests.cs`, `ExternalIdentityLinkTests.cs`, `DepartmentTests.cs`, `UserTests.cs`
- Modify: `backend/tests/EosDashboards.Application.Tests/Auth/AuthenticationFakes.cs`, `StartSignInTests.cs`, `VerifyOtpTests.cs`, `SessionLifecycleTests.cs`, `PasswordResetTests.cs`, `ChangePasswordTests.cs`, `GoogleSignInTests.cs`, `Preferences/UserPreferenceTests.cs`, `Provisioning/ProvisionSystemAdministratorTests.cs`

**Interfaces:**
- Produces: `IClock.Now: DateTime`; `SystemClock.Now` is a local `DateTime` whose ticks are divisible by `TimeSpan.TicksPerMillisecond`.
- Produces: Domain time members named `CreatedAt`, `UpdatedAt`, `ExpiresAt`, `ResendAvailableAt`, `ConsumedAt`, `OccurredAt`, `LinkedAt`, `DeactivatedAt`, `LastRefreshedAt`, and `RevokedAt`.
- Consumes: all callers replace `clock.UtcNow` with `clock.Now` and no domain method invokes `ToUniversalTime()`.

- [ ] **Step 1: Write failing domain tests for the local no-conversion contract.**

```csharp
[Fact]
public void Create_PreservesLocalCreatedAtAndExpiresAt()
{
    var createdAt = new DateTime(2026, 9, 3, 18, 30, 15, 123, DateTimeKind.Unspecified);
    var challenge = OtpChallenge.Create(1, "token", new string('a', 64), createdAt, createdAt.AddMinutes(5));

    Assert.Equal(createdAt, challenge.CreatedAt);
    Assert.Equal(createdAt.AddMinutes(5), challenge.ExpiresAt);
}

[Fact]
public void Now_HasLocalMillisecondPrecision()
{
    var value = new SystemClock().Now;
    Assert.Equal(DateTimeKind.Unspecified, value.Kind);
    Assert.Equal(0, value.Ticks % TimeSpan.TicksPerMillisecond);
}
```

- [ ] **Step 2: Run the focused domain tests and verify they fail because `Now`, `CreatedAt`, and `ExpiresAt` do not exist.**

Run: `dotnet test backend/tests/EosDashboards.Domain.Tests/EosDashboards.Domain.Tests.csproj --filter "FullyQualifiedName~OtpChallengeTests|FullyQualifiedName~UserSessionTests"`

Expected: compilation failure identifying the old `UtcNow`, `CreatedAtUtc`, or `ExpiresAtUtc` members.

- [ ] **Step 3: Implement the local time API and direct comparisons.**

```csharp
public interface IClock
{
    DateTime Now { get; }
}

public DateTime Now
{
    get
    {
        var local = DateTime.Now;
        return DateTime.SpecifyKind(
            local.AddTicks(-(local.Ticks % TimeSpan.TicksPerMillisecond)),
            DateTimeKind.Unspecified);
    }
}
```

Rename every affected timestamp parameter/property to its no-`Utc` form, change its type to `DateTime`, remove calls to `ToUniversalTime()`, and compare the supplied local values directly. Update test fakes to implement `Now` with `DateTime` values at millisecond precision.

- [ ] **Step 4: Run focused domain and Application tests and verify they pass.**

Run: `dotnet test backend/tests/EosDashboards.Domain.Tests/EosDashboards.Domain.Tests.csproj --filter "FullyQualifiedName~OtpChallengeTests|FullyQualifiedName~UserSessionTests|FullyQualifiedName~ExternalIdentityLinkTests"`

Run: `dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter "FullyQualifiedName~StartSignInTests|FullyQualifiedName~VerifyOtpTests|FullyQualifiedName~SessionLifecycleTests|FullyQualifiedName~PasswordResetTests"`

Expected: all selected tests pass with no production `Utc` time member left in these layers.

- [ ] **Step 5: Commit the domain/application slice.**

```powershell
git add backend/src/EosDashboards.Application backend/src/EosDashboards.Domain backend/src/EosDashboards.Infrastructure/Security/SystemClock.cs backend/tests/EosDashboards.Domain.Tests backend/tests/EosDashboards.Application.Tests
git commit -m "refactor: use local millisecond timestamps"
```

### Task 2: Migrate SQL Server timestamp columns and indexes

**Files:**
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs`, `DepartmentConfiguration.cs`, `ExternalIdentityLinkConfiguration.cs`, `OtpChallengeConfiguration.cs`, `RoleConfiguration.cs`, `UserConfiguration.cs`, `UserPreferenceConfiguration.cs`, `UserSessionConfiguration.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Persistence/Migrations/20260903190000_UseServerLocalMillisecondTimestamps.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Persistence/Migrations/20260903190000_UseServerLocalMillisecondTimestamps.Designer.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Migrations/EosDashboardDbContextModelSnapshot.cs`
- Modify: `backend/tests/EosDashboards.IntegrationTests/Database/DatabaseConstraintTests.cs`, `RepositoryTests.cs`

**Interfaces:**
- Consumes: renamed `DateTime` domain properties from Task 1.
- Produces: columns and indexes named without `Utc`, mapped as `datetime2(3)`.
- Produces: an idempotent-safe EF migration that turns each legacy `datetimeoffset(7)` value into its equivalent local wall-clock value before dropping offset/fractional precision.

- [ ] **Step 1: Write a failing integration assertion for SQL type, name, precision, and semantics.**

```csharp
[Fact]
public async Task OtpChallengeTimestampColumns_UseLocalMillisecondDatetime2()
{
    await using var context = database.CreateDbContext();
    var property = context.Model.FindEntityType(typeof(OtpChallenge))!
        .FindProperty(nameof(OtpChallenge.CreatedAt))!;

    Assert.Equal("datetime2(3)", property.GetColumnType());
    Assert.Null(context.Model.FindEntityType(typeof(OtpChallenge))!
        .FindProperty("CreatedAtUtc"));
}
```

- [ ] **Step 2: Run the integration test and verify it fails against the existing `datetimeoffset(7)` mapping.**

Run: `dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --filter "FullyQualifiedName~DatabaseConstraintTests"`

Expected: compilation or assertion failure for the absent `CreatedAt` property and current `datetimeoffset(7)` mapping.

- [ ] **Step 3: Change EF mappings and generate the migration from the completed model.**

Map every persisted timestamp with `.HasColumnType("datetime2(3)")`; update composite indexes to use no-`Utc` members. Generate a single migration named `UseServerLocalMillisecondTimestamps`. In its `Up`, for every timestamp column, add the no-`Utc` `datetime2(3)` column, populate it from the legacy `datetimeoffset` value converted to the configured SQL Server local wall-clock offset, rebuild indexes with the new names, then remove the legacy column. Do not edit historical migrations. In `Down`, restore the previous column names/types only for development rollback.

- [ ] **Step 4: Apply the migration to the isolated integration database and re-run the focused integration tests.**

Run: `dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --filter "FullyQualifiedName~DatabaseConstraintTests|FullyQualifiedName~RepositoryTests"`

Expected: selected persistence/concurrency tests pass and model metadata reports `datetime2(3)` for every changed timestamp.

- [ ] **Step 5: Commit the persistence migration slice.**

```powershell
git add backend/src/EosDashboards.Infrastructure/Persistence backend/tests/EosDashboards.IntegrationTests
git commit -m "feat: persist local millisecond timestamps"
```

### Task 3: Rename API contracts and isolate JWT protocol conversion

**Files:**
- Modify: `backend/src/EosDashboards.Application/Abstractions/IAccessTokenIssuer.cs`, `IUserSessionRepository.cs`
- Modify: `backend/src/EosDashboards.Application/Auth/AuthContracts.cs`
- Modify: `backend/src/EosDashboards.Api/Auth/AuthContracts.cs`, `AuthEndpoints.cs`, `GoogleAuthenticationEvents.cs`
- Modify: `backend/src/EosDashboards.Api/Security/RefreshCookieService.cs`, `SessionAuthorization.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/AuditWriter.cs`, `OtpChallengeRepository.cs`, `UserSessionRepository.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Security/JwtAccessTokenIssuer.cs`
- Modify: `backend/tests/EosDashboards.Application.Tests/Auth/AuthContractSafetyTests.cs`, `GoogleSignInTests.cs`, `SessionLifecycleTests.cs`
- Modify: `backend/tests/EosDashboards.IntegrationTests/Security/JwtAccessTokenIssuerTests.cs`, `SecurityPrimitiveTests.cs`, `Provisioning/ProvisionerTests.cs`

**Interfaces:**
- Produces: response fields such as `expiresAt`, `resendAvailableAt`, `accessTokenExpiresAt`, and `sessionExpiresAt`; no browser-delivered property ends in `Utc`.
- Consumes: local `DateTime` session values.
- Produces: `JwtAccessTokenIssuer` converts local `DateTime` to an instant only inside token construction and returns the no-`Utc` expiration property.

- [ ] **Step 1: Write failing contract tests for no-`Utc` JSON fields.**

```csharp
[Fact]
public void ChallengeResponse_UsesLocalTimeFieldNames()
{
    var json = JsonSerializer.Serialize(new OtpChallengeResponse(
        "token", "***0000", new DateTime(2026, 9, 3, 9, 0, 0), new DateTime(2026, 9, 3, 8, 56, 0)));

    Assert.Contains("expiresAt", json, StringComparison.Ordinal);
    Assert.DoesNotContain("Utc", json, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run the focused API/Application contract tests and verify the old fields fail the assertion.**

Run: `dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter "FullyQualifiedName~AuthContractSafetyTests|FullyQualifiedName~SessionLifecycleTests"`

Expected: failure because serialized names still include `Utc`.

- [ ] **Step 3: Rename contracts and keep JWT conversion private to the issuer.**

Rename all public response/request time properties and all endpoint mappings without `Utc`. In `JwtAccessTokenIssuer`, accept local `DateTime` values, validate ordering before conversion, create `DateTimeOffset` only immediately before the JWT handler is called, and return the original local expiration value in `IssuedAccessToken`. Do not write a converted value to an entity or repository.

- [ ] **Step 4: Run contract, issuer, and authorization tests.**

Run: `dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter "FullyQualifiedName~AuthContractSafetyTests|FullyQualifiedName~GoogleSignInTests|FullyQualifiedName~SessionLifecycleTests"`

Run: `dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --filter "FullyQualifiedName~JwtAccessTokenIssuerTests|FullyQualifiedName~SecurityPrimitiveTests"`

Expected: all selected tests pass; a repository-wide source scan finds no `Utc` time member outside legacy migration history.

- [ ] **Step 5: Commit the API/protocol slice.**

```powershell
git add backend/src/EosDashboards.Api backend/src/EosDashboards.Application backend/src/EosDashboards.Infrastructure/Persistence/Repositories backend/src/EosDashboards.Infrastructure/Security/JwtAccessTokenIssuer.cs backend/tests/EosDashboards.Application.Tests backend/tests/EosDashboards.IntegrationTests
git commit -m "refactor: remove UTC timestamp contracts"
```

### Task 4: Correct frontend time names, home activation, and mobile drawer bounds

**Files:**
- Modify: `frontend/src/features/auth/authTypes.ts`, `OtpForm.tsx`, `SignInPage.tsx`
- Modify: `frontend/src/layout/AppShell.tsx`, `AppSidebar.tsx`, `StatusBar.tsx`
- Modify: `frontend/src/lib/date/persianDateTime.ts`
- Modify: `frontend/src/features/auth/OtpForm.test.tsx`
- Modify: `frontend/tests/e2e/auth-shell.spec.ts`

**Interfaces:**
- Consumes: API `expiresAt`, `resendAvailableAt`, `accessTokenExpiresAt`, and `sessionExpiresAt` JSON properties.
- Produces: `AppSidebar.onActivateHome(): void`, which activates route key `home` without changing desktop collapsed state.
- Produces: temporary Drawer paper and modal bounds `{ top: 64, bottom: statusBarHeight }` relative to the viewport.

- [ ] **Step 1: Write failing frontend tests for the names and shell interactions.**

```tsx
await page.getByRole("button", { name: "خانه" }).click();
await expect(page.getByRole("navigation", { name: "منوی اصلی" })).toBeVisible();

const bounds = await page.getByRole("navigation", { name: "منوی اصلی" }).evaluate((element) => {
  const rect = element.getBoundingClientRect();
  return { bottom: Math.round(rect.bottom), viewportHeight: window.innerHeight };
});
expect(bounds.bottom).toBeLessThan(bounds.viewportHeight);
```

Update mocked responses to send `expiresAt`, `resendAvailableAt`, `accessTokenExpiresAt`, and `sessionExpiresAt`; assert countdown/resend behavior continues to work.

- [ ] **Step 2: Run the affected Vitest/Playwright tests and verify failure under the current callback and field names.**

Run: `npm --prefix frontend test -- OtpForm.test.tsx`

Run: `$env:EOS_PLAYWRIGHT_PORT=4174; npm --prefix frontend run e2e -- --grep "local credential OTP"`

Expected: test failure because the old JSON keys or the home `onClose` behavior still exist.

- [ ] **Step 3: Implement the smallest shell and contract changes.**

```tsx
<ListItemButton onClick={onActivateHome}>
  <ListItemIcon><HomeIcon /></ListItemIcon>
  <ListItemText primary="خانه" />
</ListItemButton>
```

In `AppShell`, dispatch `{ type: "activate", key: "home" }` from `onActivateHome` without updating `sidebarCollapsed` or closing the mobile drawer. Change temporary Drawer `ModalProps.sx` and `.MuiDrawer-paper` to use the same header top and `statusBarHeight` bottom. Remove `timeZone: "Asia/Tehran"` from the local Persian formatter and rename all frontend authentication time fields without `Utc`.

- [ ] **Step 4: Run frontend quality gates and rendered mobile check.**

Run: `npm --prefix frontend run typecheck`

Run: `npm --prefix frontend test -- OtpForm.test.tsx`

Run: `$env:EOS_PLAYWRIGHT_PORT=4174; npm --prefix frontend run e2e`

Expected: typecheck, focused component test, and all browser flows pass; mobile drawer bounds leave the footer visible.

- [ ] **Step 5: Commit the frontend slice.**

```powershell
git add frontend/src frontend/tests/e2e/auth-shell.spec.ts
git commit -m "fix: preserve home navigation and status bar"
```

### Task 5: Verify the integrated release and update durable state

**Files:**
- Modify: `docs/project/current-state.md`
- Modify: `docs/superpowers/specs/2026-09-03-server-local-time-and-navigation-corrections-design.md`
- Modify: `docs/superpowers/plans/2026-09-03-server-local-time-and-navigation-corrections.md`

**Interfaces:**
- Consumes: all completed Task 1–4 commits.
- Produces: an integrated main-ready source record stating exact verification and migration outcome without private data.

- [ ] **Step 1: Write the final failing verification scan before declaring the feature complete.**

```powershell
rg -n --glob '!**/Migrations/20260902*' --glob '!**/Migrations/20260903103049*' --glob '!**/Migrations/20260903150249*' 'Utc|TimeZoneInfo|Asia/Tehran' backend/src backend/tests frontend/src frontend/tests
```

Expected: the scan identifies remaining production `Utc` references or an unwanted Tehran formatter until Tasks 1–4 are complete.

- [ ] **Step 2: Run integrated checks after completing all changes.**

Run: `dotnet test backend/EosDashboards.sln -c Release`

Run: `npm --prefix frontend run typecheck`

Run: `npm --prefix frontend run format:check`

Run: `npm --prefix frontend run build:iis`

Run: `$env:EOS_PLAYWRIGHT_PORT=4174; npm --prefix frontend run e2e`

Expected: all commands succeed. Record only test counts/outcomes and no credentials or connection details.

- [ ] **Step 3: Update completed state and design/plan status.**

Set the design and this plan to `Implemented`; replace the pending timestamp bullet in `current-state.md` with the actual migration identifier, committed source revision, test outcome, and any local IIS publication status. Preserve release provenance rules and omit private data.

- [ ] **Step 4: Commit final documentation.**

```powershell
git add docs/project/current-state.md docs/superpowers/specs/2026-09-03-server-local-time-and-navigation-corrections-design.md docs/superpowers/plans/2026-09-03-server-local-time-and-navigation-corrections.md
git commit -m "docs: record local time navigation verification"
```

- [ ] **Step 5: Review and publish the integrated branch.**

Run: `git diff main...HEAD --check`

Run: `git status --short`

After review approval, merge the integrated branch to `main`, verify the merged revision, and immediately push `main` to `origin`. Do not publish IIS until the user explicitly requests a deployment.

## Plan self-review

- Spec coverage: Tasks 1–3 implement the local, millisecond, no-`Utc` time model and migration; Task 4 implements both navigation corrections; Task 5 verifies and records all outcomes.
- Placeholder scan: every executable task names its files, interfaces, test command, expected outcome, and implementation action.
- Type consistency: Task 1 establishes `DateTime IClock.Now`; Tasks 2–3 consume `DateTime` domain properties; Task 4 consumes no-`Utc` JSON names and `onActivateHome`; Task 5 scans the integrated source.
