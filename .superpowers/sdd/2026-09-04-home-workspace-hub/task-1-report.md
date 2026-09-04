# Task 1 report — Home content model

## Changed files

- `frontend/src/pages/home/homeContent.ts`
- `frontend/src/pages/home/homeContent.test.ts`

The required report is written to this file. No other implementation or
project files were changed.

## Red/green test cycle

### Red

Command, run before creating the production helper module:

```text
npm test -- --run src/pages/home/homeContent.test.ts
```

Result:

```text
FAIL  src/pages/home/homeContent.test.ts [ src/pages/home/homeContent.test.ts ]
Error: Failed to resolve import "./homeContent" from "src/pages/home/homeContent.test.ts". Does the file exist?
Test Files  1 failed (1)
Tests  no tests
```

This was the expected red result because the test-first module did not yet
exist.

### Green

After implementing the helpers:

```text
npm test -- --run src/pages/home/homeContent.test.ts
```

Result:

```text
Test Files  1 passed (1)
Tests  6 passed (6)
```

## Design notes

- `getHomeGuideText` applies the approved role precedence and exact Persian
  copy, with the approved fallback for unknown roles.
- `homeTargetMetadata` contains concise summary/action metadata for all seven
  current `workspaceTargets` route IDs. `getHomeTargetMetadata` returns the
  approved generic fallback for unknown route IDs.
- `HomeAlert` is an explicit typed boundary with the required fields and
  severity union. `initialHomeAlerts` is intentionally empty.
- `selectRecentHomeTabs` returns a new array containing at most four tabs,
  excludes the fixed `home` key, and preserves input order.

## Additional verification

- `npm run typecheck` — passed.
- `npx prettier --check src/pages/home/homeContent.ts src/pages/home/homeContent.test.ts` — passed.

## Self-review findings

- Scope is limited to the pure content boundary and focused tests.
- No UI rendering, API calls, fake records, authorization changes, route
  registry changes, reducer changes, or documentation changes were made.
- The recent-tab helper does not mutate its input; the test preserves and
  compares the original array.
- The metadata coverage test checks every currently registered target.

## Concerns

None.
