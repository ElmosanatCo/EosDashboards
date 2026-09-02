# Local Credential Authentication Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [ ]) syntax for tracking.

**Goal:** Replace Windows/AD sign-in with local username/password plus mandatory SMS OTP, password recovery/change, and a polished Persian RTL authentication experience.

**Architecture:** Keep the existing User, OtpChallenge, UserSession, JWT, refresh-cookie, and audit backbone. Add a small password-hashing port and an OTP purpose discriminator; do not introduce a full identity framework. IIS permits anonymous API transport access while application endpoints enforce credentials, OTP, JWT authorization, rate limits, and the existing origin protections.

**Tech Stack:** .NET 10, EF Core 10, SQL Server, ASP.NET Core Minimal APIs, Microsoft.Extensions.Identity.Core password hashing, React 19, TypeScript, Material UI 9, Vazirmatn, Vite, Vitest, Playwright, IIS.

**Spec:** docs/superpowers/specs/2026-09-02-local-credential-authentication-design.md

## Global Constraints

- Passwords are 8 to 128 characters with no composition rule; never trim or normalize password plaintext.
- Passwords, OTPs, complete mobile numbers, tokens, and private configuration values must never enter source, logs, audit metadata, test output, or browser-visible errors.
- Apply an additive EF migration only. Do not remove existing organizational columns or records.
- Preserve the six-digit OTP, five-minute expiry, five-attempt limit, 60-second cooldown, existing JWT/session lifetime, refresh cookie, CORS, anti-forgery, and audit controls.
- Implement inline, with focused tests. Do not use subagents, repeated review loops, broad suites, or real SMS unless the user expressly authorizes the final smoke flow.
- Keep the separate API/UI IIS applications and pools. Change API IIS authentication only with the updated local deployment.

---

## File Structure

- Domain User owns username/password-hash state; Domain OtpChallenge owns its purpose.
- Application exposes an IPasswordHasher port and owns sign-in, recovery completion, password change, OTP purpose validation, and session revocation.
- Infrastructure maps credential fields and purpose, implements platform password hashing, and provides focused repository queries.
- API owns request records and routes; Program removes Negotiate and leaves JWT policies.
- Frontend auth components own credential, OTP, recovery, and change-password forms; AppHeader owns the authenticated user-menu entry point.
- The private IIS helper is the only repository script that reads the external private data file.

## Task 1: Domain Credential, OTP Purpose, and Session-Revocation State

**Files:**
- Create: backend/src/EosDashboards.Domain/Enums/OtpChallengePurpose.cs
- Modify: backend/src/EosDashboards.Domain/Entities/User.cs
- Modify: backend/src/EosDashboards.Domain/Entities/OtpChallenge.cs
- Modify: backend/src/EosDashboards.Domain/Enums/SessionRevocationReason.cs
- Modify: backend/tests/EosDashboards.Domain.Tests/UserTests.cs
- Modify: backend/tests/EosDashboards.Domain.Tests/OtpChallengeTests.cs
- Modify: backend/tests/EosDashboards.Domain.Tests/UserSessionTests.cs

**Interfaces:**
- Produces OtpChallengePurpose.SignIn and OtpChallengePurpose.PasswordReset.
- Produces User.Username, User.PasswordHash, and SetLocalCredentials(string username, string passwordHash, DateTimeOffset updatedAtUtc).
- Changes OtpChallenge.Create to accept OtpChallengePurpose and exposes Purpose.
- Produces SessionRevocationReason.PasswordChanged.

- [ ] **Step 1: Write the failing Domain tests**

~~~csharp
[Fact]
public void SetLocalCredentials_normalizes_username_but_preserves_hash()
{
    var user = CreateUser();
    user.SetLocalCredentials("  Admin.User  ", "versioned-hash", Now);

    Assert.Equal("ADMIN.USER", user.Username);
    Assert.Equal("versioned-hash", user.PasswordHash);
}

[Fact]
public void Password_reset_challenge_keeps_its_purpose()
{
    var challenge = OtpChallenge.Create(
        1, "token", HexHash, Now, Now.AddMinutes(5),
        OtpChallengePurpose.PasswordReset);

    Assert.Equal(OtpChallengePurpose.PasswordReset, challenge.Purpose);
}
~~~

- [ ] **Step 2: Run the focused test classes and confirm failure**

Run: dotnet test backend/tests/EosDashboards.Domain.Tests/EosDashboards.Domain.Tests.csproj --filter "FullyQualifiedName~UserTests|FullyQualifiedName~OtpChallengeTests|FullyQualifiedName~UserSessionTests" --no-restore

Expected: FAIL with missing local-credential and OTP-purpose symbols.

- [ ] **Step 3: Implement the minimum Domain state**

~~~csharp
public string? Username { get; private set; }
public string? PasswordHash { get; private set; }

public void SetLocalCredentials(string username, string passwordHash, DateTimeOffset updatedAtUtc)
{
    Username = NormalizeIdentifier(username, nameof(username));
    if (string.IsNullOrWhiteSpace(passwordHash))
        throw new ArgumentException("A password hash is required.", nameof(passwordHash));

    PasswordHash = passwordHash;
    UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
}
~~~

Use SignIn for every existing OtpChallenge creation call. Add PasswordChanged as the dedicated revocation reason. Do not put plaintext password-length validation in Domain.

- [ ] **Step 4: Run the same focused tests**

Run: dotnet test backend/tests/EosDashboards.Domain.Tests/EosDashboards.Domain.Tests.csproj --filter "FullyQualifiedName~UserTests|FullyQualifiedName~OtpChallengeTests|FullyQualifiedName~UserSessionTests" --no-restore

Expected: PASS.

- [ ] **Step 5: Commit**

~~~text
git add backend/src/EosDashboards.Domain backend/tests/EosDashboards.Domain.Tests
git commit -m "feat: add local credential domain state"
~~~

## Task 2: Password Hashing and Additive Persistence

**Files:**
- Create: backend/src/EosDashboards.Application/Abstractions/IPasswordHasher.cs
- Create: backend/src/EosDashboards.Infrastructure/Security/PasswordHasher.cs
- Modify: backend/src/EosDashboards.Infrastructure/EosDashboards.Infrastructure.csproj
- Modify: backend/src/EosDashboards.Infrastructure/DependencyInjection.cs
- Modify: backend/src/EosDashboards.Infrastructure/Persistence/Configurations/UserConfiguration.cs
- Modify: backend/src/EosDashboards.Infrastructure/Persistence/Configurations/OtpChallengeConfiguration.cs
- Modify: backend/src/EosDashboards.Application/Abstractions/IUserRepository.cs
- Modify: backend/src/EosDashboards.Application/Abstractions/IOtpChallengeRepository.cs
- Modify: backend/src/EosDashboards.Application/Abstractions/IUserSessionRepository.cs
- Modify: backend/src/EosDashboards.Infrastructure/Persistence/Repositories/UserRepository.cs
- Modify: backend/src/EosDashboards.Infrastructure/Persistence/Repositories/OtpChallengeRepository.cs
- Modify: backend/src/EosDashboards.Infrastructure/Persistence/Repositories/UserSessionRepository.cs
- Create: the EF Core-generated timestamped migration named LocalCredentialAuthentication under backend/src/EosDashboards.Infrastructure/Persistence/Migrations/
- Modify: backend/src/EosDashboards.Infrastructure/Persistence/Migrations/EosDashboardDbContextModelSnapshot.cs
- Modify: backend/tests/EosDashboards.IntegrationTests/Database/ModelMappingTests.cs
- Modify: backend/tests/EosDashboards.IntegrationTests/Security/SecurityPrimitiveTests.cs

**Interfaces:**
- IPasswordHasher.Hash(string password): string.
- IPasswordHasher.Verify(string password, string passwordHash): PasswordVerificationResult.
- PasswordVerificationResult: Failed, Succeeded, RehashNeeded.
- IUserRepository.FindByUsernameAsync(string normalizedUsername, CancellationToken).
- IOtpChallengeRepository.FindLatestActiveAsync(long userId, OtpChallengePurpose purpose, CancellationToken).
- IUserSessionRepository.GetActiveByUserIdAsync(long userId, DateTimeOffset nowUtc, CancellationToken).

- [ ] **Step 1: Write failing Infrastructure tests**

~~~csharp
[Fact]
public void Password_hasher_verifies_its_own_hash_and_rejects_a_different_password()
{
    var hash = _hasher.Hash("simple pass");
    Assert.NotEqual("simple pass", hash);
    Assert.NotEqual(PasswordVerificationResult.Failed, _hasher.Verify("simple pass", hash));
    Assert.Equal(PasswordVerificationResult.Failed, _hasher.Verify("other pass", hash));
}

[Fact]
public void User_mapping_includes_credential_fields()
{
    var properties = Context.Model.FindEntityType(typeof(User))!.GetProperties();
    Assert.Contains(properties, property => property.Name == nameof(User.Username));
    Assert.Contains(properties, property => property.Name == nameof(User.PasswordHash));
}
~~~

- [ ] **Step 2: Run only those test classes and confirm failure**

Run: dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --filter "FullyQualifiedName~SecurityPrimitiveTests|FullyQualifiedName~ModelMappingTests" --no-restore

Expected: FAIL with missing password-hasher or mapping symbols.

- [ ] **Step 3: Implement the infrastructure boundary**

~~~csharp
public interface IPasswordHasher
{
    string Hash(string password);
    PasswordVerificationResult Verify(string password, string passwordHash);
}

public enum PasswordVerificationResult
{
    Failed,
    Succeeded,
    RehashNeeded,
}
~~~

Use Microsoft.AspNetCore.Identity.PasswordHasher<object> behind the Infrastructure adapter and add Microsoft.Extensions.Identity.Core. Map nullable Username (maximum 256) and PasswordHash (maximum 1024), with a unique filtered index on non-null Username. Map OtpChallenge.Purpose as a required string (maximum 32) with default SignIn. Implement the repository queries with roles included for username lookup and tracked active sessions for revocation.

- [ ] **Step 4: Generate and inspect the additive migration, then run focused tests**

Run: dotnet ef migrations add LocalCredentialAuthentication --project backend/src/EosDashboards.Infrastructure/EosDashboards.Infrastructure.csproj --startup-project backend/src/EosDashboards.Api/EosDashboards.Api.csproj --no-build

Expected: one migration adding nullable credential columns, a filtered unique index, and non-null Purpose with SignIn default; no existing data update or deletion.

Run: dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --filter "FullyQualifiedName~SecurityPrimitiveTests|FullyQualifiedName~ModelMappingTests" --no-restore

Expected: PASS.

- [ ] **Step 5: Commit**

~~~text
git add backend/src/EosDashboards.Application/Abstractions backend/src/EosDashboards.Infrastructure backend/tests/EosDashboards.IntegrationTests
git commit -m "feat: persist local credentials and OTP purpose"
~~~

## Task 3: Credential Sign-in, Reset, and Password Change Use Cases

**Files:**
- Modify: backend/src/EosDashboards.Application/Auth/AuthContracts.cs
- Modify: backend/src/EosDashboards.Application/Auth/StartSignIn.cs
- Modify: backend/src/EosDashboards.Application/Auth/VerifyOtp.cs
- Create: backend/src/EosDashboards.Application/Auth/StartPasswordReset.cs
- Create: backend/src/EosDashboards.Application/Auth/CompletePasswordReset.cs
- Create: backend/src/EosDashboards.Application/Auth/ChangePassword.cs
- Modify: backend/src/EosDashboards.Application/Provisioning/ProvisionSystemAdministrator.cs
- Modify: backend/tests/EosDashboards.Application.Tests/Auth/AuthenticationFakes.cs
- Modify: backend/tests/EosDashboards.Application.Tests/Auth/StartSignInTests.cs
- Modify: backend/tests/EosDashboards.Application.Tests/Auth/VerifyOtpTests.cs
- Create: backend/tests/EosDashboards.Application.Tests/Auth/PasswordResetTests.cs
- Create: backend/tests/EosDashboards.Application.Tests/Auth/ChangePasswordTests.cs
- Modify: backend/tests/EosDashboards.Application.Tests/Provisioning/ProvisionSystemAdministratorTests.cs

**Interfaces:**
- StartSignInCommand(string Username, string Password, string? NetworkKey).
- StartPasswordResetCommand(string Username, string? NetworkKey).
- CompletePasswordResetCommand(string ChallengeToken, string Code, string NewPassword, string? NetworkKey).
- ChangePasswordCommand(long UserId, string CurrentPassword, string NewPassword).
- PasswordPolicy.Validate(string password) accepts only 8–128 character plaintext.
- ProvisionSystemAdministratorCommand includes Username and Password.

- [ ] **Step 1: Write failing Application tests**

~~~csharp
[Fact]
public async Task Valid_password_starts_a_sign_in_purpose_challenge()
{
    var result = await _startSignIn.HandleAsync(
        new StartSignInCommand("ADMIN", "simple pass", null), CancellationToken.None);

    Assert.Equal(StartSignInStatus.Succeeded, result.Status);
    Assert.Equal(OtpChallengePurpose.SignIn, Assert.Single(_challenges.Challenges).Purpose);
}

[Fact]
public async Task Reset_completion_consumes_reset_challenge_and_revokes_sessions()
{
    var result = await _completeReset.HandleAsync(
        new CompletePasswordResetCommand("token", "246810", "new pass", null), CancellationToken.None);

    Assert.Equal(PasswordResetStatus.Succeeded, result.Status);
    Assert.All(_sessions.Sessions, s => Assert.Equal(SessionRevocationReason.PasswordChanged, s.RevocationReason));
}
~~~

- [ ] **Step 2: Run focused Auth tests and confirm failure**

Run: dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter "FullyQualifiedName~StartSignInTests|FullyQualifiedName~VerifyOtpTests|FullyQualifiedName~PasswordResetTests|FullyQualifiedName~ChangePasswordTests" --no-restore

Expected: FAIL because credential commands, reset completion, and purpose checks do not exist.

- [ ] **Step 3: Implement use cases and fakes**

~~~csharp
if (!PasswordPolicy.IsValid(command.Password))
    return await DeniedWithAuditAsync(null, traceId, cancellationToken);

var user = await users.FindByUsernameAsync(NormalizeUsername(command.Username), cancellationToken);
if (user is null || !user.IsActive || user.PasswordHash is null ||
    passwordHasher.Verify(command.Password, user.PasswordHash) == PasswordVerificationResult.Failed)
    return await DeniedWithAuditAsync(user, traceId, cancellationToken);
~~~

Start a SignIn OTP only after a successful password check. VerifyOtp accepts SignIn challenges only. StartPasswordReset must return the same successful-looking response for unknown/inactive accounts: generate an opaque placeholder token and expiry, but send no SMS or create no record. CompletePasswordReset accepts PasswordReset only, validates 8–128 characters, consumes OTP atomically, replaces the hash, revokes every active session, audits safely, and issues no session. ChangePassword verifies current password, replaces the hash, revokes sessions, and audits safely. Replace hashes marked RehashNeeded during a successful password check. Extend test fakes for username lookup, password verification, and tracked session retrieval.

- [ ] **Step 4: Run focused Auth tests**

Run: dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter "FullyQualifiedName~StartSignInTests|FullyQualifiedName~VerifyOtpTests|FullyQualifiedName~PasswordResetTests|FullyQualifiedName~ChangePasswordTests|FullyQualifiedName~ProvisionSystemAdministratorTests" --no-restore

Expected: PASS.

- [ ] **Step 5: Commit**

~~~text
git add backend/src/EosDashboards.Application backend/tests/EosDashboards.Application.Tests
git commit -m "feat: add password sign-in and recovery"
~~~

## Task 4: API Routes, Private Provisioning, and IIS Mode

**Files:**
- Modify: backend/src/EosDashboards.Api/Auth/AuthContracts.cs
- Modify: backend/src/EosDashboards.Api/Auth/AuthEndpoints.cs
- Modify: backend/src/EosDashboards.Api/Program.cs
- Delete: backend/src/EosDashboards.Api/Security/WindowsIdentityReader.cs
- Modify: backend/tools/EosDashboards.AdminProvisioner/InteractiveInput.cs
- Modify: scripts/Configure-LocalIisFromPrivateData.ps1
- Modify: backend/tests/EosDashboards.IntegrationTests/Api/AuthEndpointTests.cs
- Modify: backend/tests/EosDashboards.IntegrationTests/Provisioning/ProvisionerTests.cs

**Interfaces:**
- Request records: SignInRequest, PasswordResetStartRequest, PasswordResetCompleteRequest, ChangePasswordRequest. Their ToString methods do not include input values.
- Routes: /auth/sign-in/challenges, /auth/challenges/{token}/verify, /auth/password-reset/challenges, /auth/password-reset/challenges/{token}/complete, and /auth/password.
- ActiveUser and SystemAdministrator JWT policies remain. WindowsIdentity policy and Negotiate registration are removed.

- [ ] **Step 1: Write failing endpoint tests**

~~~csharp
[Fact]
public async Task OpenApi_exposes_local_credential_routes_without_windows_route()
{
    var document = await _client.GetStringAsync("/openapi/v1.json");
    Assert.Contains("/api/v1/auth/sign-in/challenges", document, StringComparison.Ordinal);
    Assert.DoesNotContain("/api/v1/auth/challenges\"", document, StringComparison.Ordinal);
}

[Fact]
public async Task Anonymous_password_reset_start_returns_a_generic_response()
{
    var response = await _client.PostAsJsonAsync(
        "/api/v1/auth/password-reset/challenges", new { username = "missing" });

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
~~~

- [ ] **Step 2: Run focused endpoint/provisioner tests and confirm failure**

Run: dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --filter "FullyQualifiedName~AuthEndpointTests|FullyQualifiedName~ProvisionerTests" --no-restore

Expected: FAIL because local credential routes and installer inputs do not exist.

- [ ] **Step 3: Wire API, provisioning, and local IIS configuration**

~~~csharp
group.MapPost("/sign-in/challenges", StartSignInAsync).RequireRateLimiting("auth-sensitive");
group.MapPost("/password-reset/challenges", StartPasswordResetAsync).RequireRateLimiting("auth-sensitive");
group.MapPost("/password-reset/challenges/{challengeToken}/complete", CompletePasswordResetAsync).RequireRateLimiting("auth-sensitive");
group.MapPost("/password", ChangePasswordAsync)
    .RequireAuthorization("ActiveUser")
    .RequireRateLimiting("auth-sensitive");
~~~

Keep NoStoreEndpointFilter, safe problem responses, CORS, refresh, logout, and JWT policies. Remove Negotiate registration, WindowsIdentity policy, and test Windows handler. Expire refresh cookies after successful password change. Make InteractiveInput collect username and password, using ReadSecret for password. Make the private script require five values after Method in this exact order: username, password, first name, last name, mobile. Preserve the current Windows SID/name only to locate/update the existing administrator record; it is not a sign-in identity. Suppress child-process output carrying input. Update readiness to omit default credentials, enable API Anonymous Authentication, and disable API Windows Authentication only after configuration succeeds.

- [ ] **Step 4: Run focused endpoint/provisioner tests**

Run: dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --filter "FullyQualifiedName~AuthEndpointTests|FullyQualifiedName~ProvisionerTests" --no-restore

Expected: PASS and no real SMS request.

- [ ] **Step 5: Commit**

~~~text
git add backend/src/EosDashboards.Api backend/tools/EosDashboards.AdminProvisioner scripts backend/tests/EosDashboards.IntegrationTests
git rm backend/src/EosDashboards.Api/Security/WindowsIdentityReader.cs
git commit -m "feat: expose local credential authentication API"
~~~

## Task 5: Redesigned Sign-in and Recovery UI

**Files:**
- Modify: frontend/src/features/auth/authTypes.ts
- Modify: frontend/src/features/auth/authApi.ts
- Modify: frontend/src/app/providers/AuthProvider.tsx
- Modify: frontend/src/features/auth/SignInPage.tsx
- Create: frontend/src/features/auth/CredentialForm.tsx
- Create: frontend/src/features/auth/PasswordRecoveryForm.tsx
- Modify: frontend/src/features/auth/OtpForm.tsx
- Create: frontend/src/features/auth/SignInPage.test.tsx
- Create: frontend/src/features/auth/PasswordRecoveryForm.test.tsx
- Modify: frontend/src/features/auth/OtpForm.test.tsx
- Modify: frontend/src/index.css
- Modify: frontend/tests/e2e/auth-shell.spec.ts

**Interfaces:**
- authApi.startSignIn(username, password), startPasswordReset(username), completePasswordReset(token, code, newPassword), verifyOtp(token, code).
- SignInPage mode: signIn, signInOtp, passwordReset, passwordResetOtp.
- Raw passwords and OTP stay in component state only.

- [ ] **Step 1: Write failing focused component tests**

~~~tsx
it("submits username and password before showing OTP", async () => {
  const user = userEvent.setup();
  const onStartSignIn = vi.fn();
  render(<SignInPage mode="signIn" challenge={null} busy={false} onStartSignIn={onStartSignIn} onVerifyOtp={vi.fn()} onStartPasswordReset={vi.fn()} onCompletePasswordReset={vi.fn()} onBack={vi.fn()} />);
  await user.type(screen.getByLabelText("نام کاربری"), "admin");
  await user.type(screen.getByLabelText("رمز عبور"), "simple pass");
  await user.click(screen.getByRole("button", { name: "ورود و دریافت کد تأیید" }));
  expect(onStartSignIn).toHaveBeenCalledWith("admin", "simple pass");
});

it("opens recovery without claiming that an account exists", async () => {
  render(<SignInPage mode="signIn" challenge={null} busy={false} onStartSignIn={vi.fn()} onVerifyOtp={vi.fn()} onStartPasswordReset={vi.fn()} onCompletePasswordReset={vi.fn()} onBack={vi.fn()} />);
  await userEvent.setup().click(screen.getByRole("button", { name: "فراموشی رمز" }));
  expect(screen.getByText("بازیابی رمز عبور")).toBeVisible();
});
~~~

- [ ] **Step 2: Run authentication component tests and confirm failure**

Run: npm --prefix frontend test -- --run src/features/auth/SignInPage.test.tsx src/features/auth/PasswordRecoveryForm.test.tsx src/features/auth/OtpForm.test.tsx

Expected: FAIL because credential/recovery controls and callbacks do not exist.

- [ ] **Step 3: Implement the visual composition and state flow**

~~~tsx
<Box sx={{ minHeight: "100dvh", display: "grid",
  gridTemplateColumns: { md: "minmax(340px, .9fr) minmax(420px, 1.1fr)" } }}>
  <Box component="aside" sx={{ bgcolor: "primary.dark", display: { xs: "none", md: "flex" } }} />
  <Box component="main" sx={{ display: "grid", placeItems: "center", p: { xs: 2, sm: 4 } }} />
</Box>
~~~

Keep EOS SVG unmodified. On wide screens render the navy brand panel with controlled neutral logo surface, company name, concise product statement, and a restrained teal CSS accent. Render a quiet light form surface with username/password fields, password visibility control, prominent teal action, recovery link, and text-plus-icon status feedback. On small screens collapse branding into a compact header. Adapt OtpForm for a purpose-aware title and optional new-password field during reset completion. Preserve Persian digits, countdown, keyboard support, visible focus, autocomplete hints, and no password/OTP in error text. Update mocked Playwright flow to submit credentials, complete OTP, and return to the credential form on logout.

- [ ] **Step 4: Run focused UI tests and one mocked browser flow**

Run: npm --prefix frontend test -- --run src/features/auth/SignInPage.test.tsx src/features/auth/PasswordRecoveryForm.test.tsx src/features/auth/OtpForm.test.tsx

Expected: PASS.

Run: npm --prefix frontend run e2e -- --grep "local credential OTP"

Expected: one PASS using route mocks and no SMS.

- [ ] **Step 5: Commit**

~~~text
git add frontend/src/features/auth frontend/src/app/providers/AuthProvider.tsx frontend/src/index.css frontend/tests/e2e/auth-shell.spec.ts
git commit -m "feat: redesign local credential sign-in"
~~~

## Task 6: Authenticated Password Change UI

**Files:**
- Create: frontend/src/features/auth/ChangePasswordDialog.tsx
- Create: frontend/src/features/auth/ChangePasswordDialog.test.tsx
- Modify: frontend/src/app/providers/AuthProvider.tsx
- Modify: frontend/src/layout/AppHeader.tsx
- Modify: frontend/src/App.test.tsx

**Interfaces:**
- AuthContextValue.changePassword(currentPassword: string, newPassword: string): Promise<void>.
- ChangePasswordDialog props: open, busy, error, onClose, onSubmit(currentPassword, newPassword).

- [ ] **Step 1: Write the failing dialog test**

~~~tsx
it("requires matching 8-character new password before submission", async () => {
  render(<ChangePasswordDialog open busy={false} onClose={vi.fn()} onSubmit={submit} />);
  await user.type(screen.getByLabelText("رمز فعلی"), "old pass");
  await user.type(screen.getByLabelText("رمز جدید"), "new pass");
  await user.type(screen.getByLabelText("تکرار رمز جدید"), "different");
  expect(screen.getByRole("button", { name: "ثبت رمز جدید" })).toBeDisabled();
});
~~~

- [ ] **Step 2: Run it and confirm failure**

Run: npm --prefix frontend test -- --run src/features/auth/ChangePasswordDialog.test.tsx

Expected: FAIL because the dialog does not exist.

- [ ] **Step 3: Implement user-menu action and forced local logout**

~~~tsx
await authApi.changePassword(currentPassword, newPassword);
authTokenStore.clear();
setUser(null);
setChallenge(null);
onLogout?.();
~~~

Add a تغییر رمز menu item alongside logout. The dialog has current password, new password, confirmation, visibility controls, local length validation, and safe Persian error copy. On success it closes, clears tabs/session state, and returns to local credential sign-in without attempting refresh.

- [ ] **Step 4: Run focused frontend checks**

Run: npm --prefix frontend test -- --run src/features/auth/ChangePasswordDialog.test.tsx src/App.test.tsx

Expected: PASS.

- [ ] **Step 5: Commit**

~~~text
git add frontend/src/features/auth/ChangePasswordDialog.tsx frontend/src/features/auth/ChangePasswordDialog.test.tsx frontend/src/app/providers/AuthProvider.tsx frontend/src/layout/AppHeader.tsx frontend/src/App.test.tsx
git commit -m "feat: add password change dialog"
~~~

## Task 7: Focused Checkpoint Verification and Documentation

**Files:**
- Modify: docs/project/current-state.md
- Modify: docs/project/architecture.md only if implementation differs from approved design
- Modify: docs/project/requirements.md only if implementation differs from approved design

**Interfaces:** No new runtime interface. This task records only verified, non-sensitive state.

- [ ] **Step 1: Run each focused test group once**

Run: dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter "FullyQualifiedName~Auth|FullyQualifiedName~ProvisionSystemAdministrator" --no-restore

Expected: PASS.

Run: dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --filter "FullyQualifiedName~AuthEndpointTests|FullyQualifiedName~ModelMappingTests|FullyQualifiedName~SecurityPrimitiveTests|FullyQualifiedName~ProvisionerTests" --no-restore

Expected: PASS.

Run: npm --prefix frontend run typecheck

Expected: exit 0.

Run: npm --prefix frontend test -- --run src/features/auth/SignInPage.test.tsx src/features/auth/PasswordRecoveryForm.test.tsx src/features/auth/ChangePasswordDialog.test.tsx src/features/auth/OtpForm.test.tsx

Expected: PASS.

Run: npm --prefix frontend run build

Expected: exit 0.

- [ ] **Step 2: Inspect migration and removed Windows implementation**

Run: rg -n --glob "!docs/**" --glob "!**/bin/**" --glob "!**/obj/**" "WindowsIdentity|AddNegotiate|RequireAuthorization\\(\\\"WindowsIdentity\\\"" backend frontend scripts

Expected: no active implementation matches.

Run: git diff HEAD~1 -- backend/src/EosDashboards.Infrastructure/Persistence/Migrations

Expected: additive credential/purpose columns and index only, with no personal or secret value.

- [ ] **Step 3: Update documentation and commit**

Record the exact focused checks, that automated verification sent no real SMS, and the pending local deployment/smoke boundary in current-state.md.

Run: git diff --check

Expected: no whitespace errors.

~~~text
git add docs/project
git commit -m "docs: record local credential verification"
~~~

## Task 8: Local IIS Deployment and One Authorized Smoke Flow

**Files:**
- Modify if implementation requires it: scripts/Configure-LocalIisFromPrivateData.ps1
- Modify: docs/project/current-state.md

**Interfaces:**
- The private file supplies existing database/SMS values and five installer-only values in the documented order. It never enters repository files or command output.
- Local endpoints remain https://localhost/EosDashboards/ and https://localhost/EosDashboardsApi/health/{live,ready}.

- [ ] **Step 1: Build deployable artifacts once**

Run: dotnet publish backend/src/EosDashboards.Api/EosDashboards.Api.csproj -c Release --no-restore

Expected: API publish succeeds and contains web.config.

Run: npm --prefix frontend run build

Expected: UI build succeeds with /EosDashboards/ base path.

- [ ] **Step 2: Apply private local configuration once**

From an elevated PowerShell session run:

~~~text
./scripts/Configure-LocalIisFromPrivateData.ps1 -PrivateDataFile "D:\\Workspaces\\ChatGpt\\Private Data For AI Projects\\EosDashboards\\Data.txt" -ProvisionAdministratorFromPrivateData
~~~

Expected: the helper applies the additive database migration, provisions the existing administrator's credentials without echoing them, enables API Anonymous Authentication, disables API Windows Authentication, and receives readiness 200 without default Windows credentials.

- [ ] **Step 3: Run one browser smoke flow after explicit action-time authorization**

Verify in Chrome: credential page renders; valid password sends sign-in OTP; authorized OTP reaches shell; refresh keeps session; logout returns to credential form; password change forces credential form; recovery accepts an authorized OTP and permits next sign-in. Stop after this one flow. Do not resend/retry an SMS without a new explicit user confirmation.

- [ ] **Step 4: Record outcome, commit, and push**

Update current-state.md with release version, IIS authentication mode, smoke outcome, and any non-sensitive blocker. Do not record username, password, OTP, phone, database detail, or SMS endpoint.

Run: git diff --check

Expected: no whitespace errors.

~~~text
git add AGENTS.md docs/project scripts backend frontend
git commit -m "feat: deploy local credential authentication"
git push origin feature/initial-authentication-shell
~~~

## Plan Self-Review

- Spec coverage: Tasks 1–4 implement local credentials, isolated OTP purpose, recovery, change-password, revocation, private provisioning, API protection, and IIS transition. Tasks 5–6 implement the approved Persian RTL visual direction. Tasks 7–8 provide focused verification, local deployment, and an explicitly authorized browser flow.
- Cost control: each task uses narrow tests. The only build checkpoint is Task 7; the only real-SMS/browser operation is Task 8 and needs explicit authorization.
- Security: reset is purpose-isolated and generic for unknown accounts; password changes revoke sessions; no secret appears in tracked data; existing JWT, refresh, origin, and anti-forgery defenses remain.
- Type consistency: every later route, method, component callback, and repository query is defined by an earlier producing task.
