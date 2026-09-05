# Human Resources dashboard and job-description management implementation plan

> **For inline execution:** Execute this plan in this session with focused test checkpoints. Do not use subagents or parallel review loops.

**Goal:** Deliver the approved Human Resources dashboard and unified `مدیریت شرح وظایف` workspace with department filtering, change history, version comparison, review/download/approve/reject actions, and public-skill management including safe merge.

**Architecture:** Extend the existing `EosDashboards.Application.JobDescriptions` vertical slice. Add Human Resources read models and use cases over the structured job-description version store, keep API contracts separate from persistence entities, and perform public-skill merge in one Infrastructure-backed transaction with existing audit conventions. Extend the existing React workspace targets and query client rather than creating a second navigation or authorization system.

**Tech Stack:** .NET 10, EF Core 10, SQL Server, React 19.2, TypeScript, Material UI 9, TanStack Query, Vitest/Testing Library, Playwright.

**Spec:** `docs/superpowers/specs/2026-09-05-human-resources-dashboard-and-job-description-management-design.md`

## Global Constraints

- Human Resources authorization is enforced on the server for every dashboard, list, comparison, download, review, catalog, and merge operation.
- The structured database version is canonical; generated Excel is a download representation and is never parsed for metrics, history, or comparison.
- `همه بخش‌ها` is the first visible department-selector value and represents an explicit all-departments selection.
- All persisted application timestamps remain application-server local `datetime2(3)` values without `Utc` names or Tehran conversion.
- All user-visible dates use the Persian calendar, and all visible numbers use Persian digits; API and internal identifiers retain contract representation.
- Every destructive catalog operation and merge requires explicit Persian confirmation; cancel, close, and backdrop dismissal send no mutation request.
- Existing manager and department-manager behavior must remain compatible.
- Use focused red-green tests before production code for each new behavior; do not run frontend commands concurrently when both invoke `sync:resources`.
- Preserve the untracked `output/` and `tmp/` directories and do not stage them.

---

### Task 1: Define Human Resources application contracts and comparison models

**Files:**
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/JobDescriptionContracts.cs`
- Create: `backend/src/EosDashboards.Application/JobDescriptions/HumanResourcesDashboardContracts.cs`
- Create: `backend/src/EosDashboards.Application/JobDescriptions/CompareJobDescriptionVersions.cs`
- Test: `backend/tests/EosDashboards.Application.Tests/JobDescriptions/HumanResourcesDashboardTests.cs`
- Test: `backend/tests/EosDashboards.Application.Tests/JobDescriptions/CompareJobDescriptionVersionsTests.cs`

**Interfaces:**
- Consumes: `IJobDescriptionScope`, `IJobDescriptionRepository`, `IJobDescriptionDepartmentReader`, `IClock`, and existing `JobDescriptionVersion` entities.
- Produces: `HumanResourcesDashboardResult`, `HumanResourcesChangeSummary`, `HumanResourcesChangeItem`, `JobDescriptionComparisonResult`, and an Application operation that authorizes Human Resources before reading.

- [ ] **Step 1: Write failing application tests for Human Resources access and department selection.**

  Assert that a Human Resources actor receives all-department metrics when the selected department is null, receives only the selected department when it is in the returned department set, and receives no result for an unauthorized or nonexistent department. Assert that a non-Human-Resources actor cannot obtain the result. Use the existing fake scope/clock/repository patterns in the job-description application tests.

- [ ] **Step 2: Run the focused tests and verify the expected missing-contract/application failure.**

  Run from `backend`:

  ```powershell
  dotnet test tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter "FullyQualifiedName~HumanResourcesDashboardTests"
  ```

  Expected result: a compile or test failure because the new result and handler do not exist yet. Fix only test setup errors until the failure is caused by the missing behavior.

- [ ] **Step 3: Write failing comparison tests.**

  Cover a current version with a previous version and assert field changes, added/removed skills, added/removed tasks, changed task dates/weekly hours/descriptions, and changed workflow/quality values. Cover a current version without a previous version and assert an explicit no-previous-version result. Cover a version outside Human Resources authorization.

- [ ] **Step 4: Implement the smallest Application contracts and comparison mapper.**

  Add immutable records that carry current/previous snapshots and field-level differences. Add repository methods for an authorized current version and its immediately previous retained version. Keep comparison logic deterministic and in Application; do not expose EF entities or Excel content.

- [ ] **Step 5: Run the focused application tests and refactor only after green.**

  Run both focused filters. Expected result: all tests pass with no unrelated test changes. Keep the comparison result stable enough for the API and React clients to render Persian labels without duplicating business rules.

- [ ] **Step 6: Commit the cohesive contract/comparison slice.**

  ```powershell
  git add backend/src/EosDashboards.Application/JobDescriptions/JobDescriptionContracts.cs backend/src/EosDashboards.Application/JobDescriptions/HumanResourcesDashboardContracts.cs backend/src/EosDashboards.Application/JobDescriptions/CompareJobDescriptionVersions.cs backend/tests/EosDashboards.Application.Tests/JobDescriptions/HumanResourcesDashboardTests.cs backend/tests/EosDashboards.Application.Tests/JobDescriptions/CompareJobDescriptionVersionsTests.cs
  git commit -m "feat: add human resources dashboard contracts"
  ```

### Task 2: Add Human Resources dashboard/history and version queries

**Files:**
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/GetDepartmentDashboard.cs` only if shared metric records need extraction; otherwise leave existing manager use case unchanged
- Create: `backend/src/EosDashboards.Application/JobDescriptions/GetHumanResourcesDashboard.cs`
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/ManageJobDescriptions.cs`
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/JobDescriptionContracts.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/JobDescriptionRepository.cs`
- Create: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/HumanResourcesDashboardReader.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/DependencyInjection.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/JobDescriptions/HumanResourcesDashboardReaderTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/JobDescriptions/JobDescriptionRepositoryTests.cs`

**Interfaces:**
- Consumes: Task 1 result records and current `JobDescriptionVersions`, `JobDescriptionRecords`, departments, skills, tasks, and audit correlation services.
- Produces: role-authorized dashboard read data, department list for the Human Resources selector, review list filtered by optional department, approved latest-version list, and current/previous version lookup.

- [ ] **Step 1: Write failing SQL-backed reader tests for latest-version grouping and history aggregation.**

  Seed two retained versions for one record, one approved current version for another record, and records in two departments. Assert that the approved list returns only the latest approved active version per record, the review list honors a selected department, change summaries count retained versions by department, and recent changes are server-paged in descending update order. Use the existing isolated SQL fixture and avoid printing its connection string.

- [ ] **Step 2: Run the focused integration tests and verify the expected failure.**

  Run the repository’s established SQL-backed test command only after checking `ConnectionStrings__EosDashboardTests` points to the isolated catalog ending in `_IntegrationTests`:

  ```powershell
  dotnet test tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --filter "FullyQualifiedName~HumanResourcesDashboardReaderTests|FullyQualifiedName~JobDescriptionRepositoryTests"
  ```

  Expected result: missing reader methods or failing assertions before implementation. If the isolated connection is unavailable, stop and record the environmental cause instead of repeating the command unchanged.

- [ ] **Step 3: Implement server-side read queries.**

  Add a dedicated dashboard reader that computes existing structured metrics, per-department change summaries, and a paged retained-version history. Add a Human Resources department-list query that returns names and IDs without reusing the Department Manager scope. Add latest-approved grouping by `JobDescriptionRecordId`, keeping standalone imported records valid. Add current/previous version loading with deterministic `CreatedAt`/`Id` ordering.

- [ ] **Step 4: Register the reader and update Application orchestration.**

  Register the reader in `DependencyInjection.cs`. Make the Application handlers enforce `CanReviewAsHumanResourcesAsync` before calling it. Validate positive page size/page number and selected department membership in the Application boundary.

- [ ] **Step 5: Run the focused tests again and verify green.**

  Re-run the same application and integration filters. Confirm that the query never reads generated Excel bytes and that no manager-scope behavior changed.

- [ ] **Step 6: Commit the dashboard/read-model slice.**

  ```powershell
  git add backend/src/EosDashboards.Application/JobDescriptions backend/src/EosDashboards.Infrastructure/Persistence/Repositories/JobDescriptionRepository.cs backend/src/EosDashboards.Infrastructure/Persistence/Repositories/HumanResourcesDashboardReader.cs backend/src/EosDashboards.Infrastructure/DependencyInjection.cs backend/tests/EosDashboards.IntegrationTests/JobDescriptions
  git commit -m "feat: add human resources dashboard queries"
  ```

### Task 3: Implement public-skill merge and audit-safe catalog mutation

**Files:**
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/JobDescriptionContracts.cs`
- Modify: `backend/src/EosDashboards.Application/JobDescriptions/ManageCatalog.cs`
- Modify: `backend/src/EosDashboards.Application/Abstractions/IAuditWriter.cs` only if the existing nullable subject path needs correction
- Modify: `backend/src/EosDashboards.Domain/Entities/SkillCatalogItem.cs` if a merge-specific domain transition is needed
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/Repositories/JobDescriptionRepository.cs`
- Modify: `backend/src/EosDashboards.Infrastructure/Persistence/EfUnitOfWork.cs` only if the existing transaction boundary needs an explicit wrapper
- Test: `backend/tests/EosDashboards.Application.Tests/JobDescriptions/ManageCatalogTests.cs`
- Test: `backend/tests/EosDashboards.Domain.Tests/JobDescriptions/SkillCatalogItemTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Database/RepositoryTests.cs`

**Interfaces:**
- Consumes: two public active skill IDs, `IHumanResourcesCatalogReader`, `IJobDescriptionCatalogReader`, `IAuditWriter`, `IUnitOfWork`, `ICorrelationContext`, and the current EF junction tables.
- Produces: `MergePublicSkillAsync(actorUserId, sourceSkillId, survivingSkillId, cancellationToken)` returning a stable catalog operation result and an atomic persisted merge.

- [ ] **Step 1: Write failing tests for merge invariants.**

  Assert that a Human Resources actor can merge two distinct active public skills; all version-skill references and task-required-skill references move to the surviving ID; duplicate links collapse; the source becomes inactive; the target remains active; and the audit metadata identifies the source and surviving names without a department. Assert same-ID, inactive, department-specific, missing, duplicate-name/conflict, and unauthorized requests are rejected.

- [ ] **Step 2: Run the focused tests and verify the expected red state.**

  ```powershell
  dotnet test tests/EosDashboards.Application.Tests/EosDashboards.Application.Tests.csproj --filter "FullyQualifiedName~ManageCatalogTests|FullyQualifiedName~SkillCatalogItemTests"
  ```

  Expected result: the merge operation is absent or the new invariants fail.

- [ ] **Step 3: Implement the application operation and repository transaction.**

  Add the merge command and operation status. Load both public skills for update, authorize Human Resources, validate active/distinct IDs, reassign both junction collections, remove duplicate composite-key rows before inserting replacements, deactivate the source, write one safe audit event, and save through one unit-of-work boundary. Preserve cancellation and optimistic-concurrency behavior; never physically delete the source.

- [ ] **Step 4: Add/adjust the minimum persistence test coverage.**

  Use the SQL-backed repository test to prove the transaction leaves no partial links when a conflict is raised. Do not create a migration unless the model truly changes; the merge should use existing tables and soft-deactivation.

- [ ] **Step 5: Run focused domain/application/integration tests and inspect audit metadata.**

  Confirm that only safe IDs/names/event code are stored, no private values appear, and the duplicate-link result is deterministic.

- [ ] **Step 6: Commit the merge slice.**

  ```powershell
  git add backend/src/EosDashboards.Application/JobDescriptions backend/src/EosDashboards.Domain/Entities/SkillCatalogItem.cs backend/src/EosDashboards.Infrastructure/Persistence/Repositories/JobDescriptionRepository.cs backend/src/EosDashboards.Infrastructure/Persistence/EfUnitOfWork.cs backend/tests/EosDashboards.Application.Tests/JobDescriptions/ManageCatalogTests.cs backend/tests/EosDashboards.Domain.Tests/JobDescriptions/SkillCatalogItemTests.cs backend/tests/EosDashboards.IntegrationTests/Database/RepositoryTests.cs
  git commit -m "feat: merge public skills safely"
  ```

### Task 4: Expose protected Human Resources API endpoints

**Files:**
- Modify: `backend/src/EosDashboards.Api/JobDescriptions/JobDescriptionContracts.cs`
- Modify: `backend/src/EosDashboards.Api/JobDescriptions/JobDescriptionEndpoints.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Api/JobDescriptionEndpointTests.cs`
- Test: `backend/tests/EosDashboards.IntegrationTests/Api/AdministrationEndpointTests.cs` only if shared role-fixture helpers need a focused extension

**Interfaces:**
- Consumes: Task 1–3 Application handlers and existing `ActiveUser` route group.
- Produces: JSON DTOs for Human Resources dashboard, department options, review/approved lists, comparison, public-skill merge, and stable problem codes.

- [ ] **Step 1: Write failing endpoint tests.**

  Assert anonymous requests are `401`, authenticated non-Human-Resources requests are `403`, and a Human Resources fixture can call dashboard, review list, approved list, comparison, public-skill merge, and existing download routes. Assert selected department filters are passed as IDs, all-departments returns combined results, rejection requires a nonblank reason, and merge returns conflict for identical IDs.

- [ ] **Step 2: Run the endpoint filter and verify red.**

  ```powershell
  dotnet test tests/EosDashboards.IntegrationTests/EosDashboards.IntegrationTests.csproj --filter "FullyQualifiedName~JobDescriptionEndpointTests"
  ```

- [ ] **Step 3: Add explicit request/response DTOs and endpoint mappings.**

  Add optional `departmentId`, `page`, and `pageSize` query parameters with server bounds. Add routes under the existing job-description group for Human Resources dashboard data, department options, approved descriptions, comparison, and public-skill merge. Map Application status values to safe Persian-facing API strings through the existing status helpers. Keep the existing Excel response and operation mapping intact.

- [ ] **Step 4: Run endpoint tests and verify green.**

  Check response shapes, status codes, trace identifiers, and absence of persistence entities. Run API build if the endpoint fixture requires the full startup project.

- [ ] **Step 5: Commit the protected API slice.**

  ```powershell
  git add backend/src/EosDashboards.Api/JobDescriptions backend/tests/EosDashboards.IntegrationTests/Api/JobDescriptionEndpointTests.cs backend/tests/EosDashboards.IntegrationTests/Api/AdministrationEndpointTests.cs
  git commit -m "feat: expose human resources management api"
  ```

### Task 5: Build the Human Resources dashboard UI

**Files:**
- Modify: `frontend/src/features/jobDescriptions/jobDescriptionsApi.ts`
- Create: `frontend/src/features/jobDescriptions/humanResourcesTypes.ts` if the API types become too large for the existing client
- Modify: `frontend/src/pages/HumanResourcesDashboardPage.tsx`
- Create: `frontend/src/pages/HumanResourcesDashboardPage.test.tsx`
- Modify: `frontend/src/components/pageGuides.ts`

**Interfaces:**
- Consumes: Task 4 dashboard/department API DTOs and existing `eos-accent-card`, Persian number/date helpers, and shell scroll rules.
- Produces: rendered dashboard with explicit all/one-department selector, metric cards, change summary, recent history, loading/error/empty states, and links to the management tab.

- [ ] **Step 1: Write failing component tests.**

  Add mocked-query tests that render the all-departments state, assert `همه بخش‌ها` is first, select one department and assert the next query key/request uses that ID, render Persian digits in metric counts and dates, show history rows and per-department summaries, and cover loading/error/no-history states.

- [ ] **Step 2: Run the focused frontend test and verify red.**

  From `frontend`:

  ```powershell
  npm test -- --run src/pages/HumanResourcesDashboardPage.test.tsx
  ```

  Expected result: the page is still the placeholder or the required assertions are absent.

- [ ] **Step 3: Add API client types and query functions.**

  Keep all API URLs under the existing `/api/v1/job-descriptions` boundary, encode IDs/page values, and return typed data. Do not put authorization decisions in the client.

- [ ] **Step 4: Implement the dashboard using the approved manager-facing visual system.**

  Use a fixed title/filter panel and bounded scroll regions for history. Use compact flat metric cards, a structured table for history and department summaries, explicit Persian labels, and a restrained action strip linking to `مدیریت شرح وظایف`. Keep the page full-width with the established 16px workspace gutter and mobile-safe wrapping.

- [ ] **Step 5: Add truthful help content and run the focused tests.**

  Add the four required guide sections for `human-resources-dashboard`. Expected result: focused component tests pass and no placeholder claims remain.

- [ ] **Step 6: Commit the dashboard UI slice.**

  ```powershell
  git add frontend/src/features/jobDescriptions frontend/src/pages/HumanResourcesDashboardPage.tsx frontend/src/pages/HumanResourcesDashboardPage.test.tsx frontend/src/components/pageGuides.ts
  git commit -m "feat: build human resources dashboard"
  ```

### Task 6: Replace review UI with unified management workspace

**Files:**
- Modify: `frontend/src/features/jobDescriptions/jobDescriptionsApi.ts`
- Modify: `frontend/src/pages/HumanResourcesJobDescriptionReviewPage.tsx`
- Create: `frontend/src/pages/HumanResourcesJobDescriptionManagementPage.test.tsx` or rename the existing test file while keeping test ownership clear
- Modify: `frontend/src/navigation/workspaceTargets.tsx`
- Modify: `frontend/src/pages/home/homeContent.ts`
- Modify: `frontend/src/components/pageGuides.ts`
- Modify: `frontend/src/navigation/workspaceTargets.test.tsx`

**Interfaces:**
- Consumes: Task 4 list/compare/merge API DTOs and existing `ConfirmActionDialog`, `MutationErrorAlert`, download helper, and query invalidation patterns.
- Produces: visible target title `مدیریت شرح وظایف`, three internal tabs, filtered review/approved lists, review dialog with download/approve/reject, compare dialog, and public-skill management/merge.

- [ ] **Step 1: Write failing component tests for the management workflow.**

  Cover the three tabs, all/one-department selector, review row opening the dialog, download invocation, approval mutation, rejection dialog requiring a reason, approved-list rendering, compare dialog showing a changed field, public-skill rename/deactivate/reactivate, merge dialog naming the surviving skill, and cancellation of reject/deactivate/merge sending no mutation request. Assert Persian labels and numbers rather than implementation details.

- [ ] **Step 2: Run the focused test file and verify red.**

  ```powershell
  npm test -- --run src/pages/HumanResourcesJobDescriptionReviewPage.test.tsx
  ```

  If the existing file does not cover the new surface, create the new focused test file and run it directly; do not run the full suite yet.

- [ ] **Step 3: Extend the API client.**

  Add typed department options, Human Resources dashboard/list, approved-list, comparison, and merge calls. Reuse `download(id)` for the matching canonical workbook and keep mutation query keys distinct for review, approved, dashboard, and catalog data.

- [ ] **Step 4: Refactor the page into focused subcomponents.**

  Keep the main page responsible for tab/selector/query state. Extract review table, approved table, review dialog, comparison dialog, and public-skill panel/merge dialog into focused local components or feature files when the page would otherwise grow beyond one responsibility. The review dialog must show actions in the same form; table actions open the dialog instead of approving immediately.

- [ ] **Step 5: Implement merge confirmation and mutation refresh.**

  Use two active public-skill selectors, write confirmation text with source and surviving names, disable same-item selection, and invalidate both catalog and dashboard/history keys after success. Keep names and entered rejection text on mutation failure.

- [ ] **Step 6: Update navigation and role-aware copy.**

  Change visible route title, keywords, home action label, and help content to `مدیریت شرح وظایف` while preserving the route ID only if needed for existing saved tabs. Add the required four Persian help sections describing actual released actions and limitations.

- [ ] **Step 7: Run focused component tests, typecheck, and formatting for changed files.**

  Run serially from `frontend`:

  ```powershell
  npm test -- --run src/pages/HumanResourcesJobDescriptionReviewPage.test.tsx
  npm run typecheck
  npx prettier --check src/features/jobDescriptions/jobDescriptionsApi.ts src/pages/HumanResourcesJobDescriptionReviewPage.tsx src/pages/HumanResourcesJobDescriptionReviewPage.test.tsx src/navigation/workspaceTargets.tsx src/navigation/workspaceTargets.test.tsx src/pages/home/homeContent.ts src/components/pageGuides.ts
  ```

- [ ] **Step 8: Commit the management UI slice.**

  ```powershell
  git add frontend/src/features/jobDescriptions frontend/src/pages/HumanResourcesJobDescriptionReviewPage.tsx frontend/src/pages/HumanResourcesJobDescriptionReviewPage.test.tsx frontend/src/navigation/workspaceTargets.tsx frontend/src/navigation/workspaceTargets.test.tsx frontend/src/pages/home/homeContent.ts frontend/src/components/pageGuides.ts
  git commit -m "feat: add human resources job description management"
  ```

### Task 7: Integrated verification and durable project memory

**Files:**
- Modify: `docs/project/current-state.md`
- Modify: `docs/project/requirements.md`
- Modify: `docs/project/architecture.md`
- Modify: `docs/project/roadmap.md`
- Modify: `AGENTS.md` only if a new durable project-wide rule was introduced
- Test/temporary outside repository: a dedicated temporary Playwright directory under the system temp path if the existing browser flow needs a new focused scenario

**Interfaces:**
- Consumes: all completed backend/frontend slices and the approved design/spec.
- Produces: fresh evidence for the integrated feature, current canonical memory, and a clean reviewable diff.

- [ ] **Step 1: Inspect the final diff and verify no unrelated tracked files changed.**

  Run `git status --short`, `git diff --stat main...HEAD`, and `git diff --check`. Confirm that `output/` and `tmp/` remain untracked and unstaged.

- [ ] **Step 2: Run focused backend verification at the integrated checkpoint.**

  Stop only the repository’s running API if it holds referenced assemblies. Build the complete backend solution first, then run the focused Application and Integration filters used by Tasks 1–4. If the isolated SQL connection is configured, run the full Release backend suite once; otherwise record the exact environmental blocker and do not repeat the unchanged command.

- [ ] **Step 3: Run frontend verification serially.**

  From `frontend`, run the changed component tests, the full frontend test suite once at the integrated checkpoint, `npm run typecheck`, focused Prettier checks for changed files, and `npm run build:iis`. Do not run two commands that invoke `sync:resources` concurrently.

- [ ] **Step 4: Run the rendered browser flow.**

  Use the available Browser/IAB path first; if unavailable, use the repository’s Playwright workflow and record the exact fallback reason. Test the flow: authenticated HR dashboard -> choose one department -> inspect cards/history -> open `مدیریت شرح وظایف` -> open a pending review -> view/download/approve or reject with a reason -> open approved list -> compare -> open public skills -> cancel merge -> confirm merge. Check desktop and phone-sized layouts, page identity, nonblank content, no framework overlay, console health, screenshots, and real state changes.

- [ ] **Step 5: Review rendered UI and fix material mismatches before claiming completion.**

  Inspect the first viewport, table density, dialog width, RTL alignment, Persian digits, fixed shell boundaries, inner scroll regions, focus states, and mobile overflow. Keep a short mismatch ledger in the final response; do not treat a passing build as visual verification.

- [ ] **Step 6: Update canonical documents with durable state and any failures.**

  Record the delivered HR scope, tests, current branch/release state, and next step in `current-state.md`. Align confirmed requirements and architecture boundaries. If a tooling, SQL, browser, or deployment failure occurred, record symptom, root cause, remedy, verification evidence, and prevention rule in `standards.md` or the task state as appropriate. Do not record secrets or personal data.

- [ ] **Step 7: Run final verification after documentation changes.**

  Run `git diff --check`, the focused documentation/status inspection, and the smallest relevant test/build command affected by any code or configuration change. Only then report the actual verified status. Do not commit, merge, push, or publish until the user explicitly says `نهایی کن`.

