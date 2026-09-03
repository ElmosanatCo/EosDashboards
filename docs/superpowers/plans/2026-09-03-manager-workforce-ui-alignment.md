# Manager Workforce UI Alignment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Align the existing application shell and preference-backed theme system with the approved workforce-operations UI standard.

**Architecture:** A shared Material UI token system owns colors, geometry, and component variants. The frontend and API preference allowlists evolve together, while a frontend compatibility mapper makes legacy stored palette values safe. Existing navigation behavior remains intact.

**Tech Stack:** React 19, TypeScript, Material UI 9, Vitest, Playwright, ASP.NET Core 10.

**Spec:** `docs/superpowers/specs/2026-09-03-manager-workforce-ui-alignment-design.md`

## Global Constraints

- Preserve the existing persistent collapsible desktop navigation and temporary phone overlay.
- Do not add unapproved dashboard data, routes, AI actions, search behavior, notifications, or simulated operational status.
- Use the approved dark/light tokens, five accent choices, panel/tab treatments, and 150–200 ms functional motion.
- Keep Persian RTL and WCAG 2.2 AA behavior, including reduced motion and controlled LTR content.
- Write a failing focused test before each behavior change and verify it passes after the smallest implementation.

---

### Task 1: Preference contract and safe legacy palette handling

**Files:**
- Modify: `backend/src/EosDashboards.Application/Preferences/UserPreferencePalettes.cs`
- Modify: `backend/tests/EosDashboards.Application.Tests/Preferences/UserPreferenceTests.cs`
- Modify: `frontend/src/theme/palettes.ts`
- Modify: `frontend/src/app/providers/AppThemeProvider.tsx`
- Test: `frontend/src/theme/palettes.test.ts`
- Test: `frontend/src/theme/AppThemeProvider.test.tsx`

- [x] Add focused failing backend and frontend tests for the teal default and only the five approved persisted choices.
- [x] Run the focused tests and confirm their expected failures.
- [x] Replace the contract allowlists and palette definitions; map legacy stored palette values safely to teal.
- [x] Re-run the focused tests and refactor only if needed.

### Task 2: Shared operational theme tokens

**Files:**
- Modify: `frontend/src/theme/createAppTheme.ts`
- Test: `frontend/src/theme/createAppTheme.test.ts`

- [x] Add a failing test for the approved dark and light tokens and compact component defaults.
- [x] Run the test and confirm it fails for the previous theme.
- [x] Implement the smallest shared Material UI token and component-override changes.
- [x] Re-run the focused test.

### Task 3: Compact shell, workspace tabs, and no-data panel

**Files:**
- Modify: `frontend/src/layout/AppHeader.tsx`
- Modify: `frontend/src/layout/WorkspaceTabs.tsx`
- Modify: `frontend/src/layout/StatusBar.tsx`
- Modify: `frontend/src/pages/HomePage.tsx`
- Modify: `frontend/src/index.css`
- Test: `frontend/src/App.test.tsx`
- Test: `frontend/src/navigation/tabReducer.test.ts`

- [x] Add focused failing component tests for the operational no-data panel and the selected tab treatment.
- [x] Run the tests and confirm they fail because the new UI is absent.
- [x] Apply the shared panel and tab language without changing navigation or inventing operational data.
- [x] Re-run the focused tests and inspect the rendering locally.

### Task 4: Integrated verification and rendered QA

**Files:**
- Modify if needed: `frontend/tests/e2e/auth-shell.spec.ts`
- Modify: `docs/project/current-state.md`

- [x] Update the mocked browser flow only where it asserts the retired palette contract.
- [x] Run frontend lint, typecheck, formatting, build, focused tests, and the relevant browser flow.
- [x] Inspect the installed or local UI at desktop and phone widths; test tab and palette interactions, visual clipping, page scrolling, framework overlays, and relevant console errors.
- [x] Record completed implementation state and verification evidence in `current-state.md`.
