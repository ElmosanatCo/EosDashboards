# Human Resources dashboard and job-description management design

**Status:** Approved for implementation
**Date:** 2026-09-05

## Scope

Extend the existing Human Resources role surface with a database-backed
workforce dashboard and a unified `مدیریت شرح وظایف` workspace. The dashboard
supports `همه بخش‌ها` and one selected department. The management workspace
contains the Human Resources review queue, approved descriptions, and public
skill administration.

The implementation extends the existing job-description module and version
model. It does not create a parallel dashboard data source, a separate review
subsystem, or a browser-only authorization path.

## Confirmed user experience

### Human Resources dashboard

The page provides an explicit department selector with:

- `همه بخش‌ها` as the first value and an explicit all-departments value;
- every company department available to the authorized Human Resources role;
- one-department filtering for all cards and change-history data.

The initial cards reuse approved structured job-description metrics:

- personnel total, active personnel, and archived personnel;
- healthy and incomplete descriptions;
- descriptions awaiting Human Resources review;
- approved and rejected descriptions; and
- active projects and active people on active projects where those metrics are
  available in the existing data contract.

The dashboard also contains:

- a page-bounded, server-paged `تاریخچه تغییرات` list showing the changed
  department, person, version/change type, actor where available, and Persian
  local date/time; and
- `آمار تغییرات بخش‌ها`, showing the number of retained description-version
  changes per department and the latest change time for the current selector.

The first release uses all retained database versions and does not invent a
time-window filter. Generated Excel artifacts are never parsed for these
metrics.

### Job-description management

The existing user-facing target title `بازبینی شرح وظایف` becomes
`مدیریت شرح وظایف`. Existing logical navigation can remain stable where that
avoids breaking saved tabs or links, but all visible labels, help content, and
command-search metadata use the new title.

The page has three internal tabs:

1. `منتظر تأیید`: the Human Resources review queue;
2. `تأیید شده`: the latest approved active version for each description; and
3. `مهارت‌های عمومی`: public skill administration.

The first two tabs share the explicit department selector. The queue and
approved list are server-filtered and show the person, department, quality and
workflow status, update date/time, and available actions.

Selecting a row opens a responsive review dialog. The dialog presents the
structured profile, selected skills, tasks, quality findings, workflow state,
and rejection reason when present. It provides:

- view/detail without mutation;
- download of the matching database-canonical Excel artifact;
- approve for records in Human Resources review; and
- reject with a required Persian reason in the same review form.

Approval and rejection refresh the affected list, dashboard summary, and
history queries. Canceling the rejection form preserves entered text locally
and sends no mutation. A rejected request preserves the form values and shows
an actionable Persian error.

Each list also exposes `مقایسه` when a previous version exists. The default
comparison is the current version against the immediately previous retained
version of the same job-description record. The comparison dialog groups
profile fields, personnel code, selected skills, task titles, task dates,
weekly workload, free-text descriptions, quality, and workflow changes into
added, removed, changed, or unchanged values. If there is no previous version,
the action is disabled with an explanatory state. Free-text and date values
remain readable in RTL with Persian digits and separate date/time display.

### Public skill administration

The `مهارت‌های عمومی` tab lists active skills by default and can show inactive
or all skills. Human Resources can:

- rename an active public skill;
- deactivate it through an explicit Persian confirmation;
- reactivate an inactive skill through the same confirmation rule; and
- merge two active public skills.

The merge dialog has two public-skill selectors and clearly identifies the
surviving skill by name. Its confirmation text names both the source skill and
the skill whose title remains. The operation:

- transfers every source reference in retained job-description versions and
  task-required-skill relationships to the surviving skill;
- collapses duplicate links within each version or task;
- deactivates the source skill instead of physically deleting it; and
- records an immutable audit event containing the actor, source and surviving
  skill identifiers/names, time, result, and correlation identifier.

No department selector or department name appears in this public-skill merge
form. The operation is one database transaction and rejects the same-skill,
inactive-skill, missing-skill, and conflicting-concurrency cases without
partial migration.

## Authorization and data boundaries

Only an active user with the `HumanResourcesManager` role may call the Human
Resources dashboard, Human Resources description lists, comparison endpoint,
Human Resources review mutations, public-skill administration, and merge
operation. The UI may hide unavailable targets, but the API and Application
layer enforce every scope and role check.

The Human Resources dashboard and description lists use the current structured
database records. The latest-version query must prevent retained historical
versions from appearing as duplicate current records in the approved list.
History and comparison queries may read retained versions but do not mutate
them. Existing manager and department-manager behavior remains unchanged.

## Proposed contracts

The API remains versioned under `/api/v1` and keeps the existing
`/api/v1/job-descriptions` boundary where practical:

- a Human Resources dashboard query accepts an optional department ID and
  returns cards, per-department change summaries, and a server-paged recent
  change list;
- Human Resources review and approved list queries accept an explicit optional
  department ID, with `all` represented by omission or the established
  explicit all-value at the UI boundary;
- a comparison query accepts a current version ID and returns the current and
  previous structured snapshots plus field-level/task-level differences;
- the existing Excel download remains the download operation for a reviewed
  version; and
- a public-skill merge command accepts source and surviving public-skill IDs
  and returns the surviving item plus the deactivated source status.

Responses use API DTOs distinct from EF entities, server-side pagination for
history, stable Persian status mapping, safe problem details, and the existing
correlation/audit conventions.

## Domain and persistence changes

The existing immutable job-description versions are the source for history and
comparison. No duplicate history table is introduced. The merge operation adds
the minimum domain behavior and Infrastructure transaction support needed to
reassign `JobDescriptionVersionSkill` and `TaskCatalogRequiredSkill` links,
deduplicate junction rows, and deactivate the source public skill.

The existing audit model is extended only as needed to represent a public-skill
merge event and its non-sensitive source/target metadata. No credentials,
tokens, private mobile values, or raw request payloads are stored.

All persisted timestamps remain application-server local `datetime2(3)` values
without `Utc` names. All principal keys remain SQL Server `bigint` `Id` keys.

## UI and responsive behavior

The pages retain the approved dark-default Persian RTL workforce-operations
visual system: fixed shell, full-width workspace frame, flat accent-line
panels, compact tables, gold hover line, and bounded inner scrolling for long
lists. The review, comparison, merge, and rejection surfaces are responsive
in-page dialogs. Tables remain structured tables on desktop; narrow screens
may use bounded horizontal table scrolling with visible labels and no page
overflow.

Every route receives truthful help content under the fixed sections
`وظایف این صفحه`، `امکانات`، `شیوه انجام کار`، and `محدودیت‌ها`. All visible
dates, times, counts, IDs, and other numbers use Persian digits; API IDs and
submitted values retain their contract representation.

## Error and concurrency behavior

- Duplicate rename or merge targets return stable conflict problems and leave
  entered form values intact.
- Approval and rejection validate the current workflow state on the server.
- Missing or incomplete descriptions cannot be approved through the existing
  quality gate.
- A concurrent version or catalog change returns a visible conflict state and
  does not partially update the UI or database.
- Cancel, close, and backdrop dismissal for rejection, destructive skill
  actions, and merge leave state unchanged and send no mutation request.

## Verification

Focused verification must cover:

- Application/domain merge semantics, duplicate-link collapse, authorization,
  and transaction/conflict behavior;
- dashboard department filtering, metrics, latest-version grouping, and
  change-history aggregation;
- Human Resources review/approved list authorization, approval, rejection,
  download, and comparison DTOs;
- API integration authorization and protected mutation paths;
- React interaction tests for tabs, selectors, review dialog, download,
  rejection reason, comparison, skill rename/deactivate/reactivate, merge
  confirmation, and canceled mutation requests; and
- a mocked authenticated desktop/mobile browser flow covering the dashboard,
  review, compare, and merge confirmation path without runtime console errors.

At the integrated checkpoint, run the relevant backend build/tests, frontend
typecheck/formatting/build, focused component tests, and the repository's
existing browser flow. Review the rendered desktop and phone layouts before
claiming completion.

## Out of scope

- arbitrary selection of two non-adjacent historical versions;
- time-window analytics or predictive/AI workforce insights;
- public-skill department ownership or department-specific fields;
- physical deletion of skills or retained job-description versions;
- new roles, granular permissions, exports, alerts, or production deployment;
- changing the approved Excel field contract; and
- inventing dashboard data sources beyond the structured job-description
  database.
