# Plan: Department manager job-description workspace

## Goal

Add a role-authorized department-manager workspace for personnel job
descriptions, skill assignment, flexible Excel intake, review submission to
Human Resources, and truthful personnel/skill/task summaries.

## Scope and constraints

- Extend the existing role-aware Home tab with separately selectable content for
  every role assigned to the current user.
- Add a department-manager job-description area with personnel listing, status,
  view, edit, download, manual entry, and one-or-more Excel upload.
- Normalize accepted Excel data into the approved standard workbook format,
  persist the normalized structured representation in the database first, then
  generate the standard workbook from that stored version and save the workbook
  artifact in the database for display/download; never silently replace an
  active record during import or edit.
- Missing optional values are allowed and remain empty. An explicitly supplied
  department outside the manager's authorized department or permitted child
  departments fails only that file with a clear result message.
- Ignore empty/explanatory rows. Preserve useful extra columns by appending
  `column title: value` to the corresponding task description. Preserve task
  order and repair numbering in the generated standard file.
- Every import or manual change first creates an editable department-manager
  draft. It is sent to Human Resources only after explicit manager review and
  approval.
- Human Resources may approve and publish the active version or reject it with
  a reason. Rejected records remain revisable and resubmittable with their
  review history.
- The skill catalog has public skills visible to all managers and department
  skills visible only within their authorized scope. Skill selection in forms is
  a click-to-toggle add/remove interaction.
- Proposed: task titles follow the same uniqueness and catalog approach as
  skills. Only the task description remains free text. Typed skill and task
  values may become reviewable suggestions for normalization instead of being
  silently added as new catalog values.
- Proposed: analyze personnel job descriptions against the skill and task
  catalogs and report omissions, inconsistencies, and deviations. Analysis is
  advisory; it must not silently edit a description, catalog, approval status,
  or published record.
- Raw sample workbooks containing personal names are not copied into tracked
  resources. Only sanitized reference material may be added later.
- Do not invent dashboard metrics, data sources, or future operations. Metrics
  must be derived from approved active records and a separately approved data
  contract.
- The standard Excel artifact does not include per-task required-skill fields.
  Those relationships are stored in the database and maintained by the manager.
- Catalog tasks may be marked as projects. Personnel-task records have optional
  start and end dates; missing end means active, past end means inactive. Past-
  ended tasks remain in database history but are omitted from the current Excel
  artifact.
- Use the existing API -> Application -> Domain layering, with Infrastructure
  as the only database/file/external-system access layer.
- Use focused tests during implementation and broader verification only at the
  integrated checkpoint.

## Required decision gates before code

1. The content contract is accepted: name, department, education, field of
   study, minimum experience, selected person skills, task title, optional task
   start date, and free-text task description. Personnel code is optional
   database data and is not in Excel. Excel sheet/column names are interpreted
   flexibly; only final visual/layout details remain.
2. Status labels and the two-axis model are accepted: workflow statuses are
   `منتظر تأیید`, `در حال بررسی`, `تأیید شده`, `رد شده`, and `آرشیو شده`; the
   independent data-quality status is `سالم` or `ناقص`.
3. The manager's full authority over their own and child departments is
   accepted. Create/edit forms select the target department from that scope,
   the dashboard supports all-managed-departments and single-department views,
   and child departments have no separate job-description approval inbox.
4. The first dashboard metrics are accepted as database-backed personnel,
   active/archived personnel, healthy/incomplete descriptions, workflow status,
  skill/task, skill coverage/gap, department-breakdown, active-project and
  project-person-count, and manager-action metrics. Future metrics must use the
  same approved contract.
5. Approve sanitized workbook references if the samples are to be committed to
   `resources/templates/job-descriptions/references/`.
6. The task catalog is department-scoped, not global. The Department Manager
   manages each task catalog within their own and child-department scope and
   accepts or rejects suggestions for that department. Duplicate matching is
   performed within the target department catalog.
7. The first quality-analysis presentation is accepted: findings include a
   direct action key/link at the affected field or task location and remain
   evidence-linked to the source data and catalog entries. Exact severity labels
   remain a UI detail unless a blocking rule is introduced.
8. The initial task/skill analysis decision is accepted: use an explicit
   catalog relationship and do not claim semantic or AI understanding where no
   such mapping exists. Remaining task-catalog ownership and scope decisions
   are covered by gate 6.
9. The database-canonical generation flow is accepted: persist normalized data
   first, generate Excel from that persisted version, and store the generated
   artifact in the database. No separate source-conflict policy is required.

## Implementation tasks

### 1. Write and approve the feature design

Files:

- Create: `docs/superpowers/specs/2026-09-04-department-manager-job-descriptions-design.md`
- Modify: `docs/project/requirements.md`
- Modify: `docs/project/architecture.md` only for accepted data-flow and file-
  processing boundaries
- Modify: `docs/project/current-state.md` only after the design is accepted

Document the permission matrix, department scope, standard workbook contract,
draft/review lifecycle, skill visibility, upload-result contract, first metrics,
and no-data behavior. Resolve all five decision gates before implementation.

### 2. Add the domain and persistence model

Files:

- Create focused Domain entities/enums for job-description versions, tasks,
  skills, task-required-skill relationships, skill scope, review status, and
  review history under
  `backend/src/EosDashboards.Domain/`
- Create Application ports and use-case contracts under
  `backend/src/EosDashboards.Application/`
- Create EF configurations, repositories, migration, and standard-file storage
  boundary under `backend/src/EosDashboards.Infrastructure/`
- Extend the existing architecture and database tests only where the new
  boundaries require it

Enforce authorization scope and lifecycle transitions in Application/Domain,
not in UI code. Keep active publication separate from manager drafts.

### 3. Implement skill catalog and job-description use cases

Files:

- Create focused use cases under
  `backend/src/EosDashboards.Application/JobDescriptions/` and
  `backend/src/EosDashboards.Application/Skills/`
- Create persistence adapters under
  `backend/src/EosDashboards.Infrastructure/Persistence/`
- Add contracts and endpoints under
  `backend/src/EosDashboards.Api/JobDescriptions/` and
  `backend/src/EosDashboards.Api/Skills/`

Support list/view/create/revise/approve/reject/publish/archive operations,
per-file upload results, rejection reasons, manager-scope checks, and public or
department-specific skill selection. If the proposed task catalog is approved,
apply the same uniqueness and scope rules to task titles while keeping task
descriptions free text. Store the manager-maintained required-skill relation in
the database; do not add it to the standard Excel format. Keep file parsing behind an Application port and
implement the Excel adapter in Infrastructure.

### 4. Implement flexible Excel normalization

Files:

- Create an Excel import adapter and parser tests under
  `backend/src/EosDashboards.Infrastructure/JobDescriptions/` and
  `backend/tests/EosDashboards.IntegrationTests/JobDescriptions/`
- Add Application-level normalization tests under
  `backend/tests/EosDashboards.Application.Tests/JobDescriptions/`
- Store only generated standard drafts in the approved draft storage boundary
- Persist the normalized personnel, task, skill, status, and review data in the
  database first, generate the standard Excel artifact from that data, and
  store the artifact in the database linked to the same version.
- Generate the current Excel task list from active personnel-task records only;
  retain ended records in database history and comparison data.
- Build all dashboard statistics and search/analysis queries directly from the
  structured database; never parse generated Excel artifacts for dashboard
  counts.
- Test that downloaded Excel files are generated from the corresponding stored
  database version, while confirming that the dashboard does not depend on
  re-reading the Excel artifact.

Test optional omissions, explicit out-of-scope departments, extra rows,
extra columns, empty/explanatory rows, task ordering, multiple files, and
per-file success/failure messages. If approved, test normalization of typed
skills/task titles into reviewable suggestions while preserving free-text task
descriptions. Do not add a second parser for each sample; use one normalized
contract.

### 5. Build the manager job-description workspace

Files:

- Create `frontend/src/features/jobDescriptions/`
- Create `frontend/src/features/skills/`
- Create `frontend/src/pages/DepartmentJobDescriptionsPage.tsx`
- Extend `frontend/src/navigation/routeRegistry.tsx` and
  `frontend/src/navigation/workspaceTargets.tsx`
- Add focused component tests beside the new feature files

Render personnel, description status, view/edit/download actions, manual form,
skill toggle controls, upload results, draft review, and explicit approve/send
actions. Keep the existing RTL workforce visual system and prevent unauthorized
department/skill exposure.

### 6. Add Human Resources review and publication UI

Files:

- Create `frontend/src/pages/HumanResourcesJobDescriptionReviewPage.tsx`
- Extend the existing Human Resources route registration and API client
- Add focused tests for approval, rejection with reason, revision, and
  publication state

The UI must make the review state and reason visible and must never publish a
manager draft before Human Resources approval.

### 7. Extend role-aware Home and dashboard summaries

Files:

- Modify `frontend/src/pages/home/homeContent.ts`
- Modify `frontend/src/pages/HomePage.tsx`
- Modify `frontend/src/pages/HomePage.test.tsx`
- Modify `frontend/src/pages/DepartmentDashboardPage.tsx`
- Extend the dashboard API contract and focused browser coverage

Show a role selector when the user has multiple roles. The selected role gets
its own guide, authorized actions, pending work, and dashboard entry points.
Department summaries must use approved active data only and must not display
invented counts or future operations.

### 8. Add job-description quality analysis

Files:

- Create an Application analysis contract and use case under
  `backend/src/EosDashboards.Application/JobDescriptions/`
- Create read-only analysis/query adapters under
  `backend/src/EosDashboards.Infrastructure/JobDescriptions/`
- Add API contracts/endpoints under
  `backend/src/EosDashboards.Api/JobDescriptions/`
- Add manager and Human Resources presentation components under
  `frontend/src/features/jobDescriptions/`
- Add focused Application, integration, component, and mocked browser tests

Compare each approved or reviewable description with the authorized skill and
task catalogs. Report missing catalog selections, catalog deviations, repeated
or conflicting entries, incomplete required content, and other approved
quality rules with the affected person, task/skill, and source evidence. Keep
findings separate from the approval workflow: a manager or Human Resources
reviewer decides whether to correct, accept, or otherwise handle each finding.

For the task/skill comparison, use an explicit catalog relationship: each
catalog task may declare its required skills, and the analyzer compares the
union of required skills for a person's selected tasks with that person's
selected skills. Report missing required skills and selected skills with no
supporting catalog task as review findings, not automatic errors. A free-text
task or description without a catalog mapping is reported as “needs review”;
the system must not infer a definitive semantic mismatch from keywords alone.
No external AI service is required or assumed for this initial implementation.

Every finding must expose an action key or link that takes the manager to the
affected profile field, selected skill, task title, task date, or description
location. Applying a correction remains an explicit user action.

### 9. Verify and update durable memory

Files:

- Modify `docs/project/current-state.md`
- Modify affected canonical requirements, architecture, standards, and decision
  documents

Run only the focused backend/frontend tests while implementing. At the
integrated checkpoint run the relevant build, static analysis, formatting,
component/integration tests, and mocked critical browser flows. Review the
rendered RTL manager and Human Resources pages at desktop and phone widths.
Record any environmental failure with symptom, root cause, durable remedy,
verification evidence, and prevention rule.

## Completion criteria

- The manager can create, import, inspect, edit, download, review, and submit
  job-description drafts within authorized department scope.
- Excel intake is flexible for optional omissions and useful extra content,
  rejects explicitly out-of-scope departments clearly, and reports each file's
  result independently.
- Skills can be toggled on/off in manual and edit forms using the authorized
  public and department-specific catalog.
- Human Resources can approve or reject with a reason, and only approved
  records become active.
- Home content is separately selectable by role for multi-role users.
- Dashboard summaries use approved active data and remain extensible without
  inventing metrics.
- Focused tests and integrated verification pass, documentation is current,
  and no raw personal sample workbook is committed.
