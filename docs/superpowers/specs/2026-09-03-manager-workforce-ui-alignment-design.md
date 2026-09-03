# Manager-facing Workforce-operations UI Alignment Design

**Date:** 2026-09-03

**Status:** Approved

## Goal

Align the implemented React application shell and its configurable appearance with the approved manager-facing workforce-operations UI standard, without inventing dashboard data, workflows, roles, or navigation destinations.

## Scope

- Replace the former amber-default, six-palette theme system with a dark-default teal system offering teal, indigo, emerald, amber, and rose interaction accents.
- Apply the approved dark and light surface, text, border, radius, spacing, and motion tokens through the shared Material UI theme.
- Make the application shell's top header, workspace-tab strip, and status bar visually fixed and operationally compact; preserve the existing persistent, collapsible hamburger side menu and its mobile overlay behavior.
- Restyle the existing home no-data state and shared panels/tabs so they establish the approved flat, bordered, accent-line visual language without claiming absent business data.
- Keep the existing, real version and Tehran/Persian-calendar status information. Do not add simulated synchronization, organization, health, search, notification, AI, dashboard, or role content until their underlying behavior is approved.

## Compatibility

User preferences and API contracts change together so a newly selected approved palette can be persisted. Former locally stored or server-returned palette values map safely to teal rather than breaking theme rendering; the implementation does not rewrite server data merely on read.

## Design system

- Default: dark mode with palette `teal`; dark page `#0D1113`, surfaces `#13191C` and `#182024`, divider `#2A3538`, primary text `#EDF2F0`, muted text `#96A4A6`, accent `#38B8AA`.
- Light: page `#F2F5F3`, surfaces `#FBFCFA` and `#F3F6F4`, raised `#E8EFEB`, divider `#D8E0DC`, strong border `#C3CFCA`, primary text `#17201F`, muted text `#5C6B69`.
- Panels: flat, 1px border, small radius, compact spacing, and a thin top accent. Hover and selected/active states change only that accent to gold/amber.
- Tabs: compact and quiet when inactive; the selected tab has an amber/gold underline. The tab strip remains horizontally scrollable on narrow screens.
- Motion: only 150–200 ms transitions, with the existing reduced-motion override retained.

## Verification

Focused unit/component tests first demonstrate the new default, palette migration, token use, and panel/tab state behavior. Frontend lint, typecheck, formatting, build, targeted tests, and the existing mocked browser flow protect regressions. Rendered desktop and phone checks use the in-app browser when available, covering shell visibility, tab interaction, palette selection, and absence of clipping or whole-page scroll.

## Out of scope

Dashboard data, charts, role-based routes, menu destinations, business metrics, AI insights, global search, notifications, synchronization/health data, and any backend data source are not implemented by this alignment.
