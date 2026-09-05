# Job-description skill review warning

## Status

Approved design for implementation on 2026-09-05.

## Purpose

A person may perform a catalog task before having every skill required by that task. This is not a data-quality defect and must not block department approval or Human Resources review. It is a management warning that requires follow-up by the department manager and the Chief Executive Officer.

## Behavior

- A selected skill that is unrelated to the person's tasks is valid and produces no finding.
- A required task skill missing from the person's selected skills is a non-blocking review warning.
- Structural, unresolved, uncatalogued, and missing task data remain blocking quality issues.
- A version can therefore be both `سالم` and `نیازمند بررسی`.
- The warning is recalculated when an active version is created, revised, approved, or affected by a task-catalog required-skill change.
- The warning is cleared when all required task skills are present or the relevant task requirement is removed.
- Existing approved and archived history is preserved. Active non-approved versions are backfilled by a data-only migration; records with real blocking defects remain incomplete.

## Data and domain model

Add a persisted boolean on `JobDescriptionVersion` named `NeedsReview` (mapped to `NeedsReview`). It is independent of `QualityStatus` and workflow status. The quality analyzer continues to return detailed findings for the analysis view, but missing-required-skill findings are classified as non-blocking review warnings and unrelated selected skills are not findings at all.

The domain exposes a single recalculation operation that receives the blocking-quality result and the review-warning result. Blocking findings continue to move eligible workflow states to data completion. Review-warning changes update `NeedsReview` without changing the workflow state.

## Application and API

- Extend job-description list/detail responses with `needsReview`.
- Keep the existing analysis endpoint and return missing-required-skill details so managers can see the task and missing skill.
- Extend department dashboard metrics with the count of active versions needing review.
- Add a read-only warning query scoped to the authenticated department manager or Chief Executive Officer. It returns the active version, person, department, task, and missing skill details; it does not mutate workflow.
- The department manager's job-description worklist and dashboard show the warning independently from the healthy/incomplete badge.
- The Chief Executive dashboard replaces its current placeholder with a focused warning panel for this approved slice; unrelated executive metrics remain out of scope.
- Human Resources approval continues to require `QualityStatus == Healthy`, but does not reject a healthy version solely because `NeedsReview` is true.

## Persistence and migration

Create a schema migration for `NeedsReview` with a safe default of `false`. Backfill active versions by evaluating required task skills. Versions with only missing required skills become healthy and `NeedsReview = true`; versions with blocking defects retain their incomplete state and receive the warning flag only when a missing required skill is also present. Approved and archived versions are not rewritten. The migration also corrects the previously applied catalog-quality backfill so unrelated selected skills no longer keep an otherwise complete active version in data completion.

## UI

Use a distinct warning chip and a concise Persian explanation. The warning must not use the red/incomplete treatment. Selecting the warning opens the existing detail/analysis surface so the manager can see which task lacks which skill. The CEO panel is read-only.

## Verification

- Analyzer tests prove unrelated selected skills are ignored, missing required skills are non-blocking warnings, and real data defects remain blocking.
- Domain/application tests prove healthy-plus-warning versions can be approved and sent onward.
- API and repository tests cover scoped warning reads and migration backfill behavior.
- Frontend tests cover the independent warning display for the department list, dashboard, and CEO panel.
- Run the focused backend/frontend suites, typecheck, production build, lint, and the documented IIS smoke checks.
