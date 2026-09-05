# 0029 — Shared page-help guidance

**Status:** Accepted
**Date:** 2026-09-05

## Context

Managers and administrators need a consistent, low-friction explanation of
each workspace page without leaving the current task. The guidance must remain
truthful as the product grows and must not be confused with authorization or
live business data.

## Decision

The fixed application header exposes a shared help icon beside the user menu.
Selecting it opens a Persian modal for the active workspace tab with the fixed
sections `وظایف این صفحه`, `امکانات`, `شیوه انجام کار`, and `محدودیت‌ها`.
Each route has centralized page-specific content, with a truthful fallback for
pages whose capability is not yet finalized.

## Rationale

A shared header action keeps placement, accessibility, visual hierarchy, and
modal behavior consistent while allowing each active page to explain its own workflow.
Centralized content can be updated alongside each approved implementation
stage without introducing a new data source or inventing unavailable features.

## Consequences

- New workspace routes must add or review their guide content before release.
- Guide text is UI documentation, not a replacement for server authorization,
  validation, or audit records.
- The header action follows the active tab, so it never depends on page-level
  layout or form action placement. The guide preserves the existing page and
  inner-panel scrolling boundaries. Guide bullets use RTL-safe logical
  positioning so they remain inside their cards.
