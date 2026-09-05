# 0008 — Manager-facing workforce-operations UI standard

**Status:** Accepted

**Date:** 2026-09-03

## Context

The initial authentication shell established a Persian RTL application foundation, but dashboard-specific manager pages had not received a unified visual and interaction direction. The prior default palette and balanced corporate direction did not describe the required operational character, panel emphasis, or restrained AI-insight treatment for workforce management.

## Decision

Manager-facing pages use the workforce-operations UI rules in `../standards.md`: a compact, industrial, Persian RTL workspace; dark mode and teal as the defaults; the approved light and dark tokens; the six approved interaction accent choices; fixed header, tab bar, and status bar; flat accent-line panels; structured data-first content; and restrained, evidence-linked AI insights.

The existing persistent, collapsible hamburger side-menu standard remains in force. The role-content defaults guide future page design but do not approve their data, workflow, metric, source, or authorization behavior.

## Rationale

This direction gives future management screens a deliberate common language while keeping dashboard product discovery and authorization decisions evidence-based. Retaining the approved navigation model avoids an unnecessary shell-navigation reversal.

## Consequences

- New manager-facing UI specifications and implementations must follow the refined standard and be reviewed as rendered RTL interfaces.
- The current authentication-shell implementation is not redesigned by this documentation decision; alignment work requires a separately approved implementation task.
- Dashboard metrics, data sources, charting, permissions, workflows, and role assignments remain separate product-discovery decisions.
