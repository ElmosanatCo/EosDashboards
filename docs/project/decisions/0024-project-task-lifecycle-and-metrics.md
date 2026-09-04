# 0024 — Project task lifecycle and metrics

**Status:** Accepted
**Date:** 2026-09-04

## Context

Some department tasks represent work on a named software or other project.
Multiple personnel may perform the same project task, and completed work must
remain available for history without appearing as current work.

## Decision

Each department-scoped catalog task may be marked as a project. A project task
assigned to several personnel represents one project with multiple active
contributors. The dashboard counts active projects and active people per project
from database records.

Each personnel-task record has an optional start date and optional end date. A
missing end date means the task remains active. An end date in the past makes
the task inactive. Inactive tasks remain in the database and version history,
but are omitted from the current generated Excel artifact.

## Rationale

This separates current operational workload from historical work and supports
project-level workforce reporting without duplicating project counts for each
person.

## Consequences

- Project identity and the project flag belong to the department-scoped task
  catalog and its database relationships.
- Activity checks use the approved local application-server date model.
- Dashboard project counts come from active database assignments, not generated
  Excel files.
- Version comparison and historical reports continue to show ended tasks.
