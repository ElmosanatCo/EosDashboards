# 0013 — User-visible Persian numerals

**Status:** Accepted

**Date:** 2026-09-04

## Context

The initial display rule used Persian digits for ordinary user-facing counts
and dates but kept visible identifiers and credentials in ASCII. The user
clarified that the visual language must be consistent across the application,
including numbers embedded in usernames, passwords when revealed,
personnel/organizational identifiers, versions, masked contact values, and IP
addresses.

## Decision

Render every number visible to an application user with Persian digits. Keep
form state, API request values, URL/API values, and internal identifiers in the
representation required by their existing contracts. Apply the locally hosted
Vazirmatn Farsi-Digits weight files to numeric-bearing controls and values
while retaining Vazirmatn as the only application typeface.

## Rationale

This provides one predictable Persian visual language without introducing a
second typeface, and without depending on browser OpenType feature support or
changing authentication semantics, identifier comparisons, or protocol
contracts.

## Consequences

- Numeric-bearing credential, identity, administration, audit, status, search,
  date/time, and contact displays require Persian-digit visual coverage.
- Tests must verify both Persian visual rendering and unchanged submitted
  values where a control participates in an API request.
- Technical values may remain LTR for readability while their visible digits
  use Persian glyphs.

## Supersedes/Superseded by

This decision supersedes the narrower numeric-display wording in the
appearance and shell requirement.
