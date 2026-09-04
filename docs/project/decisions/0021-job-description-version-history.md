# 0021 — Job-description version history and comparison

**Status:** Accepted
**Date:** 2026-09-04

## Context

Job descriptions change when personnel, skills, tasks, or responsibilities
change. The organization needs to understand what changed and report that
history without losing earlier approved versions.

## Decision

Every created or revised job description produces a retained version. Versions
must be comparable and reportable across profile fields, required personnel
code, selected skills, task titles, optional task start dates, and free-text
task descriptions.

The task start date is optional; its absence contributes to the `ناقص` quality
status. Personnel code is not part of the Excel format. Imported workbooks may
retain an empty code as `ناقص` for manager correction, but manager-created or
revised records require a code before the form can be saved.

## Rationale

Versioned comparison preserves organizational history and makes later quality,
approval, and workforce reporting explainable.

## Consequences

- Revisions must not overwrite the only copy of an earlier version.
- The API and UI need a version-history view and field/task-level comparison.
- Generated Excel artifacts remain linked to their corresponding database
  version.
