# 0009 — System administration and department hierarchy scope

**Status:** Accepted

**Date:** 2026-09-03

**Supersedes (in part):** 0004's deferral of user and role administration UI.

## Context

The initial authentication slice established one deployment-provisioned System
Administrator and deferred user and role administration. The product now
requires that administrator to define users, manage their access, and define
the company department structure.

## Decision

System-administrator user-account and access-assignment management is approved
product scope. Company departments are also system-administrator managed. A
department is either independent or a direct child of one parent department;
the hierarchy may not exceed two levels.

The current authentication behavior remains unchanged until a separate
implementation design is approved: no self-registration is introduced, and
the controlled deployment tool remains the current account/password-management
mechanism.

## Rationale

Administration must be available in the application to support organizational
operation beyond the initial bootstrap account. A two-level department model
represents the confirmed structure while preventing unneeded recursive
organizational complexity.

## Consequences

- A future administrative slice must implement server-enforced authorization,
  audited user/access changes, and the bounded department hierarchy.
- The initial roles and policies, account lifecycle actions, department fields,
  and department deletion or re-parenting behavior remain unresolved and must
  be designed before implementation.
- Dashboard access, metrics, data sources, and organizational manager
  assignments remain separate product-discovery decisions.
