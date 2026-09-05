# Job-description skill review warning Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow unrelated selected skills, represent missing required task skills as a non-blocking persisted review warning, and expose the warning to department managers and the Chief Executive Officer.

**Architecture:** Keep blocking quality and management review as separate domain facts. The analyzer will still return detailed missing-required-skill findings, while the application derives `HasBlockingIssues` and `NeedsReview`; the version persists the latter and the API projects it into existing worklists plus a scoped read-only warning feed. A data-only EF migration will add the flag and re-evaluate active versions without rewriting approved or archived history.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, EF Core SQL Server, xUnit, React, TypeScript, Material UI, Vitest.

**Spec:** `docs/superpowers/specs/2026-09-05-job-description-review-warning-design.md`

## Global Constraints

- Unrelated selected skills are valid and must not produce a finding.
- Missing required task skills are warnings only; they must not block department or Human Resources approval.
- Structural, unresolved, uncatalogued, and missing task data remain blocking quality issues.
- Approved and archived history is not rewritten by the migration.
- Persian RTL UI, Persian digits, existing accent-card styling, help icon, and current IIS deployment conventions remain unchanged.
- Do not stage or modify unrelated `node_modules/`, `output/`, or `tmp/` content.

---

### Task 1: Define the quality assessment contract

**Files:**
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/JobDescriptionQualityAnalyzer.cs`
- Create: `backend/src/EosDashboards.Application/JobDescriptions/JobDescriptionQualityAssessment.cs`
- Test: `backend/tests/EosDashboards.Application.Tests/JobDescriptions/JobDescriptionQualityAnalyzerTests.cs`

**Interfaces:**
- Consumes: `JobDescriptionQualityAnalyzer.Analyze(...)` findings.
- Produces: `JobDescriptionQualityAssessment.From(IReadOnlyCollection<JobDescriptionQualityFinding>)`, with `HasBlockingIssues` and `NeedsReview`.

- [ ] **Step 1: Write the failing tests**

  Replace the unsupported-selected-skill expectation with a test that a selected skill unrelated to the task produces no finding. Add an assessment test showing `missing-required-skill` produces `NeedsReview == true` and `HasBlockingIssues == false`, while `missing-task-start-date` produces `HasBlockingIssues == true`.

- [ ] **Step 2: Run the focused tests to verify they fail**

  Run:

  ```powershell
  dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter "FullyQualifiedName~JobDescriptionQualityAnalyzerTests" --no-restore
  ```

  Expected: the old analyzer still returns `unsupported-selected-skill`, and no assessment type exists.

- [ ] **Step 3: Implement the minimal assessment behavior**

  Remove the loop that emits `unsupported-selected-skill`. Add `JobDescriptionQualityAssessment` with the exact classification:

  ```csharp
  public sealed record JobDescriptionQualityAssessment(
      IReadOnlyList<JobDescriptionQualityFinding> Findings)
  {
      public bool NeedsReview => Findings.Any(item => item.Code == "missing-required-skill");
      public bool HasBlockingIssues => Findings.Any(item => item.Code != "missing-required-skill");

      public static JobDescriptionQualityAssessment From(
          IReadOnlyList<JobDescriptionQualityFinding> findings) => new(findings);
  }
  ```

- [ ] **Step 4: Run the focused tests to verify they pass**

  Re-run the command from Step 2. Expected: all analyzer and assessment tests pass.

- [ ] **Step 5: Commit the isolated assessment change**

  ```powershell
  git add backend/src/EosDashboards.Application/JobDescriptions/JobDescriptionQualityAnalyzer.cs backend/src/EosDashboards.Application/JobDescriptions/JobDescriptionQualityAssessment.cs backend/tests/EosDashboards.Application.Tests/JobDescriptions/JobDescriptionQualityAnalyzerTests.cs
  git commit -m "fix: classify missing task skills as review warnings"
  ```

### Task 2: Persist and recalculate the independent review flag

**Files:**
- Modify: `backend/src/EosDashboards.Domain/Entities/JobDescriptionVersion.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Configurations/JobDescriptionVersionConfiguration.cs`
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/ManageJobDescriptions.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/JobDescriptionRepository.cs`
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/JobDescriptionContracts.cs`
- Test: `backend/tests/EosDashboards.Domain.Tests/JobDescriptions/JobDescriptionVersionTests.cs`
- Test: `backend/tests/EosDashboards.Application.Tests/JobDescriptions/ManageJobDescriptionsTests.cs`

**Interfaces:**
- Consumes: `JobDescriptionQualityAssessment`.
- Produces: `JobDescriptionVersion.NeedsReview`, `SetCatalogQualityAssessment(bool hasBlockingIssues, bool needsReview, DateTime occurredAt)`, and repository/application list items carrying `NeedsReview`.

- [ ] **Step 1: Write failing domain and application tests**

  Add a domain test proving `SetCatalogQualityAssessment(false, true, ...)` leaves `QualityStatus` healthy and sets `NeedsReview`. Add an application test proving a version with one missing required skill is created as `PendingDepartmentApproval`, healthy, and review-marked; a version with a missing task start date remains `PendingDataCompletion`.

- [ ] **Step 2: Run the focused tests to verify they fail**

  ```powershell
  dotnet test backend/tests/EosDashboards.Domain.Tests/EosDashboards.Domain.Tests.csproj --filter "FullyQualifiedName~JobDescriptionVersionTests" --no-restore
  dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter "FullyQualifiedName~ManageJobDescriptionsTests" --no-restore
  ```

  Expected: the domain has no persisted review property and the application still treats the missing required skill as a catalog-quality blocker.

- [ ] **Step 3: Implement the domain and recalculation changes**

  Add the private `_needsReview` field, public `NeedsReview` getter, EF mapping, and `SetCatalogQualityAssessment`. Update create/revise/department-approval/HR-approval/catalog-revalidation paths to compute `JobDescriptionQualityAssessment.From(findings)` and pass both flags. Preserve workflow transitions for blocking findings only.

- [ ] **Step 4: Run the focused tests to verify they pass**

  Re-run both focused commands. Expected: all pass, including existing blocking-quality tests.

- [ ] **Step 5: Commit the domain/application change**

  ```powershell
  git add backend/src/EosDashboards.Domain/Entities/JobDescriptionVersion.cs backend/src/EosDashboards.Infrastructure/Persistence/Configurations/JobDescriptionVersionConfiguration.cs backend/src/EosDashboards.Application/JobDescriptions backend/src/EosDashboards.Infrastructure/Persistence/Repositories/JobDescriptionRepository.cs backend/tests/EosDashboards.Domain.Tests/JobDescriptions/JobDescriptionVersionTests.cs backend/tests/EosDashboards.Application.Tests/JobDescriptions/ManageJobDescriptionsTests.cs
  git commit -m "feat: persist job description review warnings"
  ```

### Task 3: Extend scoped API contracts and warning feed

**Files:**
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/JobDescriptionContracts.cs`
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/GetDepartmentDashboard.cs`
- Create: `backend/src/EosDashboards.Application/JobDescriptions/GetJobDescriptionReviewWarnings.cs`
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/AnalyzeJobDescription.cs`
- Modify: `backend/src/EosDashboards.Api/JobDescriptions/JobDescriptionContracts.cs`
- Modify: `backend/src/EosDashboards.Api/JobDescriptions/JobDescriptionEndpoints.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/JobDescriptionRepository.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/JobDescriptionScopeReader.cs`
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/JobDescriptionContracts.cs`
- Test: `backend/tests/EosDashboards.Application.Tests/JobDescriptions/ManageJobDescriptionsTests.cs`

**Interfaces:**
- Consumes: persisted `NeedsReview`, managed-department scope, and CEO role scope.
- Produces: `needsReview` on list/detail/operation responses, `needsReviewCount` in department metrics, and `GET /api/v1/job-descriptions/review-warnings` for authorized managers and CEOs.

- [ ] **Step 1: Write failing contract/application tests**

  Assert that a list item and operation result expose `NeedsReview`, the department metric includes the review count, and the warning query rejects a non-manager/non-CEO actor while returning task/skill warning rows for a scoped actor.

- [ ] **Step 2: Run focused backend tests to verify they fail**

  ```powershell
  dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter "FullyQualifiedName~JobDescription" --no-restore
  ```

  Expected: the new contracts and query are missing.

- [ ] **Step 3: Implement the minimal API and scope**

  Add `CanReviewAsChiefExecutiveAsync` to `IJobDescriptionScope` and implement it using the active `ChiefExecutiveOfficer` role. Add a repository query that returns active, non-approved, non-archived versions with their missing required task skills and catalog names. Keep the query read-only. Map Persian workflow/quality labels as existing endpoints do.

- [ ] **Step 4: Run focused backend tests to verify they pass**

  Re-run the command from Step 2 and build the API project. Expected: all pass and the endpoint compiles.

- [ ] **Step 5: Commit the API change**

  ```powershell
  git add backend/src/EosDashboards.Application/JobDescriptions backend/src/EosDashboards.Api/JobDescriptions backend/src/EosDashboards.Infrastructure/Persistence/Repositories backend/tests/EosDashboards.Application.Tests/JobDescriptions/ManageJobDescriptionsTests.cs
  git commit -m "feat: expose scoped job description review warnings"
  ```

### Task 4: Add schema migration and safe active-version backfill

**Files:**
- Create: `backend/src/EosDashboards.Infrastructure/Persistence/Migrations/20260905170000_AddJobDescriptionReviewWarning.cs`
- Generated/modify: matching migration designer and `EosDashboardDbContextModelSnapshot.cs`
- Modify: `docs/operations/iis-deployment.md`
- Test/verification: SQL backup, EF migration update, and read-only verification query

**Interfaces:**
- Consumes: the current database after `20260905153600_RevalidateExistingJobDescriptionQuality`.
- Produces: `NeedsReview` column and corrected active-version statuses.

- [ ] **Step 1: Write the migration verification query before applying it**

  The verification must check that approved/archived rows are unchanged, active rows with only missing required skills have `HasCatalogQualityIssues = 0`, `WorkflowStatus = PendingDepartmentApproval`, and `NeedsReview = 1`, and active rows with structural defects retain `PendingDataCompletion`.

- [ ] **Step 2: Generate the migration and inspect SQL**

  Add the property to the EF model, create the migration with the repository's standard `dotnet ef migrations add` command, and inspect the generated SQL. The data section must ignore unrelated selected skills, set `NeedsReview` from missing required-skill joins, and never update `Approved` or `Archived` rows.

- [ ] **Step 3: Back up and apply once**

  Create and verify a new development SQL backup, apply the migration with the documented Release/Development EF command, and run the verification query. Do not roll back or edit an already-applied migration.

- [ ] **Step 4: Commit migration and operations documentation**

  ```powershell
  git add backend/src/EosDashboards.Infrastructure/Persistence/Migrations docs/operations/iis-deployment.md
  git commit -m "feat: backfill job description review warnings"
  ```

### Task 5: Render warnings in manager and CEO surfaces

**Files:**
- Modify: `frontend/src/features/jobDescriptions/jobDescriptionsApi.ts`
- Modify: `frontend/src/pages/DepartmentJobDescriptionsPage.tsx`
- Modify: `frontend/src/pages/DepartmentDashboardPage.tsx`
- Modify: `frontend/src/pages/ChiefExecutiveDashboardPage.tsx`
- Modify: `frontend/src/pages/DepartmentJobDescriptionsPage.test.tsx`
- Create/modify: `frontend/src/pages/ChiefExecutiveDashboardPage.test.tsx`
- Modify: `frontend/src/pages/DepartmentDashboardPage.test.tsx`

**Interfaces:**
- Consumes: `needsReview`, `needsReviewCount`, and the warning-feed response.
- Produces: independent Persian warning chips/panels that do not disable approval when quality is healthy.

- [ ] **Step 1: Write failing component tests**

  Add assertions that a healthy row with `needsReview: true` shows `نیازمند بررسی` and still exposes the approval action; the department dashboard shows the review count; and the CEO page renders the warning list with person, task, and skill.

- [ ] **Step 2: Run the focused frontend tests to verify they fail**

  ```powershell
  npm --prefix frontend test -- --run src/pages/DepartmentJobDescriptionsPage.test.tsx src/pages/DepartmentDashboardPage.test.tsx src/pages/ChiefExecutiveDashboardPage.test.tsx
  ```

  Expected: the current types and placeholder CEO page do not render the new warning state.

- [ ] **Step 3: Implement the minimal UI**

  Extend API types and queries. Add the warning chip beside the quality chip, preserve the existing healthy approval condition, add a dashboard metric/card, and replace only the CEO placeholder content with the read-only warning panel. Reuse existing MUI cards, RTL layout, Persian-number formatting, and page help infrastructure.

- [ ] **Step 4: Run focused frontend tests to verify they pass**

  Re-run the command from Step 2, then run frontend typecheck and formatting on changed files.

- [ ] **Step 5: Commit the UI change**

  ```powershell
  git add frontend/src/features/jobDescriptions/jobDescriptionsApi.ts frontend/src/pages/DepartmentJobDescriptionsPage.tsx frontend/src/pages/DepartmentDashboardPage.tsx frontend/src/pages/ChiefExecutiveDashboardPage.tsx frontend/src/pages/DepartmentJobDescriptionsPage.test.tsx frontend/src/pages/ChiefExecutiveDashboardPage.test.tsx frontend/src/pages/DepartmentDashboardPage.test.tsx
  git commit -m "feat: show job description review warnings"
  ```

### Task 6: Update canonical project memory and verify the integrated change

**Files:**
- Modify: `docs/project/current-state.md`
- Modify: `docs/project/standards.md` only if the reusable error-learning rule changes
- Modify or create: `docs/project/decisions/` only if the warning semantics need a durable rationale
- Modify: `scripts/Finalize-LocalIisRelease.ps1` and `scripts/Publish-LocalIisRelease.ps1` with the latest migration default

- [ ] **Step 1: Run the focused backend and frontend suites**

  Run the changed backend tests, changed frontend tests, backend application/domain tests, frontend full suite, typecheck, production build, lint, and `git diff --check`. Do not repeat successful commands unless source/configuration changes afterward.

- [ ] **Step 2: Publish the local IIS release**

  Use the canonical elevated finalizer with the new expected migration, verify API liveness/readiness, UI entry, SPA refresh, and the authenticated manager/CEO warning surfaces in Chrome. Preserve the existing UAC workflow and do not deploy to production.

- [ ] **Step 3: Verify the user scenario**

  Confirm the affected active version is `سالم` plus `نیازمند بررسی`, the department manager can still approve it, the CEO warning panel lists it, and a real blocking defect still shows `ناقص` and cannot be approved.

- [ ] **Step 4: Update current state with evidence**

  Record the root cause, durable rule, migration, release identifier, tests, smoke results, and any remaining blocker in `current-state.md`. Keep it concise and do not record credentials or personal data.

- [ ] **Step 5: Commit, merge, push, and publish documentation state**

  ```powershell
  git add docs/project/current-state.md docs/project/standards.md docs/project/decisions scripts/Finalize-LocalIisRelease.ps1 scripts/Publish-LocalIisRelease.ps1
  git commit -m "docs: record job description review warning behavior"
  git push origin main
  ```

