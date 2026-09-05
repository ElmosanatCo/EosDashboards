# 0031 — Revalidate job-description quality after catalog changes

**Status:** Accepted
**Date:** 2026-09-05

## Context

Job-description quality depends not only on completed profile and task fields,
but also on the required-skill relationships maintained in the task catalog.
Changing a task's required skills can invalidate a description that is already
waiting in the Human Resources inbox.

## Decision

After a task's required-skill assignment changes, revalidate every active
job-description version that uses that task. Approved and archived historical
versions are not rewritten. Affected versions with findings persist an
incomplete quality state and move from Human Resources review, department
approval, or rejection back to `منتظر رفع نقص`; they must be corrected and
explicitly resubmitted by the Department Manager.

Catalog findings are part of the approval gate. Department submission and
Human Resources approval both require a healthy result. Quality findings shown
to users use catalog names where available and keep action-linked locations;
the analyzer must not expose opaque catalog identifiers as the primary
explanation.

## Rationale

This keeps the Human Resources inbox limited to manager-confirmed descriptions
whose catalog evidence is current, while preserving approved historical
versions and avoiding automatic edits to personnel selections.

## Consequences

- Quality state includes structural completeness and catalog consistency.
- Required-skill updates are transactional with active-version revalidation.
- Affected Human Resources items leave that inbox until corrected and
  resubmitted.
- Tests must cover the transition from Human Resources review to data
  completion and the submission gate.

## Supersedes

This decision supersedes the statement in decision 0015 that quality analysis
never changes workflow or approval state. It does not authorize automatic
editing of a job description's selected skills or task values.
