# Unresolved Catalog Resolution Implementation Plan

**Status:** Implemented and verified on 2026-09-04

> **For agentic workers:** Execute this plan inline in the current isolated feature branch. Do not use subagents for this repository; run each focused verification checkpoint before moving to the next task.

**Goal:** Preserve unmatched Excel skills and tasks, let a department manager resolve them to existing or new catalog values, and prevent incomplete descriptions from entering Human Resources review.

**Architecture:** Add version-owned unresolved-value collections in Domain and Infrastructure. Application import and revision flows keep unresolved source text and derive `منتظر رفع نقص` from the same quality invariant that the Domain approval method enforces. React detail/edit surfaces expose the raw values and use existing scoped catalog endpoints to create or link values; the database revision remains canonical and generated Excel remains a derivative.

**Tech Stack:** .NET 10, EF Core 10, SQL Server, xUnit, React 19.2, TypeScript, Material UI, TanStack Query, Vitest, Persian RTL UI.

**Spec:** `docs/superpowers/specs/2026-09-04-unresolved-catalog-resolution-design.md`

## Global Constraints

- Keep every readable imported skill and task value in the database; never silently discard an unmatched value.
- An incomplete record uses workflow status `منتظر رفع نقص` and cannot enter Human Resources review.
- After resolution, return to `منتظر تأیید`; resolving data never approves a record automatically.
- The manager can choose public or target-department skills and can set the project flag when creating a new task.
- Keep immutable version history and generate Excel from persisted database data.
- Do not use fuzzy matching, an external AI service, or database mutation during source-file verification.
- Use focused tests and avoid repeated full-suite runs.

---

### Task 1: Add the domain representation and workflow invariant

**Files:**
- Modify: `backend/src/EosDashboards.Domain/Enums/JobDescriptionWorkflowStatus.cs`
- Modify: `backend/src/EosDashboards.Domain/Entities/JobDescriptionVersion.cs`
- Create: `backend/src/EosDashboards.Domain/Entities/JobDescriptionVersionUnresolvedSkill.cs`
- Create: `backend/src/EosDashboards.Domain/Entities/JobDescriptionVersionUnresolvedTask.cs`
- Modify: `backend/tests/EosDashboards.Domain.Tests/JobDescriptions/JobDescriptionVersionTests.cs` or create it if absent

**Interfaces:**
- Produces `JobDescriptionWorkflowStatus.PendingDataCompletion`, `JobDescriptionVersion.UnresolvedSkills`, `JobDescriptionVersion.UnresolvedTasks`, and factory inputs that retain raw values.
- Keeps `JobDescriptionVersion.QualityStatus` as the single domain quality calculation and makes `ApproveByDepartmentManager(DateTime)` reject non-healthy versions.

- [ ] **Step 1: Write failing domain tests**

Cover these concrete behaviors:

```csharp
[Fact]
public void Incomplete_version_starts_in_pending_data_completion()
{
    var version = JobDescriptionVersion.Create(
        "پرسنل نمونه", 1, null, "", "", "", [],
        [JobDescriptionTask.Create(1, "وظیفه", "شرح", null, null, 1)],
        ["مهارت خام"],
        [new UnresolvedTaskInput("وظیفه خام", "شرح خام", null, null, 2)],
        new DateTime(2026, 9, 4));

    Assert.Equal(JobDescriptionWorkflowStatus.PendingDataCompletion, version.WorkflowStatus);
    Assert.Equal(JobDescriptionQualityStatus.Incomplete, version.QualityStatus);
    Assert.Single(version.UnresolvedSkills);
    Assert.Single(version.UnresolvedTasks);
}

[Fact]
public void Department_approval_rejects_an_incomplete_version()
{
    var version = CreateIncompleteVersion();

    Assert.Throws<InvalidOperationException>(() =>
        version.ApproveByDepartmentManager(new DateTime(2026, 9, 4)));
}
```

Use a small public `UnresolvedTaskInput` record in the Domain or Application contract, keeping raw task title, description, dates, and sort order explicit. Preserve current required-field rules, including personnel code and task start date.

- [ ] **Step 2: Run the focused domain tests and verify the expected failure**

Run:

```powershell
dotnet test backend/tests/EosDashboards.Domain.Tests/EosDashboards.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~JobDescriptionVersionTests
```

Expected: the new tests fail because the status value, unresolved collections, and factory inputs do not exist yet.

- [ ] **Step 3: Implement the minimum domain model**

Add `PendingDataCompletion`. Add immutable version-owned unresolved collections with validation for non-empty raw values. Extend `JobDescriptionVersion.Create` with unresolved skill names and unresolved task inputs, initialize workflow status from `QualityStatus`, and include unresolved collections in `QualityStatus`. Add a domain guard at the start of `ApproveByDepartmentManager` that throws when quality is incomplete. Keep existing transition rules for rejected, approved, and archived versions.

- [ ] **Step 4: Run the focused domain tests**

Run the same command. Expected: all `JobDescriptionVersionTests` pass with zero failures.

---

### Task 2: Persist unresolved values and update the migration

**Files:**
- Create: `backend/src/EosDashboards.Infrastructure/Persistence/Configurations/JobDescriptionVersionUnresolvedSkillConfiguration.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Persistence/Configurations/JobDescriptionVersionUnresolvedTaskConfiguration.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/EosDashboardDbContext.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/JobDescriptionRepository.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/JobDescriptionImportReader.cs`
- Create: EF Core migration under `backend/src/EosDashboards.Infrastructure/Persistence/Migrations/`
- Modify: `backend/tests/EosDashboards.IntegrationTests/Database/ModelMappingTests.cs`

**Interfaces:**
- Produces `DbSet<JobDescriptionVersionUnresolvedSkill>` and `DbSet<JobDescriptionVersionUnresolvedTask>` with required foreign keys to `JobDescriptionVersions` and cascade deletion only with the owning version.
- Repository version reads must include both unresolved collections; version writes must persist them in the same unit-of-work transaction.

- [ ] **Step 1: Add failing mapping assertions**

Extend `ModelMappingTests` to assert raw skill name, raw task title/description, nullable task dates, sort order, and version foreign keys are mapped with the documented Unicode string lengths and SQL Server types. Add an integration test that saves a version with one unresolved skill and one unresolved task and reads both back.

- [ ] **Step 2: Run the focused integration tests and verify failure**

Run:

```powershell
dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~ModelMappingTests
```

Expected: compilation or assertion failures because the new entities and mappings are absent.

- [ ] **Step 3: Implement entities, EF configuration, and repository loading**

Use `long` `Id` primary keys, `nvarchar` fields with explicit maximum lengths, local application times only where timestamps are needed, and no lazy loading. Update repository create/read paths so unresolved collections are loaded explicitly with `Include`/`ThenInclude` or projections, following existing no-tracking projection conventions for list reads.

- [ ] **Step 4: Create and apply one additive migration**

Run the repository’s EF migration command, inspect the generated migration for only the two unresolved tables, foreign keys, indexes, and no destructive operation, then apply it to the configured local Development database.

- [ ] **Step 5: Run the focused integration tests**

Run the same focused command and expect all mapping/persistence tests to pass.

---

### Task 3: Preserve unmatched import values and enforce application workflow

**Files:**
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/JobDescriptionWorkbookContracts.cs`
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/ImportJobDescriptions.cs`
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/ManageJobDescriptions.cs`
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/JobDescriptionContracts.cs`
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/ManageCatalog.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/JobDescriptions/ExcelJobDescriptionWorkbookAdapter.cs` only if the parser contract needs raw-row metadata
- Modify: focused Application and Integration tests under `backend/tests/`

**Interfaces:**
- `ImportedJobDescriptionWorkbook` continues to expose all readable source tasks and skill names.
- `CreateJobDescriptionCommand`/revision input carries `IReadOnlyCollection<string> UnresolvedSkillNames` and `IReadOnlyCollection<UnresolvedTaskInput> UnresolvedTasks`.
- `JobDescriptionVersion.Create` receives matched catalog IDs plus unresolved values and creates `PendingDataCompletion` when required.

- [ ] **Step 1: Write failing import and revision tests**

Add a parser/import test whose catalog reader matches one skill and one task while returning one unmatched skill and one unmatched task. Assert the resulting version retains the unmatched raw values, reports `ناقص`, and has workflow status `منتظر رفع نقص`. Add an application test that an incomplete version cannot be approved.

- [ ] **Step 2: Run the focused tests and verify failure**

Run:

```powershell
dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~JobDescriptions
dotnet test backend/tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --no-restore --filter FullyQualifiedName~JobDescriptions
```

Expected: the tests fail because import currently filters unmatched values out and application contracts do not carry them.

- [ ] **Step 3: Implement preservation and status derivation**

Partition source tasks into matched catalog tasks and unresolved task inputs without changing source order. Partition source skills similarly. Create the version with both sets. Keep suggestions in the per-file import result, but make them informational because the actual raw values now live in the draft. On revision, retain unresolved values that the edit request did not resolve. Map `PendingDataCompletion` to `منتظر رفع نقص` in all application status projections.

- [ ] **Step 4: Run focused tests**

Run both commands from Step 2. Expected: all focused import, revision, and approval tests pass.

---

### Task 4: Expose unresolved values and block approval at the API boundary

**Files:**
- Modify: `backend/src/EosDashboards.Api/JobDescriptions/JobDescriptionContracts.cs`
- Modify: `backend/src/EosDashboards.Api/JobDescriptions/JobDescriptionEndpoints.cs`
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/ManageJobDescriptions.cs`
- Modify: API/integration tests under `backend/tests/EosDashboards.IntegrationTests/JobDescriptions/`

**Interfaces:**
- Detail response exposes selected catalog skills/tasks and unresolved skills/tasks with raw task description and dates.
- List response exposes `منتظر رفع نقص` and an explicit `canApprove`/quality-blocking result if the current response contract needs it.
- `POST /api/v1/job-descriptions/{versionId}/department-approval` returns a stable `incomplete_job_description` problem code and summaries when the quality status is incomplete.

- [ ] **Step 1: Write failing API/application tests**

Assert that detail returns raw unresolved values, a list item uses `منتظر رفع نقص`, and a department approval attempt returns HTTP 409 (or the existing project-standard conflict status) with the stable problem code and no workflow mutation.

- [ ] **Step 2: Run the tests and verify failure**

Run the focused `JobDescriptions` integration test filter. Expected: unresolved fields and the new problem response are absent.

- [ ] **Step 3: Implement contracts and endpoint guard**

Project unresolved collections into DTOs, map the new localized status, and check `version.QualityStatus` before calling the domain approval method. Return the existing safe problem envelope with an added stable code and compact raw-value summaries. Keep authorization and department scope checks before data access.

- [ ] **Step 4: Run focused API tests and build**

Run:

```powershell
dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~JobDescriptions
dotnet build backend/src/EosDashboards.Api/EosDashboards.Api.csproj --no-restore
```

Expected: focused API tests pass and the build reports zero errors.

---

### Task 5: Make generated Excel and detail data loss-proof

**Files:**
- Modify: `backend/src/EosDashboards.Infrastructure/JobDescriptions/ExcelJobDescriptionWorkbookAdapter.cs`
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/ManageJobDescriptions.cs`
- Modify: `backend/tests/EosDashboards.IntegrationTests/JobDescriptions/ExcelJobDescriptionWorkbookAdapterTests.cs`

**Interfaces:**
- `IJobDescriptionWorkbookGenerator.Generate(JobDescriptionVersion version, DateOnly asOf)` emits unresolved skill names and unresolved task title/description rows while the version is incomplete.

- [ ] **Step 1: Write failing generator round-trip tests**

Create a version with one selected catalog skill, one unresolved skill, one selected task, and one unresolved task. Generate and parse the workbook. Assert the unresolved names and task content are present rather than an empty table.

- [ ] **Step 2: Run the focused adapter tests and verify failure**

Run:

```powershell
dotnet test backend/tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~ExcelJobDescriptionWorkbookAdapterTests
```

Expected: unresolved values are missing from the generated workbook.

- [ ] **Step 3: Implement derivative workbook output**

Append unresolved skills to the skill source field and unresolved tasks to the task rows while retaining source order. Keep past-ended task rows omitted from the current artifact only when they are resolved catalog tasks under the existing rule. Do not make the generator the source of quality or statistics.

- [ ] **Step 4: Run the adapter tests**

Run the same command and expect all adapter tests to pass.

---

### Task 6: Add manager resolution controls to detail and edit forms

**Files:**
- Modify: `frontend/src/features/jobDescriptions/jobDescriptionsApi.ts`
- Modify: `frontend/src/pages/DepartmentJobDescriptionsPage.tsx`
- Modify: `frontend/src/pages/DepartmentJobDescriptionsPage.test.tsx`
- Modify: `frontend/src/pages/DepartmentCatalogPage.tsx` only if a shared catalog-create helper is required

**Interfaces:**
- The edit form receives unresolved skills/tasks and sends retained unresolved values plus selected IDs in the existing revise request.
- Skill resolution lists public skills and target-department skills; new skill creation accepts public or department-specific scope.
- Task resolution lists target-department tasks; new task creation includes `isProject` and immediately links the created ID.

- [ ] **Step 1: Write failing React tests**

Add tests that render a detail/edit state with unresolved skill/task values and assert:

```ts
expect(screen.getByText("مهارت خام")).toBeInTheDocument();
expect(screen.getByText("وظیفه خام")).toBeInTheDocument();
expect(screen.getByRole("button", { name: "ایجاد مهارت جدید" })).toBeInTheDocument();
expect(screen.getByRole("checkbox", { name: "پروژه" })).toBeInTheDocument();
```

Assert that the manager approval control is disabled or absent while the draft is `منتظر رفع نقص`, and that the resolution choices include a public skill and a target-department skill.

- [ ] **Step 2: Run the focused React tests and verify failure**

Run:

```powershell
Set-Location frontend
npm test -- --run src/pages/DepartmentJobDescriptionsPage.test.tsx
```

Expected: the raw values, project checkbox, and approval gate controls are absent from the current UI.

- [ ] **Step 3: Implement API types and query/mutation flow**

Extend detail/list types with unresolved values and the new workflow status. In the edit dialog, keep unresolved state in local form state. For each unresolved skill, render a controlled select of public plus target-department skills and a create-new action with scope choice. For each unresolved task, render a controlled select of target tasks or create-new action with a visible project checkbox. On successful creation, append the returned catalog ID and remove only that raw item from the unresolved collection. Save with the existing revision mutation so the previous version remains retained.

- [ ] **Step 4: Implement status-aware manager actions**

Render `منتظر رفع نقص` as a warning workflow chip. Disable the manager approval action for incomplete items and show a concise reason listing missing/unresolved data. Keep view/download/edit available within authorization scope.

- [ ] **Step 5: Run focused React tests**

Run the same Vitest command and expect all page tests to pass.

---

### Task 7: Verify the integrated flow and update durable state

**Files:**
- Modify: `docs/project/requirements.md`
- Modify: `docs/project/current-state.md`
- Modify: `docs/project/architecture.md` if the final persistence boundary wording needs the new unresolved collections
- Modify: focused test files only where verification exposes a real regression

- [ ] **Step 1: Run the compact backend checkpoint**

Run the focused Domain, Application, Integration, and API build commands from earlier tasks once after all backend changes. Do not rerun successful commands without a code/configuration change.

- [ ] **Step 2: Run the compact frontend checkpoint**

Run:

```powershell
Set-Location frontend
npm test -- --run src/pages/DepartmentJobDescriptionsPage.test.tsx src/pages/DepartmentCatalogPage.test.tsx src/pages/DepartmentDashboardPage.test.tsx
npm run typecheck
npm run format:check
npm run lint
```

Record existing lint warnings separately; the changed files must introduce no lint errors.

- [ ] **Step 3: Run the safe read-only source-file check**

Use the already loaded bundled runtime to parse every `.xlsx` under `D:\TMP` without calling the import endpoint or writing to the database. Confirm readable raw values remain available for the supplied formats.

- [ ] **Step 4: Run one internal-browser smoke flow**

The flow under test is: `http://127.0.0.1:5173/department-job-descriptions` -> show an incomplete draft or import result -> open detail/edit -> see raw unresolved values and resolution controls -> observe `منتظر رفع نقص` and the blocked approval action. Also open the task catalog resolution control and confirm the project checkbox and public/department skill choices are visible. Capture a fresh DOM/screenshot and check console errors.

- [ ] **Step 5: Update canonical documentation**

Record the implemented status transition, unresolved persistence, API approval gate, and resolution controls in `requirements.md`, `current-state.md`, and the architecture boundary if needed. Include the symptom, root cause, remedy, and focused verification evidence. Do not store personal workbook contents.

- [ ] **Step 6: Inspect the final diff**

Run `git diff --check` and `git status --short`. Confirm only the intended feature files, migration, tests, and canonical documentation changed; preserve unrelated worktree changes.
