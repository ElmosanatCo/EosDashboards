# Administration Usability and Audit Attribution Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make System Administrator user/department operations usable in modal dialogs and add safe IP/device attribution plus Persian-calendar audit filtering.

**Architecture:** A request-scoped audit context extends the existing correlation abstraction so every request-originated audit record receives a direct remote IP and a coarse device kind. The audit aggregate, EF model, projection, API contract, and React view evolve together through an additive migration. Management forms become reusable modal content controlled by their list pages, while a shared Persian timestamp picker replaces native Gregorian controls.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, EF Core 10 / SQL Server, React 19, TypeScript, Material UI 9, TanStack Query, Vitest, Playwright.

**Spec:** `docs/superpowers/specs/2026-09-03-administration-usability-and-audit-attribution-design.md`

## Global Constraints

- System Administrator authorization remains server-enforced; UI visibility is discoverability only.
- Store only a direct remote IP and `Desktop`, `Mobile`, `Tablet`, or `Unknown`; never store raw User-Agent or trust forwarded headers.
- Existing audit data remains immutable and displays missing new attribution as `ثبت نشده`.
- Retain local-server `datetime2(3)` persistence without `Utc` names or Tehran conversion.
- All date-selection UI uses Persian calendar/digits; do not add MUI X or a new dependency.
- Preserve the fixed roles, two-level department model, temporary-password, session-revocation, and secret/mobile protections.
- Run focused red/green checks per task; run broad verification once before integration and publication.

---

### Task 1: Establish request attribution and persist it in immutable audit records

**Files:**
- Modify: `backend/src/EosDashboards.Application/Abstractions/ICorrelationContext.cs`
- Modify: `backend/src/EosDashboards.Api/Security/HttpCorrelationContext.cs`
- Modify: `backend/src/EosDashboards.Application/Abstractions/IAuditWriter.cs`
- Modify: `backend/src/EosDashboards.Domain/Entities/AuditLog.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/AuditWriter.cs`
- Modify: `backend/tests/EosDashboards.Domain.Tests/UserTests.cs`
- Modify: `backend/tests/EosDashboards.IntegrationTests/Database/RepositoryTests.cs`

**Interfaces:**
- `ICorrelationContext` exposes `TraceId`, `ClientIpAddress`, and `ClientDeviceKind`.
- `AuditRecord` and `AuditLog.Create` gain nullable `string? ClientIpAddress` and `string? ClientDeviceKind` values.
- `HttpCorrelationContext` normalizes `HttpContext.Connection.RemoteIpAddress` and classifies only the current request's User-Agent.

- [ ] **Step 1: Write failing Domain and SQL repository tests.** Assert that an audit record round-trips a direct IP and `Desktop`, and that absent attribution is permitted. Name the production change under test in the test name.

- [ ] **Step 2: Run the focused tests and verify they fail because the new audit properties do not exist.**

  Run: `dotnet test backend/tests/EosDashboards.Domain.Tests/EosDashboards.Domain.Tests.csproj -c Release --filter "FullyQualifiedName~UserTests"`

  Run: `dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~RepositoryTests"`

- [ ] **Step 3: Add the smallest request-attribution model.** Extend the correlation context, add nullable fields limited to 45 and 16 characters respectively, feed them through `AuditWriter`, and classify strings into the four specified values without retaining the source header.

- [ ] **Step 4: Run the same focused tests and verify they pass.**

- [ ] **Step 5: Generate and inspect an additive EF migration.** Name it `AddAuditRequestAttribution`; check that it only adds nullable audit columns and updates the model snapshot.

- [ ] **Step 6: Commit the tested audit persistence change.**

  ```text
  git add backend/src/EosDashboards.Application backend/src/EosDashboards.Api backend/src/EosDashboards.Domain backend/src/EosDashboards.Infrastructure backend/tests
  git commit -m "feat: attribute audit records to request origin"
  ```

### Task 2: Expose safe audit attribution and prove management mutations reach the API

**Files:**
- Modify: `backend/src/EosDashboards.Application/Administration/AdministrationContracts.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/AuditLogReader.cs`
- Modify: `backend/src/EosDashboards.Api/Administration/AdministrationContracts.cs`
- Modify: `backend/src/EosDashboards.Api/Administration/AdministrationEndpoints.cs`
- Modify: `backend/tests/EosDashboards.Application.Tests/Administration/AuditDashboardTests.cs`
- Modify: `backend/tests/EosDashboards.IntegrationTests/Api/AdministrationEndpointTests.cs`

**Interfaces:**
- `AuditLogListItem` and the API audit response add nullable `clientIpAddress` and `clientDeviceKind`.
- Authenticated System Administrator POSTs to `/api/v1/administration/departments` and `/users` return 201, safe projections, and immutable audit events.

- [ ] **Step 1: Write failing reader and API tests.** The reader projection must include stored attribution. The authenticated endpoint test must submit `{ name, parentDepartmentId: null }`, then a valid user command, assert both 201 responses, and assert the created audit records contain the request attribution.

- [ ] **Step 2: Run the focused tests and verify they fail at the missing response fields or unimplemented authenticated mutation fixture.**

  Run: `dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj -c Release --filter "FullyQualifiedName~AuditDashboardTests"`

  Run: `dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AdministrationEndpointTests"`

- [ ] **Step 3: Implement projections and a minimal authenticated API test fixture.** Reuse the real EF repositories against the `_IntegrationTests` catalog, seed fixed role/department/system-administrator data only inside the fixture, issue a test JWT with that role, and clean through the existing fixture lifecycle. Do not test with the development database.

- [ ] **Step 4: Run the same focused tests and verify they pass.**

- [ ] **Step 5: Commit the endpoint/projection and regression coverage.**

  ```text
  git add backend/src/EosDashboards.Application backend/src/EosDashboards.Api backend/src/EosDashboards.Infrastructure backend/tests
  git commit -m "test: cover administration creation requests"
  ```

### Task 3: Replace administration form tabs with reusable modal workflows

**Files:**
- Create: `frontend/src/features/administration/AdministrationDialog.tsx`
- Create: `frontend/src/pages/UserFormDialog.tsx`
- Create: `frontend/src/pages/DepartmentFormDialog.tsx`
- Modify: `frontend/src/pages/UserManagementPage.tsx`
- Modify: `frontend/src/pages/DepartmentManagementPage.tsx`
- Modify: `frontend/src/navigation/routeRegistry.tsx`
- Modify: `frontend/src/layout/AppShell.tsx`
- Modify: `frontend/src/pages/UserFormPage.tsx` (remove after dialog extraction)
- Modify: `frontend/src/pages/DepartmentFormPage.tsx` (remove after dialog extraction)
- Create: `frontend/src/pages/UserManagementPage.test.tsx`
- Create: `frontend/src/pages/DepartmentManagementPage.test.tsx`

**Interfaces:**
- `AdministrationDialog` provides a labelled responsive `Dialog`, a cancel action, a pending-safe close guard, and focus return to the invoker.
- Each management page owns `selectedId: number | null | undefined`; `undefined` means closed, `null` means create, and a number means edit.
- On success, modal mutations invalidate their list/dashboard query, close, and announce a Persian success message. On failure, entered values remain and an API problem is shown.

- [ ] **Step 1: Write failing component tests.** Verify that `تعریف کاربر` and `تعریف واحد` open dialogs rather than create workspace tabs; fill valid data, mock a 201 response, assert the request body, closed dialog, and refreshed list. Add a 400 response case that leaves form values visible and displays a safe error.

- [ ] **Step 2: Run the two component tests and verify they fail because the buttons currently open tabs.**

  Run: `npm --prefix frontend run test -- --run src/pages/UserManagementPage.test.tsx src/pages/DepartmentManagementPage.test.tsx`

- [ ] **Step 3: Extract focused dialog content and wire it into list pages.** Preserve labels, personnel-code username fallback, masked-mobile handling, fixed roles, root-parent selector, loading/error states, and existing server mutation contracts. Remove only the dynamic form-tab route support; management list pages remain normal workspace tabs.

- [ ] **Step 4: Run the component tests, typecheck, and format check.**

  Run: `npm --prefix frontend run typecheck`

  Run: `npm --prefix frontend run format:check`

- [ ] **Step 5: Commit the modal workflow.**

  ```text
  git add frontend/src
  git commit -m "fix: use dialogs for administration forms"
  ```

### Task 4: Centralize Persian date/time selection and restore a compact status bar

**Files:**
- Create: `frontend/src/lib/date/persianCalendar.ts`
- Create: `frontend/src/components/PersianDateTimePicker.tsx`
- Modify: `frontend/src/pages/SystemAuditPage.tsx`
- Modify: `frontend/src/features/administration/administrationApi.ts`
- Modify: `frontend/src/features/administration/administrationUi.ts`
- Modify: `frontend/src/layout/StatusBar.tsx`
- Modify: `frontend/src/layout/StatusBar.test.tsx`
- Create: `frontend/src/components/PersianDateTimePicker.test.tsx`

**Interfaces:**
- `PersianDateTimePicker` takes `label`, `value: Date | null`, and `onChange(Date | null)`, renders Persian-calendar day/month/year selections and a separate time input, and returns server-local `Date` values.
- Audit request construction serializes picker values using the existing ISO-like local timestamp format required by the API.
- `AuditLog` includes `clientIpAddress` and `clientDeviceKind`; `eventLabel` maps all current authentication event codes.

- [ ] **Step 1: Write failing picker tests.** Choose a Persian date using Persian digits, choose its time separately, and assert the emitted local timestamp. Assert that the rendered controls contain no native `date` or `datetime-local` type. Extend the status-bar test to assert its clock container has a one-line layout.

- [ ] **Step 2: Run focused tests and verify the picker test fails because the shared picker is absent.**

  Run: `npm --prefix frontend run test -- --run src/components/PersianDateTimePicker.test.tsx src/layout/StatusBar.test.tsx`

- [ ] **Step 3: Implement the minimal Gregorian/Persian conversion utility and accessible shared picker.** Use no browser locale parsing or third-party dependency. Make the audit range use two picker instances, display IP/device columns with `ثبت نشده` fallback, and lay the status date/time values in one `nowrap` horizontal row.

- [ ] **Step 4: Run the same tests, typecheck, and format check.**

- [ ] **Step 5: Commit the Persian date and audit view correction.**

  ```text
  git add frontend/src
  git commit -m "feat: add Persian audit date selection"
  ```

### Task 5: Validate rendered interaction, document, integrate, and publish

**Files:**
- Modify: `frontend/tests/e2e/auth-shell.spec.ts`
- Modify: `docs/project/requirements.md`
- Modify: `docs/project/architecture.md`
- Modify: `docs/project/standards.md`
- Modify: `docs/project/decisions/0012-system-administration-and-audit-visibility.md`
- Modify: `docs/project/current-state.md`
- Modify: `AGENTS.md`

**Interfaces:**
- Browser test flow: authenticated System Administrator -> management page -> create modal -> successful response -> refreshed list/closed modal; audit page -> Persian range input -> IP/device columns; footer -> single line at desktop and phone widths.

- [ ] **Step 1: Write a failing mocked browser flow for each management dialog and the audit/status-bar behavior.** Ensure the network route records actual POST request body and returns an updated GET list.

- [ ] **Step 2: Run the focused browser test and verify it fails before the modal/picker behavior is complete.**

  Run: `$env:EOS_PLAYWRIGHT_PORT=4175; npm --prefix frontend run e2e -- --grep "System Administrator"`

- [ ] **Step 3: Update durable canonical documents.** Record the direct-IP/no-forwarded-header rule, coarse device-only classification, Persian picker requirement, confirmed user-facing modal behavior, observed missing mutation coverage, root cause established by the added request-path test, and release state. Keep `current-state.md` concise and remove obsolete “implementation underway” wording.

- [ ] **Step 4: Run broad verification once.**

  Run: `dotnet test backend/EosDashboards.sln -c Release`

  Run: `npm --prefix frontend run lint; npm --prefix frontend run typecheck; npm --prefix frontend run format:check; npm --prefix frontend run build:iis; npm --prefix frontend run test -- --run`

  Run: `$env:EOS_PLAYWRIGHT_PORT=4175; npm --prefix frontend run e2e`

- [ ] **Step 5: Perform rendered QA using Playwright because the Browser plugin is unavailable.** Verify app identity, meaningful content, no error overlay, console health, interaction proof, and screenshots for desktop/mobile. Keep screenshots outside tracked source.

- [ ] **Step 6: Commit, push, migrate, and publish.** Update the IIS publisher expected migration, create a verified development database backup, apply the migration with `ASPNETCORE_ENVIRONMENT=Development`, build committed `main` artifacts, use the publisher preflight, then verify HTTPS liveness/readiness, UI entry, internal refresh, and a non-sensitive administrator mutation/check if available.

  ```text
  git add AGENTS.md docs backend frontend
  git commit -m "fix: complete administration usability and audit attribution"
  git push origin main
  ```

## Plan self-review

- [x] Modal usability, audit IP/device attribution, Persian date selection, one-line status layout, request-path regression coverage, documentation, migration, and publication each have a task.
- [x] New public contracts use the same camel-case serialization path as current administration responses.
- [x] The plan explicitly avoids user-agent persistence, forwarded-header trust, a production-like development mutation, MUI X, and a third-party date-picker dependency.
