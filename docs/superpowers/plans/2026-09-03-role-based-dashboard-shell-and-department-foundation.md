# Role-based dashboard shell and department foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish the real department and fixed-role foundation, expose an authenticated user's roles and department safely, and provide three authorized empty dashboard targets through the existing RTL workspace and global command search.

**Architecture:** The Domain owns department-depth and user-department invariants. EF Core persists the organizational model and a migration safely aligns the established administrator through its existing role assignment. Application authentication projects stable role codes and a department summary. The React shell reads a single role-filtered target catalogue for sidebar entries, internal tabs, route recovery, and command search; it never treats client filtering as API authorization.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, Entity Framework Core with SQL Server, React 19, TypeScript, Material UI, Vitest, Playwright, PowerShell IIS publisher.

**Spec:** `docs/superpowers/specs/2026-09-03-role-based-dashboard-shell-and-department-foundation-design.md`

## Global Constraints

- Keep local username/password plus SMS OTP and pre-linked Google OpenID Connect behavior unchanged.
- Create no user, role, department, or identity administration form in this slice.
- Keep the existing persistent collapsible navigation behavior, fixed home tab, theme, Persian RTL layout, and no-data honesty.
- The only initial role-specific targets are `داشبورد بخش`, `داشبورد منابع انسانی`, and `داشبورد مدیرعامل`; System Administrator alone grants none of them.
- Never seed, assert, log, or display personal data. Resolve the existing initial user only via its existing System Administrator role.
- A user has one required department; department depth is at most two; any number of users may be department managers of the same department.
- Add focused failing tests before behavior changes and use the established isolated SQL integration database only.

---

### Task 1: Model fixed roles, departments, and the persistence boundary

**Files:**
- Add: `backend/src/EosDashboards.Domain/Entities/Department.cs`
- Add: `backend/src/EosDashboards.Domain/Authorization/SystemRoleCodes.cs`
- Modify: `backend/src/EosDashboards.Domain/Entities/User.cs`
- Add: `backend/src/EosDashboards.Application/Abstractions/IDepartmentRepository.cs`
- Modify: `backend/src/EosDashboards.Application/Abstractions/IRoleRepository.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/EosDashboardDbContext.cs`
- Add: `backend/src/EosDashboards.Infrastructure/Persistence/Configurations/DepartmentConfiguration.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/RoleRepository.cs`
- Add: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/DepartmentRepository.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/DependencyInjection.cs`
- Test: `backend/tests/EosDashboards.Domain.Tests/DepartmentTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Database/ModelMappingTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Database/RepositoryTests.cs`

- [ ] Add failing domain tests for an independent department, a direct child, rejection of a child as a parent, and an explicit user department assignment.
- [ ] Add the `Department` aggregate and fixed code constants; add the required department relation to `User` without changing role multiplicity.
- [ ] Configure bigint keys, required Unicode department name, self-reference and user foreign key with restrictive delete behavior; expose repositories that retrieve roles by identifiers and departments by identifier/name.
- [ ] Register the repositories, then run the Domain and focused SQL mapping/repository tests until the new invariants and mappings pass.

### Task 2: Safely migrate and provision the organizational baseline

**Files:**
- Add: EF Core migration file generated in `backend/src/EosDashboards.Infrastructure/Persistence/Migrations/` with the name `AddDepartmentsAndRoleDashboardFoundation`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Migrations/EosDashboardDbContextModelSnapshot.cs`
- Modify: `backend/src/EosDashboards.Application/Provisioning/ProvisionSystemAdministrator.cs`
- Modify: `backend/tests/EosDashboards.Application.Tests/Provisioning/ProvisionSystemAdministratorTests.cs`
- Modify: `backend/tests/EosDashboards.IntegrationTests/Database/DatabaseConstraintTests.cs`
- Modify: `backend/tests/EosDashboards.IntegrationTests/Provisioning/ProvisionerTests.cs`

- [ ] Add failing provisioning and integration tests for all four fixed roles, `نرم افزار`, direct-child `فناوری اطلاعات`, the System Administrator's Department Manager assignment, and rejection of users without a department.
- [ ] Generate the EF migration using `dotnet ef migrations add AddDepartmentsAndRoleDashboardFoundation --project backend/src/EosDashboards.Infrastructure --startup-project backend/src/EosDashboards.Api --output-dir Persistence/Migrations`.
- [ ] Review the generated migration and make its data upgrade deterministic: idempotently ensure all fixed roles and both departments, assign only the user already holding `SystemAdministrator` to `نرم افزار`, add that user's `DepartmentManager` role, then fail before the new required constraint if any user remains without a department.
- [ ] Update the deployment-only provisioner so a new or repaired bootstrap administrator finds `نرم افزار`, holds both required roles, and has that department; do not add UI or accept department input.
- [ ] Run `dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj -c Release` and `dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj -c Release` using the established isolated test database configuration.

### Task 3: Return server-authoritative role codes and department summary in every session response

**Files:**
- Modify: `backend/src/EosDashboards.Application/Auth/AuthContracts.cs`
- Modify: `backend/src/EosDashboards.Application/Auth/VerifyOtp.cs`
- Modify: `backend/src/EosDashboards.Application/Auth/RefreshSession.cs`
- Modify: `backend/src/EosDashboards.Application/Auth/GoogleSignIn.cs`
- Modify: `backend/src/EosDashboards.Api/Auth/AuthContracts.cs`
- Modify: `backend/tests/EosDashboards.Application.Tests/Auth/AuthenticationFakes.cs`
- Modify: `backend/tests/EosDashboards.Application.Tests/Auth/VerifyOtpTests.cs`
- Modify: `backend/tests/EosDashboards.Application.Tests/Auth/SessionLifecycleTests.cs`
- Modify: `backend/tests/EosDashboards.Application.Tests/Auth/GoogleSignInTests.cs`
- Modify: `backend/tests/EosDashboards.IntegrationTests/Api/AuthEndpointTests.cs`

- [ ] Add failing tests showing local OTP verification, refresh, current-user lookup, and approved linked Google sign-in all return the same role-code and department summary without revealing hidden profile data.
- [ ] Add authenticated-user role-code and department-summary contract types; resolve them through Application abstractions when constructing the session projection while retaining numeric role identifiers only where existing compatibility requires them.
- [ ] Make every API authentication response serialize the enriched contract identically, including `/me`; retain existing OTP, refresh rotation, cookie, token, and Google-link security behavior.
- [ ] Run the focused Application authentication and API authentication endpoint tests; then run `dotnet test backend/EosDashboards.sln -c Release`.

### Task 4: Define one authorized workspace catalogue and the three honest dashboard pages

**Files:**
- Add: `frontend/src/navigation/workspaceTargets.tsx`
- Modify: `frontend/src/features/auth/authTypes.ts`
- Modify: `frontend/src/layout/AppShell.tsx`
- Modify: `frontend/src/layout/AppSidebar.tsx`
- Modify: `frontend/src/navigation/TabWorkspaceProvider.tsx`
- Add: `frontend/src/pages/DepartmentDashboardPage.tsx`
- Add: `frontend/src/pages/HumanResourcesDashboardPage.tsx`
- Add: `frontend/src/pages/ChiefExecutiveDashboardPage.tsx`
- Test: `frontend/src/navigation/workspaceTargets.test.tsx`
- Test: `frontend/src/App.test.tsx`

- [ ] Add failing component/unit tests for exact role filtering, a user with several roles seeing all matching targets, System Administrator alone seeing none of the three targets, and direct unauthorized routes recovering to home.
- [ ] Extend the frontend authenticated-user type with `roleCodes` and a department summary; keep bootstrap behavior safe while the session has not restored.
- [ ] Build the catalogue once with Persian title, path, icon, required role codes, and searchable labels; make sidebar, tab content, and route resolution consume its filtered targets.
- [ ] Create each dashboard as a title plus an explicit no-data state only; do not add counts, cards, people, workflows, charts, or guessed operations.
- [ ] Run `npm --prefix frontend run test -- --run src/navigation/workspaceTargets.test.tsx src/App.test.tsx` after the focused tests are green.

### Task 5: Add the compact role-filtered global command search

**Files:**
- Modify: `frontend/src/layout/AppHeader.tsx`
- Modify: `frontend/src/layout/AppShell.tsx`
- Add: `frontend/src/layout/CommandSearch.tsx`
- Modify: `frontend/src/index.css`
- Test: `frontend/src/layout/CommandSearch.test.tsx`
- Modify: `frontend/tests/e2e/auth-shell.spec.ts`

- [ ] Add failing component tests for `Ctrl+K` focus, Persian title/keyword filtering, exclusion of unavailable targets, and opening or activating an internal workspace tab from a selected result.
- [ ] Place the compact central command search in the fixed header using the already approved dark RTL visual language; keep it keyboard-accessible, labelled, and responsive without shifting established navigation behavior.
- [ ] Query only the current user's filtered catalogue, focus on `Ctrl+K`, and route a chosen page through the existing tab workspace; prepare the catalogue type for later approved operations and dashboard elements without inventing any now.
- [ ] Update the mocked authenticated Playwright state and verify visible search, role-specific navigation, tabs, the three empty pages, and unauthorized-route recovery at desktop and phone widths.

### Task 6: Integrate, inspect, document, and publish one committed release source

**Files:**
- Modify: `docs/project/current-state.md`
- Modify: `docs/project/requirements.md`
- Modify: `docs/project/architecture.md`
- Modify: `docs/project/roadmap.md`
- Modify: `docs/project/standards.md`
- Modify: `AGENTS.md`
- Modify: `docs/superpowers/specs/2026-09-03-role-based-dashboard-shell-and-department-foundation-design.md`

- [ ] Reconcile canonical documentation with the implemented fixed roles, required user department, seed baseline, session contract, role-filtered navigation/search, exclusions, release outcome, and next steps; replace stale statements rather than appending chat history.
- [ ] Run `npm --prefix frontend run lint`, `npm --prefix frontend run typecheck`, `npm --prefix frontend run format:check`, `npm --prefix frontend run build:iis`, `npm --prefix frontend run test -- --run`, and `set EOS_PLAYWRIGHT_PORT=4174&& npm --prefix frontend run test:e2e`.
- [ ] Inspect the built UI artifact and a local browser at desktop and phone widths for RTL search, clipping, focus, role filtering, tab behavior, no-data pages, and relevant console errors.
- [ ] Commit all documentation and implementation from one integrated `main` source, push the configured remote, build the Release API artifact with `dotnet publish backend/src/EosDashboards.Api/EosDashboards.Api.csproj -c Release -o backend/artifacts/api-local-credential`, then run `powershell -ExecutionPolicy Bypass -File scripts/Publish-LocalIisRelease.ps1`.
- [ ] After the IIS switch, verify HTTPS API liveness/readiness, UI entry, an internal SPA refresh route, and an authenticated role-filtered dashboard/search smoke flow if non-sensitive access is available; record the release identifier and non-sensitive result in `current-state.md`.

## Coverage review

- [ ] Confirm every approved item in the specification has an implementing task and every excluded item remains absent.
- [ ] Confirm test factories, fixtures, and test-only persisted users assign a department so the new database constraint exercises intended production behavior rather than failing incidentally.
- [ ] Confirm migration, provisioning, and documentation use Unicode-safe text handling and no test output contains user or configuration secrets.
- [ ] Confirm the command search derives results only from the same current-user filtered catalogue as the sidebar and workspace route guard.
