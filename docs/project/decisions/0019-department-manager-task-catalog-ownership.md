# 0019 — Department manager task-catalog ownership

**Status:** Accepted
**Date:** 2026-09-04

## Context

Task titles must be unique and consistent so that job-description statistics
and quality analysis are meaningful. The manager responsible for a department
needs authority to define the task vocabulary used by that department.

## Decision

Each department has its own task catalog. Task titles are not organization-wide
values, and a task from one department is not automatically available in
another department.

The Department Manager owns each task catalog within their own department and
child-department scope. The manager may:

- define catalog task titles;
- review typed task-title suggestions; and
- accept or reject those suggestions.

Accepted task titles remain catalog values. Task descriptions remain free text.
Task-to-required-skill relationships are maintained in the database within the
approved scope.

## Rationale

The responsible manager is closest to the department's actual work and can
maintain a vocabulary that supports consistent entry and meaningful reports.

## Consequences

- Task-catalog create, suggestion-review, and task-to-skill operations require
  server-side manager-scope authorization.
- Duplicate matching is performed within the target department catalog.
- A shared task vocabulary across departments is not part of the initial
  design.
- A manager's task catalog must not expose or change another manager's scope.
