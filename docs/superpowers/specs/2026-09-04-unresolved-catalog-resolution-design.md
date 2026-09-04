# Unresolved catalog values in job-description drafts

**Status:** Implemented and verified
**Date:** 2026-09-04

## Problem

An uploaded workbook can contain skill text and task titles that do not match
the current catalogs. The current import path keeps only matched catalog IDs,
so unmatched values disappear from the structured draft and the detail view
looks empty. A manager must be able to see the original text, correct it, and
link it to an existing catalog item or create an authorized new item.

## Goals

- Preserve every readable imported skill and task value in the database.
- Show unresolved values in list/detail/edit surfaces instead of hiding them.
- Prevent an incomplete draft from reaching Human Resources.
- Let the Department Manager resolve each value without guessing or external AI.
- Preserve the original unresolved state in version history.
- Keep the database version canonical; generated Excel remains a derivative.

## Workflow invariant

Add the workflow status `منتظر رفع نقص`.

The status transitions are:

```text
import or revision with incomplete data
  -> منتظر رفع نقص
manager resolves all missing/unlinked data
  -> منتظر تأیید
manager approves
  -> در حال بررسی
Human Resources approves
  -> تأیید شده
```

An incomplete record remains visible to the manager and Human Resources, but
the manager approval endpoint rejects it and the UI does not offer an enabled
approval action. A revision that becomes incomplete returns to `منتظر رفع نقص`.
After the data becomes complete, the record returns to `منتظر تأیید`; resolving
data never approves it automatically. Existing rejection and archive rules
remain unchanged.

`سالم` requires the existing required profile/task fields plus every imported
skill and task being linked to a catalog value. Missing personnel code and
missing task start date continue to make the quality status `ناقص` according to
the approved requirements.

## Persistence model

Keep the current catalog-linked collections and add two version-owned
collections:

- `JobDescriptionVersionUnresolvedSkill`: version ID, raw skill name, and
  source/order metadata needed for stable display and comparison;
- `JobDescriptionVersionUnresolvedTask`: version ID, raw task title, free-text
  description, optional dates, and sort order.

Unresolved values are not catalog entries and are never silently discarded.
When a manager edits a draft, the new immutable version contains either the
selected existing catalog link or the newly created catalog link. The previous
version retains the raw unresolved value, so comparison can show how it was
resolved. If the manager saves without resolving a value, it remains in the new
version and the workflow stays `منتظر رفع نقص`.

The generated workbook includes unresolved skill names and unresolved task
content while a draft is incomplete, so download never presents an apparently
empty description. After resolution, the normal catalog-linked representation
is generated from the database version.

## Import and application flow

The Excel parser continues to return all readable source skills and tasks. The
import application matches normalized names against authorized catalogs but
also passes unmatched names and task rows into the new unresolved collections.
It creates the draft rather than failing merely because a catalog match is
missing. Existing structural failures, invalid explicit departments, missing
person name, and absence of a readable task retain their current behavior.

The application computes quality and workflow together at version creation and
revision. The domain approval operation also checks quality, so the invariant
is enforced independently of the UI. The API returns a specific incomplete
result with the unresolved item summaries when approval is attempted too
early.

## Detail and edit experience

The detail view contains two explicit areas:

- selected catalog skills/tasks;
- `موارد نیازمند تطبیق`, listing the original skill names and task titles with
  their descriptions and dates.

The edit form preserves all raw values and offers, per unresolved item:

- for a skill, choose from all public skills plus the department-specific skills
  of the target department;
- create a new skill and choose public or department-specific scope using the
  existing catalog authorization rules, then link it immediately;
- for a task, choose an existing authorized task from the target department or
  create a new catalog task;
- when creating a new task, set its project flag with a visible `پروژه`
  checkbox; when an existing task is selected, show that task's project state;
- keep the job-description text, dates, and ordering in the version form.

No automatic fuzzy or AI mapping is performed. The manager makes the final
mapping decision. A raw value cannot be removed by simply hiding it; it must be
linked, or it remains unresolved and keeps the approval gate active.

## API changes

- Add the `منتظر رفع نقص` workflow value to domain and API status mapping.
- Extend list/detail responses with the quality-blocking state and unresolved
  skill/task values.
- Extend create/revise input so unresolved values can be retained while a
  manager edits other fields.
- Keep existing catalog create endpoints and scope checks; the edit flow uses
  them before linking a newly created item. The task-create path must accept the
  project flag, and the skill-create path must accept public or target-specific
  scope.
- Return a stable problem code and human-readable summaries when approval is
  blocked by incomplete data.

## UI behavior

- Display `منتظر رفع نقص` as a distinct warning workflow chip.
- Hide or disable manager approval while unresolved values or other incomplete
  fields remain, with a clear reason.
- Show raw imported values in detail and edit forms even when no catalog item
  matches.
- Refresh the draft/detail and catalog queries after a successful mapping or
  catalog creation.

## Verification

Use focused tests only:

- domain tests for quality calculation and the approval invariant;
- integration/parser tests for preserving unmatched source values;
- application/API tests for the blocked approval response and revised linked
  version;
- focused React tests for raw-value visibility, mapping controls, and the
  disabled approval state;
- one internal-browser smoke flow covering import-result visibility or an
  existing incomplete draft, mapping controls, and the `منتظر رفع نقص` chip.

Do not upload the local sample workbooks as part of verification unless the
user explicitly requests database mutation. Read-only parser tests remain the
safe evidence for the source-file formats.

## Out of scope

- automatic AI or fuzzy catalog assignment;
- changing Human Resources permissions or child-department ownership;
- changing the approved Excel field contract beyond retaining unresolved text;
- deleting or rewriting prior versions.
