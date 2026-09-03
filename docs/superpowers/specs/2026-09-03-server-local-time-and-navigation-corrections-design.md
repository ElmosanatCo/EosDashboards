# Server-local time and navigation corrections design

**Date:** 2026-09-03

**Status:** Approved in conversation; awaiting written-spec review

## Goal

Replace the universal-time persistence convention with local application
server time at millisecond precision. Correct two shell navigation behaviors:
the fixed home tab must activate without collapsing the desktop menu, and the
temporary mobile drawer must stop above the status bar.

## Time model

All application timestamps are local server wall-clock values. The clock
returns the current local server time truncated to milliseconds. Domain,
Application, API, Infrastructure, persistence configurations, migrations,
indexes, tests, and browser contracts rename every `Utc` time member to the
same PascalCase name without the suffix, for example `CreatedAt`, `ExpiresAt`,
and `OccurredAt`.

SQL Server persists these values as `datetime2(3)`. A value therefore has
only year, month, day, hour, minute, second, and millisecond—no offset,
timezone, or finer fractional precision. Normal business comparisons for OTP
validity, resend availability, sessions, auditing, and preference updates use
these local values directly. No normal path converts a database value to
Asia/Tehran time.

The migration converts each existing UTC `datetimeoffset` value once to its
equivalent server-local wall-clock value, then changes it to `datetime2(3)`
and renames the column. This preserves the real historic time of existing
records while adopting the approved representation. JWT numeric-date claims
may make a short-lived protocol conversion because that external standard
requires it; no resulting UTC value is stored in the application database.

## Navigation corrections

The sidebar receives an explicit home-activation callback. Clicking `خانه`
activates the existing, non-closable home tab and preserves the persistent
desktop sidebar state; it never calls the collapse callback. Opening another
authorized target keeps the existing desktop behavior and continues to close
the temporary mobile drawer after navigation. The home action does not close
the temporary drawer either; its close button and backdrop remain available.

The mobile drawer paper is bounded below by the fixed status-bar height and
above by the header. Its modal/backdrop region follows the same vertical
limits, so neither the menu surface nor its overlay covers the status bar.

## Tests and verification

- Domain/Application tests use a millisecond-precision local test clock and
  assert expiry, resend, refresh, audit, and session behavior without `Utc`
  names.
- An EF integration test applies the migration and confirms renamed
  `datetime2(3)` columns and local-wall-clock conversion of representative
  legacy values.
- Frontend tests assert that the home action activates the fixed tab without
  invoking sidebar collapse, and that temporary drawer geometry ends at the
  status-bar boundary.
- Type checking, targeted backend/frontend tests, and browser shell tests
  verify the released behavior. A rendered mobile viewport check confirms the
  status bar remains visible and interactive while the drawer is open.

## Scope boundary

This change does not design a new dashboard, change roles or authorization,
alter the status-bar content, or introduce cross-time-zone support.
