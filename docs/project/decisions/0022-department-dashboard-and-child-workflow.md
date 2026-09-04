# 0022 — Department dashboard metrics and child workflow

**Status:** Accepted
**Date:** 2026-09-04

## Context

Department Managers need one operational view across their responsibility and
the ability to focus on a single department. Child departments do not operate
independent job-description approval inboxes.

## Decision

The first Department Manager dashboard reads directly from structured database
data and includes:

- personnel counts;
- active and archived personnel counts;
- healthy and incomplete description counts;
- workflow-status counts;
- skill and task counts;
- skill coverage and identified gaps;
- department breakdowns; and
- manager actions such as approving descriptions and following up incomplete
  records.

The parent Department Manager owns the job-description workflow for their own
and child departments. Child departments do not have separate job-description
approval inboxes. The dashboard provides an all-managed-departments view and a
single-department view.

## Rationale

These metrics connect the dashboard to the manager's confirmed operational
responsibility and keep approval ownership unambiguous.

## Consequences

- All metrics use the database as their source and never parse generated Excel
  artifacts.
- Dashboard filters and API queries must enforce the manager's full scope.
- Action cards must open the relevant approval or incomplete-data location.
