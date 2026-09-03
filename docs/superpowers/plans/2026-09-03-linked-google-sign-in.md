# Linked Google Sign-in Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a pre-linked active user create the existing EosDashboards session through Google OpenID Connect, without password or SMS OTP.

**Architecture:** The API starts and completes a server-owned Google OpenID Connect Authorization Code flow with PKCE through ASP.NET Core's dedicated `Google` scheme. A persistent external-identity link maps the verified Google identity to one existing EosDashboards user; the callback uses the existing session and secure refresh-cookie service before redirecting to the SPA. React only discovers availability and navigates to the start endpoint.

**Tech Stack:** .NET 10, ASP.NET Core OpenID Connect handler, Entity Framework Core 10 / SQL Server, React 19, Material UI, Vitest, Playwright.

**Spec:** `docs/superpowers/specs/2026-09-03-google-sign-in-design.md`

## Global Constraints

- Preserve local username/password plus mandatory SMS OTP, recovery, password-change, session-refresh, logout, CORS, and authorization behavior unchanged.
- Use the server-side Authorization Code flow with PKCE; no Google client secret, authorization code, ID token, refresh credential, state, nonce, or complete email may enter React, logs, audit metadata, test output, URLs after callback, or documentation.
- Only an active user with an explicitly provisioned Google email link may sign in. Do not create users, roles, permissions, or links from an unknown Google account.
- The local callback URI is exactly `https://localhost/EosDashboardsApi/api/v1/auth/google/callback`.
- Browser session smoke checks use `https://localhost/EosDashboards/`; the HTTP Vite preview is not a valid secure-cookie session test.
- Do not call Google in automated tests. Do not initiate a real authorization or modify external Google Cloud settings without the user's explicit request.
- Use UTF-8 at every provisioning boundary; never write the provided Google email into repository documentation.
- Keep all Google client configuration server-side under the accepted local-configuration policy. The frontend gets only the anonymous enabled/disabled capability response.

---

## File structure

| Path | Responsibility |
| --- | --- |
| `backend/src/EosDashboards.Domain/Enums/ExternalIdentityProvider.cs` | Supported external provider code, initially `Google`. |
| `backend/src/EosDashboards.Domain/Entities/ExternalIdentityLink.cs` | Invariants for a pre-approved email link and immutable provider-subject binding. |
| `backend/src/EosDashboards.Application/Abstractions/IExternalIdentityLinkRepository.cs` | Application-facing lookup/upsert contract for identity links. |
| `backend/src/EosDashboards.Application/Auth/GoogleSignIn.cs` | Maps a validated Google identity to an active user and issues the established application session. |
| `backend/src/EosDashboards.Infrastructure/Persistence/Configurations/ExternalIdentityLinkConfiguration.cs` | SQL Server mapping and filtered unique indexes. |
| `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/ExternalIdentityLinkRepository.cs` | EF Core implementation of the link repository. |
| `backend/src/EosDashboards.Infrastructure/Security/GoogleAuthenticationOptions.cs` | Typed, startup-validated Google configuration. |
| `backend/src/EosDashboards.Api/Auth/GoogleAuthenticationEvents.cs` | OIDC ticket/callback behavior, safe redirects, and session-cookie setup. |
| `backend/src/EosDashboards.Api/Auth/AuthEndpoints.cs` | Provider discovery and Google-start endpoints. |
| `backend/tools/EosDashboards.AdminProvisioner/InteractiveInput.cs` | Hidden optional Google-email input; it never echoes the value. |
| `frontend/src/features/auth/GoogleSignInButton.tsx` | Accessible visual action that navigates to the API start endpoint. |
| `frontend/src/app/providers/AuthProvider.tsx` | Provider discovery and generic callback-error feedback. |
| `frontend/src/features/auth/SignInPage.tsx` | Composes the Google action beside the existing credential route. |
| `docs/operations/google-sign-in.md` | Local Google Cloud setup, provisioning, deployment, and HTTPS smoke instructions without secrets or personal data. |

### Task 1: External identity link model and SQL persistence

**Files:**
- Create: `backend/src/EosDashboards.Domain/Enums/ExternalIdentityProvider.cs`
- Create: `backend/src/EosDashboards.Domain/Entities/ExternalIdentityLink.cs`
- Create: `backend/src/EosDashboards.Application/Abstractions/IExternalIdentityLinkRepository.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Persistence/Configurations/ExternalIdentityLinkConfiguration.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/ExternalIdentityLinkRepository.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/EosDashboardDbContext.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/DependencyInjection.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Persistence/Migrations/20260903103049_ExternalIdentityLinks.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Migrations/EosDashboardDbContextModelSnapshot.cs`
- Test: `backend/tests/EosDashboards.Domain.Tests/Entities/ExternalIdentityLinkTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Database/ModelMappingTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Database/RepositoryTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Database/DatabaseConstraintTests.cs`

**Interfaces:**
- Produces `ExternalIdentityProvider.Google`.
- Produces `ExternalIdentityLink.CreatePending(long userId, ExternalIdentityProvider provider, string normalizedEmail, DateTimeOffset nowUtc)` and `BindSubject(string subject, DateTimeOffset nowUtc)`.
- Produces `IExternalIdentityLinkRepository.FindByProviderSubjectAsync`, `FindPendingByProviderEmailAsync`, `Add`, and `Update`.
- Consumed by provisioning and Google session issuance in later tasks.

- [x] **Step 1: Write failing domain tests for link invariants.**

```csharp
[Fact]
public void Pending_google_link_binds_its_subject_once()
{
    var link = ExternalIdentityLink.CreatePending(11, ExternalIdentityProvider.Google,
        "PERSON@EXAMPLE.COM", Now);

    link.BindSubject("google-subject", Now.AddMinutes(1));

    Assert.Equal("PERSON@EXAMPLE.COM", link.NormalizedEmail);
    Assert.Equal("google-subject", link.ProviderSubject);
    Assert.Throws<InvalidOperationException>(() =>
        link.BindSubject("different-subject", Now.AddMinutes(2)));
}
```

- [x] **Step 2: Run the domain test to verify it fails.**

Run: `dotnet test backend/tests/EosDashboards.Domain.Tests/EosDashboards.Domain.Tests.csproj --filter FullyQualifiedName~ExternalIdentityLinkTests`

Expected: FAIL because the provider and link types do not exist.

- [x] **Step 3: Add the minimal domain model and repository contract.**

```csharp
public interface IExternalIdentityLinkRepository
{
    Task<ExternalIdentityLink?> FindByProviderSubjectAsync(
        ExternalIdentityProvider provider, string subject, CancellationToken cancellationToken);
    Task<ExternalIdentityLink?> FindPendingByProviderEmailAsync(
        ExternalIdentityProvider provider, string normalizedEmail, CancellationToken cancellationToken);
    void Add(ExternalIdentityLink link);
}
```

Normalize approved email with `Trim().ToUpperInvariant()`, require positive `UserId`, reject blank provider subject, and make rebinding to a different subject fail. A repeated bind of the same subject is idempotent.

- [x] **Step 4: Add EF mapping, repository, and migration.**

```csharp
builder.ToTable("ExternalIdentityLinks");
builder.HasKey(link => link.Id);
builder.Property(link => link.Id).HasColumnType("bigint").UseIdentityColumn();
builder.HasIndex(link => new { link.Provider, link.NormalizedEmail }).IsUnique();
builder.HasIndex(link => new { link.Provider, link.ProviderSubject })
    .IsUnique().HasFilter("[ProviderSubject] IS NOT NULL");
builder.HasOne<User>().WithMany().HasForeignKey(link => link.UserId)
    .OnDelete(DeleteBehavior.Restrict);
```

Register the repository through `AddInfrastructurePersistence`, add the `DbSet`, then generate a single additive migration from `backend/` using the existing EF startup project. Verify the generated migration contains no seeded personal data.

- [x] **Step 5: Run focused domain and SQL-backed persistence tests.**

Run: `dotnet test backend/tests/EosDashboards.Domain.Tests/EosDashboards.Domain.Tests.csproj --filter FullyQualifiedName~ExternalIdentityLinkTests`

Run: `dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --filter "FullyQualifiedName~ModelMappingTests|FullyQualifiedName~RepositoryTests|FullyQualifiedName~DatabaseConstraintTests"`

Expected: PASS, including duplicate email/subject rejection and pending-email/subject lookup coverage.

- [x] **Step 6: Commit the independently testable persistence slice.**

```powershell
git add backend/src/EosDashboards.Domain backend/src/EosDashboards.Application/Abstractions backend/src/EosDashboards.Infrastructure backend/tests/EosDashboards.Domain.Tests backend/tests/EosDashboards.IntegrationTests
git commit -m "feat: persist linked external identities"
```

### Task 2: Provision an approved Google email without exposing it

**Files:**
- Modify: `backend/src/EosDashboards.Application/Provisioning/ProvisionSystemAdministrator.cs`
- Modify: `backend/tools/EosDashboards.AdminProvisioner/InteractiveInput.cs`
- Modify: `backend/tools/EosDashboards.AdminProvisioner/Program.cs`
- Test: `backend/tests/EosDashboards.Application.Tests/Provisioning/ProvisionSystemAdministratorTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Provisioning/ProvisionerTests.cs`

**Interfaces:**
- Consumes `IExternalIdentityLinkRepository` from Task 1.
- Extends `ProvisionSystemAdministratorCommand` with `string? GoogleEmail`.
- Produces one pending `Google` identity link for the provisioned administrator when a Google email is supplied.

- [x] **Step 1: Write failing provisioning tests.**

```csharp
[Fact]
public async Task Provisioning_creates_or_updates_a_pending_google_email_link()
{
    var result = await context.Provisioner.HandleAsync(
        context.Command with { GoogleEmail = "person@example.com" }, CancellationToken.None);

    var link = Assert.Single(context.ExternalIdentityLinks.Links);
    Assert.Equal(result.UserId, link.UserId);
    Assert.Null(link.ProviderSubject);
}
```

Also assert that a command without `GoogleEmail` leaves links untouched and that the interactive console uses `ReadSecret()` for the email and never writes it or an email-shaped confirmation string.

- [x] **Step 2: Run the focused provisioning tests to verify they fail.**

Run: `dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter FullyQualifiedName~ProvisionSystemAdministratorTests`

Expected: FAIL because the command has no Google email field and the provisioner has no link behavior.

- [x] **Step 3: Extend the application command and idempotent transaction.**

```csharp
public sealed record ProvisionSystemAdministratorCommand(
    string OrganizationalId, string AccountName, string Username, string Password,
    string FirstName, string LastName, string Mobile, string? GoogleEmail);
```

Inside the existing serialized transaction, normalize a nonblank Google email and add or update only that administrator's Google link. Updating its approved email never changes an already bound provider subject. Keep the existing provisioning audit value-free.

- [x] **Step 4: Extend interactive input using the existing hidden-entry path.**

```csharp
console.Write("ایمیل Google برای اتصال ورود (مخفی، اختیاری): ");
var googleEmail = console.ReadSecret();
```

Accept an empty hidden value as no update. Do not print the input, masked email, or a derived email. The confirmation copy states only that the Google sign-in link will be updated.

- [x] **Step 5: Run focused tests and verify UTF-8/non-disclosure behavior.**

Run: `dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter FullyQualifiedName~ProvisionSystemAdministratorTests`

Run: `dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --filter FullyQualifiedName~ProvisionerTests`

Expected: PASS, with the supplied email absent from console-output assertions.

- [x] **Step 6: Commit the provisioning slice.**

```powershell
git add backend/src/EosDashboards.Application/Provisioning backend/tools/EosDashboards.AdminProvisioner backend/tests/EosDashboards.Application.Tests/Provisioning backend/tests/EosDashboards.IntegrationTests/Provisioning
git commit -m "feat: provision approved Google sign-in links"
```

### Task 3: Issue a standard application session from a validated Google identity

**Files:**
- Create: `backend/src/EosDashboards.Application/Auth/GoogleSignIn.cs`
- Modify: `backend/src/EosDashboards.Application/Auth/AuthContracts.cs`
- Modify: `backend/tests/EosDashboards.Application.Tests/Auth/AuthenticationFakes.cs`
- Create: `backend/tests/EosDashboards.Application.Tests/Auth/GoogleSignInTests.cs`

**Interfaces:**
- Consumes `IUserRepository`, `IExternalIdentityLinkRepository`, `IUserSessionRepository`, `ISecretHasher`, `ISecureTokenGenerator`, `IAccessTokenIssuer`, `IAuditWriter`, and `IUnitOfWork`.
- Produces `GoogleIdentity(string Subject, string Email, bool EmailVerified)` and `GoogleSignIn.HandleAsync(GoogleIdentity, CancellationToken)`.
- Returns `GoogleSignInResult`, which contains the existing `AuthenticationResult` used by OTP verification when successful, so the API callback can set the established refresh cookie unchanged.

- [x] **Step 1: Write failing Google session-issuance tests.**

```csharp
[Fact]
public async Task Verified_prelinked_email_binds_subject_and_issues_standard_session()
{
    var result = await context.GoogleSignIn.HandleAsync(
        new GoogleIdentity("google-subject", "person@example.com", true),
        CancellationToken.None);

    Assert.Equal(GoogleSignInStatus.Succeeded, result.Status);
    Assert.Equal("google-subject", context.PendingLink.ProviderSubject);
    Assert.Single(context.Sessions.Sessions);
}
```

Add cases for an unverified email, unknown email, inactive user, and an existing subject link. After an explicit administrator email update, the stable bound subject remains authoritative. Assert each failure creates no session and audit records use event codes only.

- [x] **Step 2: Run the test to verify it fails.**

Run: `dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter FullyQualifiedName~GoogleSignInTests`

Expected: FAIL because `GoogleSignIn` and `GoogleIdentity` do not exist.

- [x] **Step 3: Implement the use case by reusing the current session semantics.**

```csharp
var link = await links.FindByProviderSubjectAsync(Google, identity.Subject, cancellationToken)
    ?? await links.FindPendingByProviderEmailAsync(Google, NormalizeEmail(identity.Email), cancellationToken);
if (!identity.EmailVerified || link is null) return Denied();

link.BindSubject(identity.Subject, now);
var session = UserSession.Create(user.Id, hasher.Hash(refreshCredential), now);
```

Require the resolved link's user to be active. Bind a pending subject and create the session in one `ExecuteSerializedTransactionAsync` operation so two first sign-ins cannot claim the same email. Return the same absolute-eight-hour expiry and ten-minute access-token behavior as `VerifyOtp`. Do not make an OTP challenge or call the SMS sender.

- [x] **Step 4: Run focused application tests.**

Run: `dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter "FullyQualifiedName~GoogleSignInTests|FullyQualifiedName~SessionLifecycleTests"`

Expected: PASS, confirming Google cannot bypass the active-user/link constraints but receives the ordinary secure session on success.

- [ ] **Step 5: Commit the session-issuance slice.**

```powershell
git add backend/src/EosDashboards.Application/Auth backend/tests/EosDashboards.Application.Tests/Auth
git commit -m "feat: issue sessions for linked Google identities"
```

### Task 4: Configure the server-owned Google OpenID Connect flow and API endpoints

**Files:**
- Modify: `backend/Directory.Packages.props`
- Modify: `backend/src/EosDashboards.Api/EosDashboards.Api.csproj`
- Create: `backend/src/EosDashboards.Api/Security/GoogleAuthenticationOptions.cs`
- Create: `backend/src/EosDashboards.Api/Security/GoogleAuthenticationOptionsValidator.cs`
- Create: `backend/src/EosDashboards.Api/Auth/GoogleAuthenticationEvents.cs`
- Modify: `backend/src/EosDashboards.Api/Program.cs`
- Modify: `backend/src/EosDashboards.Api/Auth/AuthEndpoints.cs`
- Modify: `backend/src/EosDashboards.Api/Auth/AuthContracts.cs`
- Modify: `backend/src/EosDashboards.Api/appsettings.json`
- Modify: `backend/src/EosDashboards.Api/appsettings.Development.json`
- Test: `backend/tests/EosDashboards.IntegrationTests/Api/AuthEndpointTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Security/GoogleAuthenticationOptionsTests.cs`

**Interfaces:**
- Consumes `GoogleSignIn.HandleAsync` from Task 3 and `RefreshCookieService.Set`.
- Registers named scheme `Google` without replacing the existing JWT default authentication scheme.
- Produces `GET /api/v1/auth/providers` returning `{ google: boolean }` and `GET /api/v1/auth/google/start`.
- The handler owns callback path `/api/v1/auth/google/callback` and redirects success to `/EosDashboards/`.

- [x] **Step 1: Write failing configuration and endpoint contract tests.**

```csharp
[Fact]
public async Task Providers_reports_google_only_when_complete_configuration_enables_it()
{
    var response = await client.GetFromJsonAsync<SignInProvidersResponse>(
        "/api/v1/auth/providers");

    Assert.True(response!.Google);
}

[Fact]
public void Enabled_google_requires_client_id_secret_and_exact_https_callback()
{
    var result = validator.Validate(null, new GoogleAuthenticationOptions
    {
        Enabled = true, ClientId = "id", ClientSecret = "secret", RedirectUri = "http://localhost/callback"
    });

    Assert.False(result.Succeeded);
}
```

Add tests that disabled configuration returns `google: false`, start redirects only when enabled, OpenAPI includes discovery/start but does not expose client configuration, and callback failures redirect safely without token-bearing URLs.

- [x] **Step 2: Run the tests to verify they fail.**

Run: `dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --filter "FullyQualifiedName~GoogleAuthenticationOptionsTests|FullyQualifiedName~AuthEndpointTests"`

Expected: FAIL because the options, provider-discovery response, and Google endpoints do not exist.

- [x] **Step 3: Add typed configuration and the OpenID Connect package.**

```xml
<PackageVersion Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="10.0.11" />
```

```csharp
public sealed class GoogleAuthenticationOptions
{
    public const string SectionName = "GoogleAuthentication";
    public bool Enabled { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? RedirectUri { get; init; }
}
```

Validate enabled configuration has nonblank client id/secret and exactly the configured absolute HTTPS callback; disabled configuration must not require values. Store no real value in tests or documentation.

- [x] **Step 4: Register the isolated OIDC scheme and callback events.**

```csharp
authentication.AddOpenIdConnect("Google", options =>
{
    options.Authority = "https://accounts.google.com";
    options.ClientId = google.ClientId;
    options.ClientSecret = google.ClientSecret;
    options.CallbackPath = "/api/v1/auth/google/callback";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.UsePkce = true;
    options.SaveTokens = false;
});
```

Keep JWT as the default scheme. Configure secure host-only correlation and nonce cookies, short lifetime, and the callback behavior required for Google top-level navigation. In `OnTokenValidated`, extract only `sub`, `email`, and `email_verified`, call `GoogleSignIn`, then on success call `RefreshCookieService.Set` and redirect to the UI application root. On every denied/cancelled/correlation/remote-failure path, clear temporary correlation state, write value-free audit data, and redirect to `/EosDashboards/?authError=google`.

- [x] **Step 5: Add discovery and start endpoints.**

```csharp
group.MapGet("/providers", (IOptions<GoogleAuthenticationOptions> options) =>
    Results.Ok(new SignInProvidersResponse(options.Value.Enabled)));

group.MapGet("/google/start", async (HttpContext context) =>
{
    await context.ChallengeAsync("Google", new AuthenticationProperties
    {
        RedirectUri = "/EosDashboards/"
    });
    return Results.Empty;
}).RequireRateLimiting("auth-sensitive");
```

Do not add CORS credentials or a client secret to frontend configuration. Keep the start endpoint anonymous but rate limited. The callback is handled by the OIDC middleware before endpoint routing and must not apply the XHR-only origin filter.

- [x] **Step 6: Run API configuration/contract tests and build.**

Run: `dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --filter "FullyQualifiedName~GoogleAuthenticationOptionsTests|FullyQualifiedName~AuthEndpointTests"`

Run: `dotnet build backend/EosDashboards.sln --no-restore -c Release`

Expected: PASS. The OpenAPI document lists capability/start endpoints only; no response, header, or log assertion contains tokens or secrets.

- [ ] **Step 7: Commit the OIDC and endpoint slice.**

```powershell
git add backend/Directory.Packages.props backend/src/EosDashboards.Api backend/tests/EosDashboards.IntegrationTests
git commit -m "feat: add secure Google authorization flow"
```

### Task 5: Add the responsive Google action and callback feedback to the sign-in UI

**Files:**
- Create: `frontend/src/features/auth/GoogleSignInButton.tsx`
- Modify: `frontend/src/features/auth/authApi.ts`
- Modify: `frontend/src/features/auth/authTypes.ts`
- Modify: `frontend/src/app/providers/AuthProvider.tsx`
- Modify: `frontend/src/features/auth/SignInPage.tsx`
- Modify: `frontend/src/features/auth/SignInPage.test.tsx`
- Create: `frontend/src/features/auth/GoogleSignInButton.test.tsx`
- Modify: `frontend/tests/e2e/auth-shell.spec.ts`

**Interfaces:**
- Consumes `GET /api/v1/auth/providers` returning `SignInProviders { google: boolean }`.
- `GoogleSignInButton` receives `available: boolean`, `busy: boolean`, and `onStart: () => void`.
- `onStart` navigates with `window.location.assign("/EosDashboardsApi/api/v1/auth/google/start")`; it never calls a JSON API with a credential.

- [ ] **Step 1: Write failing component tests for capability-driven rendering.**

```tsx
it("offers Google only when the API reports it enabled", () => {
  render(<GoogleSignInButton available busy={false} onStart={vi.fn()} />);

  expect(screen.getByRole("button", { name: "ورود با Google" })).toBeVisible();
});
```

Add tests that unavailable Google renders no inert/disabled control, busy state disables the action, and the action invokes only the navigation callback. Extend sign-in page tests to assert both methods remain visible without affecting credential submission.

- [ ] **Step 2: Run focused frontend tests to verify they fail.**

Run: `npm test -- --run src/features/auth/GoogleSignInButton.test.tsx src/features/auth/SignInPage.test.tsx`

Expected: FAIL because the Google component and provider capability state do not exist.

- [ ] **Step 3: Implement provider discovery and safe callback feedback.**

```ts
getSignInProviders: () => apiFetch<SignInProviders>("/api/v1/auth/providers", {}, false),
startGoogleSignIn: () => window.location.assign(
  `${import.meta.env.VITE_API_BASE_URL ?? ""}/api/v1/auth/google/start`,
),
```

Fetch providers once while unauthenticated; a discovery failure hides the Google action and does not block local sign-in. On page load, translate only the generic `authError=google` callback marker into the approved Persian error copy, then remove it with `history.replaceState` so it is not retained in the address bar or history.

- [ ] **Step 4: Compose the polished action in the existing sign-in surface.**

```tsx
{googleAvailable ? (
  <>
    <Divider>یا</Divider>
    <GoogleSignInButton available busy={busy} onStart={startGoogleSignIn} />
  </>
) : null}
```

Use the MUI Google icon, full-width button, deliberate hover/focus states, and existing responsive spacing. Keep form autofill, forgot-password padding, RTL direction, dark image readability, and all six palette contrast behavior intact.

- [ ] **Step 5: Add mocked browser coverage.**

```ts
await page.route("**/api/v1/auth/providers", route =>
  route.fulfill({ json: { google: true } }));
await page.getByRole("button", { name: "ورود با Google" }).click();
await expect(page).toHaveURL(/\/auth\/google\/start$/);
```

Add a second scenario where `authError=google` shows the Persian generic feedback and then removes the query string. Keep all existing mocked credential/OTP scenarios.

- [ ] **Step 6: Run frontend tests, typecheck, and browser suite.**

Run: `npm test -- --run src/features/auth/GoogleSignInButton.test.tsx src/features/auth/SignInPage.test.tsx`

Run: `npm run typecheck`

Run: `npx playwright test tests/e2e/auth-shell.spec.ts`

Expected: PASS, including theme-safe Google action presentation and safe callback feedback.

- [ ] **Step 7: Commit the UI slice.**

```powershell
git add frontend/src frontend/tests/e2e/auth-shell.spec.ts
git commit -m "feat: offer linked Google sign-in"
```

### Task 6: Complete configuration, operations guidance, final verification, and local publication

**Files:**
- Create: `docs/operations/google-sign-in.md`
- Modify: `docs/operations/authentication-runbook.md`
- Modify: `docs/operations/iis-deployment.md`
- Modify: `docs/project/architecture.md`
- Modify: `docs/project/requirements.md`
- Create: `docs/project/decisions/0007-linked-google-sign-in.md`
- Modify: `docs/project/current-state.md`
- Modify: `docs/project/roadmap.md`
- Modify: `AGENTS.md`

**Interfaces:**
- Consumes the migration, provisioner, typed API options, endpoint routes, and UI produced by Tasks 1–5.
- Produces repeatable, secret-free local setup instructions and canonical documentation of the approved external-authentication boundary.

- [ ] **Step 1: Write focused operations/documentation acceptance checks.**

```powershell
rg -n -S "nasimbaledi|ClientSecret.*[A-Za-z0-9]{16,}|GoogleAuthentication.*secret" docs scripts backend/tests
```

Expected: no personal email, client secret, token, or copied configuration value. Add an operations checklist test/inspection that requires the exact localhost callback URI and never instructs an HTTP Vite smoke test.

- [ ] **Step 2: Document the required local Google Cloud setup.**

Document: create a Google consent screen; create a Web OAuth client; add exactly `https://localhost/EosDashboardsApi/api/v1/auth/google/callback`; place ClientId/ClientSecret/RedirectUri in server-side API/IIS settings; provision the approved email through the hidden-entry tool; publish; then use only the IIS HTTPS UI for the authorized manual smoke check. Do not include actual values or screenshots containing them.

- [ ] **Step 3: Apply the database migration and configure the local server only after user-authorized Google Cloud setup.**

Run the reviewed EF migration and the normal elevated local publisher. Do not invoke the deprecated private-data helper for normal publication. Do not start a real Google authorization or write a real secret until the user explicitly confirms the Google Cloud client is ready.

- [ ] **Step 4: Run final automated verification.**

Run: `dotnet test backend/EosDashboards.sln -c Release`

Run: `npm run lint`

Run: `npm run typecheck`

Run: `npm test`

Run: `npx playwright test`

Run: `npm run build:iis`

Expected: PASS. Record the verification result without private values.

- [ ] **Step 5: Run one user-authorized IIS HTTPS smoke flow.**

Open `https://localhost/EosDashboards/`, select Google, authenticate only with the already linked account, confirm dashboard entry, refresh once to confirm the ordinary session lifecycle, then logout. Do not inspect cookies, tokens, password fields, OTPs, or Google account data. If Google Cloud client configuration is unavailable, stop before this step and report the exact configuration dependency.

- [ ] **Step 6: Commit documentation and integration state.**

```powershell
git add AGENTS.md docs
git commit -m "docs: record linked Google sign-in operation"
```

Do not merge or push unless the user requests it. If a merge is requested, first update all canonical documents and then push the destination branch as required by `AGENTS.md`.

## Plan self-review

- **Spec coverage:** Tasks 1–3 implement explicit pre-linking, stable-subject binding, active-user checks, and standard session issuance. Task 4 implements the server-side PKCE/callback/configuration/security boundary. Task 5 implements the responsive UX and safe feedback. Task 6 covers Google Cloud setup, local configuration, controlled smoke verification, and canonical documentation.
- **Placeholder scan:** No `TODO`, `TBD`, deferred implementation step, or unspecified interface remains in executable work. The named EF migration is additive and is generated from the actual model per repository convention.
- **Type consistency:** `ExternalIdentityProvider`, `ExternalIdentityLink`, `IExternalIdentityLinkRepository`, `GoogleIdentity`, `GoogleSignIn`, `GoogleAuthenticationOptions`, `SignInProviders`, and `GoogleSignInButton` are defined before consuming tasks and retain identical names throughout.
