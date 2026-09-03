# 0011 — Server-local millisecond timestamps

**Status:** Accepted

**Date:** 2026-09-03

## Context

The initial authentication implementation persisted technical timestamps as
UTC `datetimeoffset` values with `Utc` property names and converted displayed
values to Asia/Tehran time. The approved local deployment and business
operation instead use the application server's local wall clock.

## Decision

Persist every application timestamp as local server date and time in SQL
Server `datetime2(3)`. Persist only year, month, day, hour, minute, second,
and millisecond; do not persist an offset or finer precision. Use names such
as `CreatedAt`, `UpdatedAt`, `ExpiresAt`, `OccurredAt`, and `RevokedAt`, with
no `Utc` or underscore time suffix.

Use millisecond-precision local server time directly for ordinary application
logic. A one-time migration converts legacy UTC values to their equivalent
local wall-clock values before changing their column type and name. External
protocol adapters may make a transient conversion only where that protocol
requires it; this does not permit UTC persistence or a Tehran-time conversion
in normal application logic.

## Rationale

The system has one approved local server-time basis. A single local
wall-clock representation matches the operational requirement, gives direct
field names, and avoids unnecessary timezone conversion in application code.

## Consequences

- Every persisted-time property, database column, index, API contract, and
  test fixture is renamed without `Utc`.
- The migration is data-preserving in meaning: legacy UTC values are converted
  once before their offset is removed and precision is limited to milliseconds.
- Authentication/session comparisons use the local server clock. JWT numeric
  date conversion remains an external protocol detail.
- Cross-time-zone database replication or a server timezone change would
  require a new explicit time-model decision.

## Supersedes

The universal-time persistence rule in decision 0003 and the initial
authentication design's UTC timestamp convention.
