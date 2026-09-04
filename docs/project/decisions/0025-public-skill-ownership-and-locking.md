# 0025 — Public skill ownership and cross-department locking

**Status:** Accepted  
**Date:** 2026-09-04

## Context

Public skills must be reusable across departments without allowing one
department's manager to rename or remove a value that another department has
already adopted.

## Decision

The manager registers a public skill against an owner department, while the
skill remains organization-wide and has no target department of its own. The
registering manager may edit or deactivate it while every recorded usage is
still within the owner department. Once a recorded job-description version in
another department references it, only Human Resources may edit or deactivate
the public skill.

The catalog response reports the owner department, the number of departments
using the skill, and manager-allowed actions. The API enforces the same rule;
the UI only reflects that decision and is not the security boundary.

## Rationale

This preserves the registering manager's ability to correct an unused or
locally used value while protecting a shared vocabulary after cross-department
adoption.

## Consequences

- Public skill registration requires a department within the manager's scope.
- Existing public skills without an owner remain Human Resources-managed.
- Usage is derived from persisted job-description version references, so
  pending and historical references are visible to the locking rule.
- Human Resources retains unrestricted public-skill rename and deactivation
  authority.
