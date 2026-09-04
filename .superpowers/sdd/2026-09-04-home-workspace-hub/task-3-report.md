# Task 3 report — rendered Home verification

## Changed files

- `frontend/tests/e2e/auth-shell.spec.ts`
  - Replaced both authenticated assertions for the retired
    `داده‌ای برای نمایش وجود ندارد.` text with current Home verification.
  - Added exact assertions for all six approved Home headings, administrator
    welcome context, guide copy, and the `Ctrl+K` hint.
  - Verified the administrator's capability cards and action cards while
    retaining the existing global-search assertion that excludes
    `داشبورد مدیرعامل`.
  - Verified the exact alert and future-capability empty-state text.
  - Verified that a Home capability action opens `مدیریت کاربران`, that the
    opened tab appears under `ادامهٔ کار` after returning Home, and that its
    continue action activates the tab again.
  - Added a real `390x844` rendered Home check for equal scroll/client widths
    and card/action bounds inside the viewport.
  - Preserved the existing sign-in, shell, drawer, search, palette, audit,
    Google, OTP, forwarding, and logout flows.

The existing untracked plan file was not modified.

## Tests run and outputs

### Focused mocked browser flow

Command:

```text
npm run e2e -- tests/e2e/auth-shell.spec.ts
```

Result:

```text
Running 9 tests using 1 worker
9 passed (17.7s)
```

The run used the repository's deterministic Playwright route mocks and did
not use live credentials or IIS. The Browser plugin was not available in this
session, so the repository Playwright workflow was used.

### Focused frontend Home tests

Command:

```text
npm test -- --run src/pages/home/homeContent.test.ts src/pages/HomePage.test.tsx
```

Result:

```text
Test Files  2 passed (2)
Tests       15 passed (15)
Duration    2.30s
```

### Static checks

Command:

```text
& .\\node_modules\\.bin\\prettier.cmd --check tests/e2e/auth-shell.spec.ts
git diff --check
```

Result:

```text
All matched files use Prettier code style!
git diff --check: exit 0
```

## Self-review

- The retired no-data string no longer appears in `auth-shell.spec.ts`.
- New locators are scoped to Home regions/cards where repeated Persian labels
  exist, avoiding strict-mode ambiguity while retaining visible-title checks.
- The mobile assertion measures the rendered page after setting the browser
  viewport to exactly `390x844`; it checks Home workspace overflow and every
  Home accent card and action button against the viewport bounds.
- No production, backend, route, mock-service, credential, or IIS files were
  changed.
- No fabricated alerts, metrics, future features, or unauthorized page
  assertions were added.

## Concerns

None for the requested scope. The only environment note is that validation
used the repository Playwright runner because the Browser plugin was not
available.

## Fix round 1 report — rendered Home review findings

### Review findings addressed

- P2 heading coverage now scopes every heading to `home-workspace` and uses
  `exact: true`, proving the six required Home headings without page-wide or
  partial-name matching.
- The `390x844` browser check now asserts `window.innerWidth`,
  `document.documentElement.scrollWidth`, and `document.body.scrollWidth` are
  all exactly `390`.
- The mobile check now counts the five capability cards and five action cards,
  proves each group occupies more than one rendered row, and verifies every
  action control is below its card title at the mobile breakpoint. Existing
  viewport-bound checks remain in place.

### Verification

Command:

```text
npm run e2e -- tests/e2e/auth-shell.spec.ts
```

Result:

```text
Running 9 tests using 1 worker
9 passed (17.6s)
```

Additional checks:

```text
& .\\node_modules\\.bin\\prettier.cmd --check tests/e2e/auth-shell.spec.ts
All matched files use Prettier code style!

git diff --check
exit 0
```

### Self-review and concerns

- Only `frontend/tests/e2e/auth-shell.spec.ts` and this required report were
  changed; the existing untracked plan remains untouched and uncommitted.
- Existing sign-in, shell, search, drawer, palette, audit, Google, OTP,
  forwarding, and logout coverage remains preserved.
- No production code, backend code, live credential, IIS, or project
  documentation was changed.
- No concerns for this fix round. The repository Playwright runner remains the
  validation path because the Browser plugin is unavailable in this session.
