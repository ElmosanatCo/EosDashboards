# Department manager job-description workspace design

**Status:** Approved for implementation
**Date:** 2026-09-04

## Scope

The Department Manager manages job descriptions for their own department and
all child departments. The parent manager owns the workflow; child departments
do not have separate job-description approval inboxes. The dashboard supports
all managed departments or one selected department.

## Standard job-description content

The structured record contains:

- person name;
- target department;
- optional database personnel code, never required in the Excel input;
- education;
- field of study;
- minimum experience;
- selected public and department-scoped skills;
- one or more department-scoped catalog task titles;
- optional task start date;
- optional task end date; and
- free-text task description.

Task titles and skills are catalog values. Only task descriptions are free
text. Each department has an independent task catalog. Catalog tasks may be
marked as projects and may declare required skills in the database.

Excel sheet names and source column labels are not contractual for imports. The
downloaded standard workbook uses the approved reference layout stored at
`resources/templates/job-description-reference.xlsx`, based on the supplied
personnel workbook sample: its RTL `Sheet1`, merged cells, widths, row
heights, styles, theme, print setup, Persian labels, and task-table geometry
remain intact while persisted values are filled in. The importer recognizes
supported content and common labels, ignores empty/explanatory rows, preserves
useful extra columns in the task description, and repairs task numbering.

## Persistence flow

Manual entry and Excel upload both normalize into a database draft first. The
database version is canonical. The standard Excel workbook is generated from
that persisted version and stored in the database linked to the same version.
Dashboard statistics, search, analysis, and workflow always read structured
database data, never generated Excel artifacts.

## Workflow and quality axes

Workflow status is one of `منتظر تأیید`, `در حال بررسی`, `تأیید شده`, `رد شده`,
or `آرشیو شده`. Quality status is independently `سالم` or `ناقص`. Missing task
start date and missing optional database personnel code contribute to `ناقص`.
The manager reviews and sends drafts; Human Resources approves or rejects with a
reason. A rejected version is revisable and retains its history.
Only an unapproved draft in `منتظر تأیید` or `منتظر رفع نقص` may be deleted by
the Department Manager. Deletion and archival both require explicit user
confirmation. Rejected, under-review, approved, and archived versions remain
retained history; an approved departed-person record is archived rather than
deleted.

## History and analysis

Every change creates a retained comparable version. Ended tasks remain in
database history and comparisons but are omitted from the current generated
Excel artifact. Quality findings are deterministic and use explicit
task-to-required-skill relationships. Each finding includes an action link to
the affected field or task location; no automatic correction or external AI
service is assumed.

## Dashboard baseline

The initial manager dashboard reads the database and shows personnel, active and
archived personnel, healthy/incomplete descriptions, workflow statuses, skills,
tasks, skill coverage/gaps, department breakdowns, active projects, active
people per project, and actionable approval/incomplete-data work.

## Remaining UI detail

Final visual details of the generated workbook and exact wording/severity
styling for quality findings remain presentation details and must not alter the
data or authorization rules above.
