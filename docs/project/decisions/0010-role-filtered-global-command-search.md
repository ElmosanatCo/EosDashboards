# 0010 — Role-filtered global command search

**Status:** Accepted

**Date:** 2026-09-03

## Context

Manager-facing workspaces need a compact way to discover authorized pages and
operations as the menu grows. A user-provided dashboard screenshot establishes
the intended compact-header and operational-dashboard composition without
approving its depicted content.

## Decision

The fixed header includes a compact global command search. It returns only
targets that the current user is authorized to access: workspace pages,
permitted operations, and later eligible dashboard elements. Selecting a
result opens or activates its internal tab. Unauthorized targets are omitted.

The unchanged screenshot at
`resources/images/references/manager-workforce-dashboard-reference.png` is an
internal visual reference for this search and compatible dashboard composition.

## Rationale

A shared role-filtered catalogue lets menus and search remain consistent as
role-specific operations expand, without disclosing unavailable capabilities.
The reference supports visual continuity while keeping product-data and
workflow discovery separate.

## Consequences

- New navigable features declare their title, route, roles, and searchable
  label in one shared catalogue.
- Search results are a convenience layer only; all future API operations
  retain server-side authorization.
- The reference image must not be shipped as product UI or used to infer
  metrics, data, workflow, or branding requirements.
