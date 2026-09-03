# Roadmap

**Last updated:** 2026-09-03

## Phase 1 — Project memory

**Status:** Complete

Create and verify the repository-based context and decision system.

## Phase 2 — Product discovery

**Status:** In progress

Identify users, dashboards, metrics, data sources, quality attributes, and the first deliverable slice. The approved system-administration scope includes user/access management, a company-department hierarchy limited to parent departments and their direct children, and an operational audit dashboard. The data foundation, fixed roles, protected administration APIs, RTL management workspace, command search, and audit dashboard are complete; retention and business-dashboard discovery remain deferred.

Project-wide foundation standards are complete. Dashboard-specific discovery remains in progress.

## Phase 3 — Architecture and foundation

**Status:** Core implementation and local IIS deployment complete; Google activation pending client configuration

Scaffold the separate backend and frontend foundations; add database migrations and administrator provisioning; implement local username/password sign-in, SMS OTP, password recovery/change, session management, the RTL themed tabbed SPA shell, branding, status bar, and automated verification.

The code, isolated database verification, mocked browser flow, approved branding,
separate IIS deployment, database migration, and initial administrator
provisioning are complete. Local username/password plus SMS OTP remains
available. Pre-linked Google sign-in has its data model, server-owned PKCE
flow, API capability endpoints, and UI action implemented. Its real local
activation and smoke test await a Google Cloud Web OAuth client and its
server-side Client ID/Client Secret configuration.

## Phase 4 — First vertical dashboard slice

**Status:** Foundation complete; dashboard discovery planned

Deliver one approved dashboard end to end, including its data path, API, UI, authorization, and tests.

Later phases will be defined from validated requirements and feedback. No delivery dates are committed.
