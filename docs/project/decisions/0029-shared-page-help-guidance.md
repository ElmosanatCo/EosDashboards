# 0029 — Shared page-help guidance

**Status:** Accepted
**Date:** 2026-09-04

## Context

Managers and administrators need a consistent, low-friction explanation of
each workspace page without leaving the current task. The guidance must remain
truthful as the product grows and must not be confused with authorization or
live business data.

## Decision

Every authorized workspace page, including authorized form pages, exposes a
shared help icon in the upper-left corner. The icon opens a Persian modal with
the fixed sections `وظایف این صفحه`, `امکانات`, `شیوه انجام کار`, and
`محدودیت‌ها`. Each route has centralized page-specific content, with a truthful
fallback for pages whose capability is not yet finalized.

## Rationale

A shared frame keeps placement, accessibility, visual hierarchy, and modal
behavior consistent while allowing each page to explain its own workflow.
Centralized content can be updated alongside each approved implementation
stage without introducing a new data source or inventing unavailable features.

## Consequences

- New workspace routes must add or review their guide content before release.
- Guide text is UI documentation, not a replacement for server authorization,
  validation, or audit records.
- The frame positions the icon in the existing empty left edge opposite the
  page's title row without adding horizontal or vertical page padding, and
  preserves the existing page and inner-panel scrolling boundaries without
  increasing page height. Guide bullets use RTL-safe logical positioning so
  they remain inside their cards.
