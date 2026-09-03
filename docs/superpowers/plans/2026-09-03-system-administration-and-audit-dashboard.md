# System Administration and Audit Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver server-authorized user and department administration plus a
truthful System Administrator audit dashboard and audit-history workspace.

**Architecture:** Domain aggregates protect user/role/department state and the
last-administrator invariant. Application commands perform mutations,
revocations, and immutable audit writes in one transaction; Infrastructure
persists concurrency, projections, indexes, and query paging. The React
workspace consumes safe REST projections through role-protected targets; the
server remains authoritative.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, EF Core 10 / SQL Server,
React 19, TypeScript, Material UI 9, TanStack Query, Vitest, Playwright.

**Spec:** `docs/superpowers/specs/2026-09-03-system-administration-and-audit-dashboard-design.md`

## Global Constraints

- Require the existing `SystemAdministrator` policy on every new API route;
  client target filtering is discoverability only.
- Retain the four fixed role codes. Do not implement custom roles, granular
  permissions, Google-link administration, exports, alerts, or audit retention.
- Persist application time with the local server clock in `datetime2(3)` and
  use no `Utc` field names or Tehran conversion.
- Encrypt mobile input through `IMobileProtector`; return only masked values;
  never place passwords, OTPs, or full mobile numbers in API projections, audit
  metadata, tests' output, logs, or browser storage.
- User profile changes, role/unit changes, activation changes, and administrator
  resets revoke the target's active sessions but not the acting administrator's
  own session. Protect the last active System Administrator.
- Keep users permanently; allow only activation/deactivation. Every user has a
  department, and every active user has at least one fixed role.
- Follow the approved dark-default Persian RTL workforce-operations UI system;
  review desktop and phone rendering and add explicit loading, empty, denied,
  conflict, and error states.
- Use focused red/green tests during tasks and run broad verification once at
  integration and publication checkpoints.

---

### Task 1: Model mutable account and department invariants

**Files:**
- Modify: `backend/src/EosDashboards.Domain/Entities/User.cs`
- Modify: `backend/src/EosDashboards.Domain/Entities/Department.cs`
- Modify: `backend/src/EosDashboards.Domain/Entities/UserSession.cs`
- Modify: `backend/src/EosDashboards.Domain/Enums/SessionRevocationReason.cs`
- Test: `backend/tests/EosDashboards.Domain.Tests/UserTests.cs`
- Test: `backend/tests/EosDashboards.Domain.Tests/DepartmentTests.cs`
- Test: `backend/tests/EosDashboards.Domain.Tests/UserSessionTests.cs`

**Interfaces:**
- Produces `User.SetTemporaryLocalCredentials`, `User.UpdateProfile`,
  `User.UpdateOrganizationalId`, `User.ReplaceRoles`, `User.Activate`, `User.Deactivate`, and
  `User.CompleteTemporaryPasswordChange`.
- Produces `User.RowVersion` and `Department.RowVersion` as concurrency values
  mapped by Task 2; neither property has a public setter.
- Produces `Department.Rename`, `Department.AssignParent`, and
  `Department.MakeIndependent`; parent checks reject a child as a parent.
- Produces `SessionRevocationReason.AdministrativeChange` for target-session
  invalidation.

- [ ] **Step 1: Write failing aggregate tests.**

  ```csharp
  [Fact]
  public void Temporary_credentials_require_password_change_and_roles_can_be_replaced()
  {
      var user = CreateUser();
      user.SetTemporaryLocalCredentials("42", "hash", Now);
      user.ReplaceRoles([1, 2], Now);
      Assert.True(user.MustChangePassword);
      Assert.Equal([1L, 2L], user.UserRoles.Select(x => x.RoleId));
  }

  [Fact]
  public void Child_department_cannot_become_a_parent()
  {
      var root = Department.CreateRoot("ریشه", Now);
      var child = Department.CreateChild(root, "فرزند", Now);
      Assert.Throws<InvalidOperationException>(() => root.AssignParent(child, Now));
  }
  ```

- [ ] **Step 2: Run the focused Domain tests and confirm they fail for the absent methods/flags.**

  Run: `dotnet test backend/tests/EosDashboards.Domain.Tests/EosDashboards.Domain.Tests.csproj -c Release --filter "FullyQualifiedName~UserTests|FullyQualifiedName~DepartmentTests|FullyQualifiedName~UserSessionTests"`

  Expected: FAIL because the new state transitions do not yet exist.

- [ ] **Step 3: Implement the smallest explicit transitions.**

  ```csharp
  public void SetTemporaryLocalCredentials(string username, string passwordHash, DateTime updatedAt)
  {
      SetLocalCredentials(username, passwordHash, updatedAt);
      MustChangePassword = true;
  }

  public void CompleteTemporaryPasswordChange(string passwordHash, DateTime updatedAt)
  {
      SetLocalCredentials(Username!, passwordHash, updatedAt);
      MustChangePassword = false;
  }
  ```

  Trim/validate profile and department names, preserve the current two-level
  checks, reject an empty replacement-role collection for an active user, add
  a private-set `byte[] RowVersion` property to each mutable aggregate, and do
  not expose a collection setter.

- [ ] **Step 4: Run the same focused tests and confirm they pass.**

- [ ] **Step 5: Commit the aggregate-only change.**

  ```text
  git add backend/src/EosDashboards.Domain backend/tests/EosDashboards.Domain.Tests
  git commit -m "feat: model administration account invariants"
  ```

### Task 2: Add persistence, concurrency, and administration query boundaries

**Files:**
- Modify: `backend/src/EosDashboards.Application/Abstractions/IUserRepository.cs`
- Modify: `backend/src/EosDashboards.Application/Abstractions/IDepartmentRepository.cs`
- Modify: `backend/src/EosDashboards.Application/Abstractions/IUserSessionRepository.cs`
- Add: `backend/src/EosDashboards.Application/Abstractions/IAuditLogReader.cs`
- Add: `backend/src/EosDashboards.Application/Administration/AdministrationContracts.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Configurations/DepartmentConfiguration.cs`
- Add: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/AuditLogReader.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/UserRepository.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/DepartmentRepository.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/UserSessionRepository.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/DependencyInjection.cs`
- Add: a generated EF migration named `AddSystemAdministrationAndAuditDashboard`
  under `backend/src/EosDashboards.Infrastructure/Persistence/Migrations/`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Migrations/EosDashboardDbContextModelSnapshot.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Database/ModelMappingTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Database/RepositoryTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Database/DatabaseConstraintTests.cs`

**Interfaces:**
- Consumes the Task 1 aggregate API.
- Produces `AdministrationUserListItem`, `AdministrationUserDetail`,
  `DepartmentListItem`, `AuditLogListItem`, `AuditLogQuery`, and
  `SystemAdministrationSummary` records in Application.
- Produces `PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize,
  long TotalCount)` in `AdministrationContracts.cs`.
- Adds tracked retrieval for mutations and no-tracking server-paged projections
  for reads. `IAuditLogReader` returns only safe projections, never `AuditLog`
  entities to the API.

- [ ] **Step 1: Write focused SQL tests for the database contract.**

  ```csharp
  [Fact]
  public async Task Department_name_is_unique_and_mutable_records_have_rowversion()
  {
      await AddDepartmentAsync("منابع انسانی");
      await Assert.ThrowsAsync<DbUpdateException>(() => AddDepartmentAsync("منابع انسانی"));
      Assert.NotNull(RequiredProperty<User>(nameof(User.RowVersion)));
  }

  [Fact]
  public async Task Audit_reader_pages_safe_actor_and_subject_projections()
  {
      var page = await reader.QueryAsync(new AuditLogQuery(from, to, null, null, null, 1, 20), default);
      Assert.DoesNotContain("091", JsonSerializer.Serialize(page));
  }
  ```

- [ ] **Step 2: Run the focused SQL tests and confirm the missing index,
  row-version, and reader contract fail.**

  Run: `dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~ModelMappingTests|FullyQualifiedName~RepositoryTests|FullyQualifiedName~DatabaseConstraintTests"`

- [ ] **Step 3: Implement safe tracked/no-tracking repositories and generate the migration.**

  ```csharp
  Task<User?> GetForUpdateAsync(long id, CancellationToken cancellationToken);
  Task<PagedResult<AdministrationUserListItem>> QueryAsync(AdministrationUserQuery query, CancellationToken cancellationToken);
  Task<IReadOnlyList<DepartmentListItem>> GetTreeAsync(CancellationToken cancellationToken);
  Task<SystemAdministrationSummary> GetSummaryAsync(DateTime since, DateTime now, CancellationToken cancellationToken);
  ```

  Configure `MustChangePassword` as required with default `false`, configure
  `RowVersion` on users/departments, add a unique department-name index, and
  add audit/session composite indexes required by the exact date/filter and
  active-session queries. Generate with:

  ```text
  dotnet ef migrations add AddSystemAdministrationAndAuditDashboard --project backend/src/EosDashboards.Infrastructure --startup-project backend/src/EosDashboards.Api --output-dir Persistence/Migrations
  ```

  Review the migration: it must add only schema/index/default data changes and
  must not rewrite personal values.

- [ ] **Step 4: Run the focused integration tests and inspect migration SQL.**

  Run: `dotnet ef migrations script --idempotent --project backend/src/EosDashboards.Infrastructure --startup-project backend/src/EosDashboards.Api -o backend/artifacts/system-administration-idempotent.sql`

  Expected: PASS; the reviewed script creates the unique/index/concurrency
  structures without PII literals.

- [ ] **Step 5: Commit persistence and migration.**

  ```text
  git add backend/src/EosDashboards.Application backend/src/EosDashboards.Infrastructure backend/tests/EosDashboards.IntegrationTests
  git commit -m "feat: persist administration query model"
  ```

### Task 3: Implement user-management commands and forced temporary-password completion

**Files:**
- Add: `backend/src/EosDashboards.Application/Administration/ManageUsers.cs`
- Add: `backend/src/EosDashboards.Application/Administration/AdministrationAuditEvents.cs`
- Modify: `backend/src/EosDashboards.Application/Auth/AuthContracts.cs`
- Modify: `backend/src/EosDashboards.Application/Auth/VerifyOtp.cs`
- Modify: `backend/src/EosDashboards.Application/Auth/RefreshSession.cs`
- Modify: `backend/src/EosDashboards.Application/Auth/ChangePassword.cs`
- Modify: `backend/src/EosDashboards.Api/Program.cs`
- Test: `backend/tests/EosDashboards.Application.Tests/Administration/ManageUsersTests.cs`
- Test: `backend/tests/EosDashboards.Application.Tests/Auth/ChangePasswordTests.cs`
- Test: `backend/tests/EosDashboards.Application.Tests/Auth/VerifyOtpTests.cs`

**Interfaces:**
- Produces `CreateUserCommand`, `UpdateUserCommand`, `SetUserActiveCommand`,
  `ResetUserPasswordCommand`, `ManageUserStatus`, and `MustChangePassword` in
  the authenticated-user projection.
- `ManageUsers` receives actor ID from the API, validates the four role IDs,
  encrypts replacement mobile values, uses `GetForUpdateAsync`, and writes
  `UserCreated`, `UserUpdated`, `UserRolesChanged`, `UserDepartmentChanged`,
  `UserActivated`, `UserDeactivated`, or `UserPasswordReset` audit codes.

- [x] **Step 1: Write failing Application tests for each sensitive command.**

  ```csharp
  [Fact]
  public async Task Create_defaults_username_to_personnel_code_and_requires_a_password_change()
  {
      var result = await sut.CreateAsync(actorId, new CreateUserCommand(
          "124", "کاربر نمونه", "نام", "خانوادگی", "+989111111111", null,
          "temporary-password", 1, [2]), default);
      Assert.Equal("124", result.User!.Username);
      Assert.True(result.User.MustChangePassword);
      Assert.Equal("UserCreated", Assert.Single(audit.Records).EventCode);
  }

  [Fact]
  public async Task Deactivating_the_last_active_system_administrator_is_rejected_without_audit_success()
  {
      var result = await sut.SetActiveAsync(actorId, new SetUserActiveCommand(lastAdminId, false, rowVersion), default);
      Assert.Equal(ManageUserStatus.LastSystemAdministrator, result.Status);
  }
  ```

- [x] **Step 2: Run the new Application tests and confirm command types are absent.**

  Run: `dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj -c Release --filter "FullyQualifiedName~ManageUsersTests|FullyQualifiedName~ChangePasswordTests|FullyQualifiedName~VerifyOtpTests"`

- [x] **Step 3: Implement command handling in one serialized transaction per mutation.**

  ```csharp
  public Task<ManageUserResult> UpdateAsync(
      long actorUserId, UpdateUserCommand command, CancellationToken cancellationToken);

  private async Task RevokeTargetSessionsAsync(long actorUserId, long targetUserId, DateTime now)
  {
      if (actorUserId == targetUserId) return;
      foreach (var session in await sessions.GetActiveByUserIdAsync(targetUserId, now, CancellationToken.None))
          session.Revoke(SessionRevocationReason.AdministrativeChange, now);
  }
  ```

  Require a department for every create/update, roles for active users, a
  unique personnel code and username, and an expected row version. A reset
  hashes the supplied temporary password and sets `MustChangePassword`; a
  regular password change clears that flag only after the current password
  verifies. Preserve the existing post-password-change logout semantics.

- [x] **Step 4: Project `mustChangePassword` through OTP verification and
  refresh, then rerun the focused tests.**

  Expected: PASS; an authenticated temporary-password user can receive a
  session but cannot enter the workspace before changing the password.

- [ ] **Step 5: Commit user commands and auth projection.**

  ```text
  git add backend/src/EosDashboards.Application backend/src/EosDashboards.Api/Program.cs backend/tests/EosDashboards.Application.Tests
  git commit -m "feat: add audited user administration"
  ```

### Task 4: Implement department, audit-history, and system-summary use cases

**Files:**
- Add: `backend/src/EosDashboards.Application/Administration/ManageDepartments.cs`
- Add: `backend/src/EosDashboards.Application/Administration/GetAuditHistory.cs`
- Add: `backend/src/EosDashboards.Application/Administration/GetSystemAdministrationDashboard.cs`
- Test: `backend/tests/EosDashboards.Application.Tests/Administration/ManageDepartmentsTests.cs`
- Test: `backend/tests/EosDashboards.Application.Tests/Administration/AuditDashboardTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Database/RepositoryTests.cs`

**Interfaces:**
- Produces `CreateDepartmentCommand`, `UpdateDepartmentCommand`,
  `DeleteDepartmentCommand`, `DepartmentOperationStatus`, `AuditHistoryQuery`,
  `AuditHistoryPage`, and `SystemAdministrationDashboard`.
- Audit dashboard window is `[clock.Now.AddHours(-24), clock.Now)`; active
  session count uses `RevokedAt == null && CreatedAt <= now && now < ExpiresAt`.

- [ ] **Step 1: Write red tests for hierarchy/deletion and exact dashboard windows.**

  ```csharp
  [Fact]
  public async Task Delete_rejects_a_department_with_users_or_children()
  {
      var result = await sut.DeleteAsync(actorId, new DeleteDepartmentCommand(parentId, rowVersion), default);
      Assert.Equal(DepartmentOperationStatus.NotEmpty, result.Status);
  }

  [Fact]
  public async Task Dashboard_counts_only_successful_and_failed_security_events_in_the_last_24_hours()
  {
      var dashboard = await sut.HandleAsync(default);
      Assert.Equal(2, dashboard.SuccessfulSignIns);
      Assert.Equal(1, dashboard.FailedSecurityAttempts);
      Assert.Equal(3, dashboard.UsersWithActiveSessions);
  }
  ```

- [ ] **Step 2: Run the focused tests and confirm the operations/query handlers are absent.**

  Run: `dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj -c Release --filter "FullyQualifiedName~ManageDepartmentsTests|FullyQualifiedName~AuditDashboardTests"`

- [ ] **Step 3: Implement the exact rules and safe audit query.**

  Reject duplicate names, a child chosen as parent, a parent with children
  becoming a child, and deletion when user/child counts are nonzero. Write
  `DepartmentCreated`, `DepartmentUpdated`, and `DepartmentDeleted` events.
  Restrict `AuditHistoryQuery` to requested 7-day, 30-day, or supplied local
  date range; require `from < to`, cap a page at 100 rows, and support event,
  actor, subject, and success filters. Centralize the finite failed-security
  event-code list rather than using a text match.

- [ ] **Step 4: Run focused Application and SQL tests.**

  Run: `dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj -c Release --filter "FullyQualifiedName~ManageDepartmentsTests|FullyQualifiedName~AuditDashboardTests"`

  Run: `dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~RepositoryTests"`

- [ ] **Step 5: Commit department and read-model behavior.**

  ```text
  git add backend/src/EosDashboards.Application/Administration backend/tests/EosDashboards.Application.Tests backend/tests/EosDashboards.IntegrationTests
  git commit -m "feat: add department and audit dashboard services"
  ```

### Task 5: Expose System Administrator REST endpoints and API security tests

**Files:**
- Add: `backend/src/EosDashboards.Api/Administration/AdministrationContracts.cs`
- Add: `backend/src/EosDashboards.Api/Administration/AdministrationEndpoints.cs`
- Modify: `backend/src/EosDashboards.Api/Program.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Api/AdministrationEndpointTests.cs`
- Modify: `backend/tests/EosDashboards.IntegrationTests/Api/AuthEndpointTests.cs`

**Interfaces:**
- Maps `/api/v1/administration/dashboard`, `/users`, `/users/{id}`,
  `/users/{id}/active`, `/users/{id}/password-reset`, `/roles`, `/departments`,
  `/departments/{id}`, and `/audit-logs`.
- Every endpoint requires `SystemAdministrator`, is `no-store`, reads actor ID
  through `SessionAuthorizationHandler.TryReadId`, and translates only defined
  command status codes to safe `ApiResults.Problem` codes.

- [ ] **Step 1: Write API red tests for authorization and safe payloads.**

  ```csharp
  [Fact]
  public async Task Audit_history_is_forbidden_to_an_authenticated_non_administrator()
  {
      var response = await SendAsActiveDepartmentManagerAsync(HttpMethod.Get, "/api/v1/administration/audit-logs");
      Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task User_create_returns_masked_mobile_and_no_password_value()
  {
      var response = await PostAsAdministratorAsync("/api/v1/administration/users", request);
      Assert.DoesNotContain("091", await response.Content.ReadAsStringAsync());
  }
  ```

- [ ] **Step 2: Run the new API test class and confirm routes are absent.**

  Run: `dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AdministrationEndpointTests"`

- [ ] **Step 3: Map endpoint request/response records and exact failure codes.**

  Use `personnel_code_conflict`, `username_conflict`, `department_name_conflict`,
  `department_not_empty`, `department_hierarchy_invalid`, `last_system_administrator`,
  and `concurrency_conflict`. Return 400 for malformed values, 404 for an
  absent requested entity, 409 for conflicts, and retain 401/403 behavior from
  the existing authorization pipeline. Do not serialize exception text,
  row-version internals, protected mobile data, or audit metadata that is not
  explicitly safe.

- [ ] **Step 4: Run API tests and the full Release backend suite once.**

  Run: `dotnet test backend/EosDashboards.sln -c Release`

  Expected: PASS with existing authentication, refresh, logout, and Google
  paths retained.

- [ ] **Step 5: Commit the API surface and tests.**

  ```text
  git add backend/src/EosDashboards.Api backend/tests/EosDashboards.IntegrationTests
  git commit -m "feat: expose system administration API"
  ```

### Task 6: Add typed frontend administration access and authorized navigation

**Files:**
- Add: `frontend/src/features/administration/administrationTypes.ts`
- Add: `frontend/src/features/administration/administrationApi.ts`
- Modify: `frontend/src/features/auth/authTypes.ts`
- Modify: `frontend/src/app/providers/AuthProvider.tsx`
- Modify: `frontend/src/navigation/workspaceTargets.tsx`
- Modify: `frontend/src/navigation/routeRegistry.tsx`
- Modify: `frontend/src/navigation/TabWorkspaceProvider.tsx`
- Modify: `frontend/src/layout/AppShell.tsx`
- Test: `frontend/src/features/administration/administrationApi.test.ts`
- Test: `frontend/src/navigation/workspaceTargets.test.tsx`
- Test: `frontend/src/App.test.tsx`

**Interfaces:**
- Consumes the Task 5 REST projections through `apiFetch`.
- Adds System Administrator targets `system-administration-dashboard`,
  `administration-users`, `administration-departments`, and `administration-audit`.
- Adds route-aware closable form tabs `administration-user-create` and
  `administration-user-edit`; only their list/dashboard/audit parents appear
  in the sidebar and command search.

- [ ] **Step 1: Write failing type/component tests for a System Administrator
  discovering exactly four new targets and a forced temporary-password user.**

  ```tsx
  expect(authorizedWorkspaceTargets(["SystemAdministrator"]).map(x => x.routeId)).toEqual([
    "system-administration-dashboard", "administration-users",
    "administration-departments", "administration-audit",
  ]);
  expect(screen.getByRole("dialog", { name: "تغییر رمز عبور" })).toBeVisible();
  ```

- [ ] **Step 2: Run the focused frontend tests and confirm target/routes are absent.**

  Run: `npm --prefix frontend run test -- --run src/navigation/workspaceTargets.test.tsx src/App.test.tsx src/features/administration/administrationApi.test.ts`

- [ ] **Step 3: Implement typed API calls, forced-password gating, and route support.**

  Add `mustChangePassword` to `AuthenticatedUser`; keep the existing dialog
  non-dismissible when the flag is true, submit the current temporary password,
  then use the existing logout-on-password-change behavior. Extend route guards
  so authorized dynamic user-form paths render their tab and unauthorized typed
  paths safely return to home. Do not cache list/detail data across logout.

- [ ] **Step 4: Run the focused tests and TypeScript check.**

  Run: `npm --prefix frontend run typecheck`

- [ ] **Step 5: Commit typed client and navigation.**

  ```text
  git add frontend/src
  git commit -m "feat: add administration workspace navigation"
  ```

### Task 7: Build the users and departments workspaces

**Files:**
- Add: `frontend/src/features/administration/UserDirectoryPage.tsx`
- Add: `frontend/src/features/administration/UserFormPage.tsx`
- Add: `frontend/src/features/administration/DepartmentManagementPage.tsx`
- Add: `frontend/src/features/administration/DepartmentForm.tsx`
- Add: `frontend/src/features/administration/administrationCopy.ts`
- Modify: `frontend/src/index.css`
- Test: `frontend/src/features/administration/UserDirectoryPage.test.tsx`
- Test: `frontend/src/features/administration/UserFormPage.test.tsx`
- Test: `frontend/src/features/administration/DepartmentManagementPage.test.tsx`

**Interfaces:**
- Uses Task 6 safe list/detail records and mutation functions.
- Opens a user create/edit `TabDescriptor` with the user ID in its pathname and
  a distinct key from `createTabKey`; closing returns focus to the directory.

- [ ] **Step 1: Write component red tests for actual form behavior.**

  ```tsx
  await user.click(screen.getByRole("button", { name: "ایجاد کاربر" }));
  await user.type(screen.getByLabelText("کد پرسنلی"), "124");
  expect(screen.getByLabelText("نام کاربری")).toHaveValue("");
  await user.click(screen.getByRole("button", { name: "ثبت کاربر" }));
  expect(createUser).toHaveBeenCalledWith(expect.objectContaining({ username: undefined }));

  expect(screen.queryByRole("option", { name: "زیرواحد نمونه" })).not.toBeInTheDocument();
  ```

- [ ] **Step 2: Run the three focused component tests and confirm they fail.**

  Run: `npm --prefix frontend run test -- --run src/features/administration/UserDirectoryPage.test.tsx src/features/administration/UserFormPage.test.tsx src/features/administration/DepartmentManagementPage.test.tsx`

- [ ] **Step 3: Implement the management pages with deliberate states.**

  Use flat accent-line list panels, compact server-search controls, pagination,
  status text/icons, role checkboxes, department selector, confirmation dialogs
  for deactivate/reset/delete, and row-version conflict recovery that reloads
  rather than overwriting. The mobile field displays its server mask and a blank
  replacement input. Make the department form's `زیرمجموعهٔ واحد دیگر است`
  switch reveal only root parents; disable/delete with a specific explanation
  when the server marks a unit non-empty.

- [ ] **Step 4: Run the focused tests, lint, and format check.**

  Run: `npm --prefix frontend run lint`

  Run: `npm --prefix frontend run format:check`

- [ ] **Step 5: Commit user and department UI.**

  ```text
  git add frontend/src
  git commit -m "feat: build user and department management pages"
  ```

### Task 8: Build the System Administrator dashboard and audit workspace

**Files:**
- Add: `frontend/src/features/administration/SystemAdministrationDashboardPage.tsx`
- Add: `frontend/src/features/administration/AuditHistoryPage.tsx`
- Add: `frontend/src/features/administration/auditEventPresentation.ts`
- Test: `frontend/src/features/administration/SystemAdministrationDashboardPage.test.tsx`
- Test: `frontend/src/features/administration/AuditHistoryPage.test.tsx`
- Modify: `frontend/tests/e2e/auth-shell.spec.ts`

**Interfaces:**
- Renders `SystemAdministrationDashboard` and `AuditHistoryPage` projections
  from Task 6. `auditEventPresentation.ts` is the single finite mapping of safe
  API event codes to Persian title/description/status presentation.

- [ ] **Step 1: Write failing dashboard/audit component and browser-flow tests.**

  ```tsx
  expect(screen.getByText("کاربران دارای نشست فعال")).toBeVisible();
  expect(screen.getByText("۲۴ ساعت گذشته")).toBeVisible();
  await user.click(screen.getByRole("link", { name: "مشاهدهٔ همهٔ ممیزی‌ها" }));
  expect(openTab).toHaveBeenCalledWith(expect.objectContaining({ routeId: "administration-audit" }));
  ```

  Add a mocked authenticated Playwright state that verifies the dashboard,
  latest audit description, audit filters, and a phone-width layout; verify a
  non-administrator does not discover the four administration targets.

- [ ] **Step 2: Run the focused dashboard/audit tests and confirm they fail.**

  Run: `npm --prefix frontend run test -- --run src/features/administration/SystemAdministrationDashboardPage.test.tsx src/features/administration/AuditHistoryPage.test.tsx`

- [ ] **Step 3: Implement truthful summary and audit interaction.**

  Render the five approved 24-hour metrics, latest event stream with actor and
  subject labels, explicit empty/error/loading states, and one link that opens
  audit history prefiltered by the dashboard time window. Audit history uses
  preset 7/30-day ranges plus validated local custom dates, event/result/actor/
  subject filters, and server paging. Never infer counts, show a live-presence
  label, or use a charting dependency.

- [ ] **Step 4: Run focused component tests and browser flow on the dedicated port.**

  Run: `npm --prefix frontend run test -- --run src/features/administration/SystemAdministrationDashboardPage.test.tsx src/features/administration/AuditHistoryPage.test.tsx`

  Run: `$env:EOS_PLAYWRIGHT_PORT=4174; npm --prefix frontend run e2e`

- [ ] **Step 5: Commit dashboard/audit UI and browser coverage.**

  ```text
  git add frontend/src frontend/tests/e2e
  git commit -m "feat: add system administration audit dashboard"
  ```

### Task 9: Integrate, verify, document, and publish the complete slice

**Files:**
- Modify: `docs/project/current-state.md`
- Modify: `docs/project/architecture.md`
- Modify: `docs/project/requirements.md`
- Modify: `docs/project/roadmap.md`
- Modify: `docs/operations/iis-deployment.md`
- Modify: `AGENTS.md`

**Interfaces:**
- Consumes all completed backend/frontend contracts and the migration from Task 2.
- Produces one committed, tested `main` source, reviewed local IIS artifact, and
  an updated canonical implementation state.

- [ ] **Step 1: Update canonical documentation before integration.**

  Replace the planned-state wording with implementation facts: migration name,
  API authorization boundary, forced temporary-password behavior, session
  invalidation, department restrictions, active-session definition, audit
  privacy, focused verification, release identifier, and safe smoke outcome.
  Extend the IIS runbook's migration check for the new migration; never record
  user data or server configuration values.

- [ ] **Step 2: Run the full integration verification once.**

  ```text
  dotnet test backend/EosDashboards.sln -c Release
  npm --prefix frontend run lint
  npm --prefix frontend run typecheck
  npm --prefix frontend run format:check
  npm --prefix frontend run build:iis
  npm --prefix frontend run test -- --run
  $env:EOS_PLAYWRIGHT_PORT=4174; npm --prefix frontend run e2e
  ```

- [ ] **Step 3: Inspect the built artifact and rendered UI.**

  Confirm no source maps or server secrets are in the UI artifact. Inspect the
  System Administrator dashboard, user and department pages, forced-password
  state, audit filtering, conflict/empty/error states, and no relevant console
  errors at desktop and phone widths.

- [ ] **Step 4: Commit, push, migrate, and publish only after verification.**

  ```text
  git add AGENTS.md docs backend frontend
  git commit -m "feat: complete system administration slice"
  git push origin codex/system-administration-and-audit-dashboard
  git switch main
  git merge --no-ff codex/system-administration-and-audit-dashboard -m "merge: system administration slice"
  git push origin main
  dotnet publish backend/src/EosDashboards.Api/EosDashboards.Api.csproj -c Release -o backend/artifacts/api-system-administration
  powershell -ExecutionPolicy Bypass -File scripts/Publish-LocalIisRelease.ps1
  ```

  Before the IIS switch, verify a local database backup and apply the reviewed
  Release migration. Then verify HTTPS liveness/readiness, UI entry, an SPA
  refresh route, and a non-sensitive authenticated System Administrator smoke
  flow if access is available.

- [ ] **Step 5: Record publication evidence and complete the integration.**

  Commit any resulting non-sensitive documentation update, push the destination
  branch, and report the release identifier plus the verification outcome. If a
  push or deployment step fails, preserve the work and record the exact
  recoverable blocker rather than claiming completion.

## Coverage review

- [ ] Every approved rule in the spec maps to Tasks 1–8; Task 9 documents and
  verifies the integrated result.
- [ ] Dynamic form tabs are route-guarded and not unnecessarily searchable;
  System Administrator data remains server-authorized.
- [ ] Audit events disclose only safe actor/subject projections and event
  categories; all secret and full-mobile paths are absent from tests and UI.
- [ ] The dashboard uses audit/session data only, has the agreed 24-hour window,
  and labels active sessions without implying real-time browser presence.
