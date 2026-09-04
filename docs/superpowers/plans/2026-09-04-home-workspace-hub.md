# Plan: Build the role-aware Home workspace hub

## Goal

Replace the current Home tab no-data card with a responsive, Persian RTL
workspace hub that explains the signed-in manager's available capabilities,
offers direct actions for authorized pages, shows open-workspace continuation,
reserves a visible area for future capabilities, and includes an honest empty
state for alerts and action-needed work until an approved real data source
exists.

## Scope and constraints

- Use the existing authenticated user, authorized workspace-target registry, and
  internal tab workspace as the initial sources of truth.
- Do not add simulated metrics, alerts, tasks, counts, or future product
  capabilities.
- Keep authorization filtering in the existing `authorizedWorkspaceTargets`
  function; Home must never reveal a page that the current user cannot open.
- Keep the existing dark-default manager visual system, RTL layout, shared
  accent-card treatment, fixed home tab, and responsive no-horizontal-overflow
  behavior.
- Do not add a backend endpoint or deploy/publish this slice; the alert feed and
  future-module registry remain explicit extension points for a later approved
  data contract.

## Implementation tasks

### 1. Add a small, testable Home content model

Files:

- `frontend/src/pages/home/homeContent.ts` (new)
- `frontend/src/pages/home/homeContent.test.ts` (new)

Define pure types and helpers for Home copy and display data:

- Role-aware guide text selected from the user's role codes, with a safe
  fallback for a future role.
- Capability summaries and action labels keyed by existing route IDs, with a
  generic fallback so a newly registered authorized target automatically
  appears on Home even before dedicated copy is added.
- A typed `HomeAlert`/action-needed boundary with an empty initial collection;
  do not populate it with fake records.
- A helper for selecting the limited set of open workspace tabs shown in
  “ادامهٔ کار”, excluding the fixed Home tab while preserving tab order.

Tests first should cover administrator copy, fallback copy, route-summary
fallback, and filtering the fixed Home tab without exposing unauthorized
targets.

### 2. Build the Home presentation and provider-backed container

Files:

- `frontend/src/pages/HomePage.tsx`
- `frontend/src/pages/HomePage.test.tsx`
- `frontend/src/navigation/routeRegistry.tsx` (only if the container/view
  split requires a route registration adjustment)

Keep `HomePage` as the provider-backed container. It will read `user` from
`useAuth`, derive targets using `authorizedWorkspaceTargets(user.roleCodes)`,
and read `tabs` plus `dispatch` from `useTabWorkspace`. Opening a capability
dispatches the existing `open` action with `createWorkspaceTab(target)`;
continuing an open tab dispatches the existing `activate` action.

Expose a presentational view with explicit props so most rendering remains
unit-testable without constructing the full authentication tree.

Render these sections in a responsive grid/stack:

- Welcome/context panel with the user's name, department, role-aware guide,
  and a short keyboard-search hint.
- “امکانات در اختیار شما” cards generated only from authorized targets.
- “کارهایی که می‌توانید انجام دهید” action cards/buttons using the same
  authorized target source and existing workspace-tab behavior.
- “هشدارها و کارهای نیازمند اقدام” with a clearly labeled empty state until a
  real approved source supplies items.
- “ادامهٔ کار” for currently open non-Home tabs, with activation controls.
- “امکانات آینده” as a restrained reserved area that explains it will fill as
  approved capabilities are added; it must not present unimplemented items as
  available.

Update component tests before implementation so the old no-data assertion is
replaced by red tests for the approved headings, role-filtered capability
cards, empty alert state, future-reserved area, and action dispatch behavior.
Then implement the smallest UI that makes those tests pass.

### 3. Verify responsive and accessibility behavior in the rendered app

Files:

- `frontend/tests/e2e/auth-shell.spec.ts` (extend the existing mocked
  authenticated coverage)
- `frontend/src/pages/HomePage.test.tsx` (focused component behavior)

Add a mocked authenticated browser flow that verifies:

- An administrator sees the approved administration capabilities and guide.
- A role with a different authorized target does not see administrator-only
  cards.
- Clicking a Home action opens/activates the expected workspace tab.
- The alerts/action-needed section is visible and honest when no data exists.
- The future area is visible without claiming an unimplemented feature.
- Desktop and phone-sized layouts wrap cards and action controls without
  horizontal overflow; headings and buttons remain readable in RTL.
- Every icon-only control introduced by this slice has an accessible label or
  tooltip; text actions retain visible labels.

Use the existing focused frontend test, typecheck, formatting, and mocked
browser-test commands. Do not run an IIS publication or authenticated live
smoke flow for this UI-only change unless separately requested.

### 4. Update durable project memory

Files:

- `docs/project/current-state.md`
- `docs/project/requirements.md` only if the approved Home behavior belongs in
  the product requirements rather than implementation state

Record that the Home tab now contains role-filtered guidance, authorized
capability/action sections, an honest alert/action-needed empty state, open-tab
continuation, and a reserved future-capability area. Record the no-fake-data
boundary and the requirement that future capabilities update the authorized
Home content automatically. Keep the current next-step product-discovery
questions intact.

## Verification commands

From `frontend/`:

1. `npm test -- --run src/pages/home/homeContent.test.ts src/pages/HomePage.test.tsx`
2. `npm run typecheck`
3. `npm run format:check`
4. `npm run e2e -- tests/e2e/auth-shell.spec.ts`

If the focused checks pass, run `npm run build` as the final frontend
verification. Review the rendered Home tab at desktop and phone widths through
the existing local preview before claiming completion.

## Completion criteria

- Home is useful for the current user without inventing data.
- Authorized capabilities and actions are generated from the existing role
  filtering, and unauthorized items never render.
- The alerts/action-needed and future-capability areas are present, clearly
  labeled, and honest about the absence of a real data source.
- Open tabs can be resumed from Home.
- The layout remains coherent and free of horizontal overflow on narrow
  screens.
- Focused tests, typecheck, formatting, build, and the relevant mocked browser
  flow pass.
- Canonical project memory describes the new durable behavior.

