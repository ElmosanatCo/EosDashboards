# Roadmap

**Last updated:** 2026-09-03

## Phase 1 — Project memory

**Status:** Complete

Create and verify the repository-based context and decision system.

## Phase 2 — Product discovery

**Status:** In progress

Identify users, dashboards, metrics, data sources, quality attributes, and the first deliverable slice.

Project-wide foundation standards are complete. Dashboard-specific discovery remains in progress.

## Phase 3 — Architecture and foundation

**Status:** Core implementation and local IIS deployment complete; SMS connectivity blocked

Scaffold the separate backend and frontend foundations; add database migrations and administrator provisioning; implement local username/password sign-in, SMS OTP, password recovery/change, session management, the RTL themed tabbed SPA shell, branding, status bar, and automated verification.

The code, isolated database verification, mocked browser flow, approved branding, separate IIS deployment, database migration, and initial administrator provisioning are complete. UI and API readiness pass locally. A user-authorized live sign-in reached the API and validated the provisioned account, but the configured external SMS endpoint timed out; end-to-end OTP, refresh, and logout smoke verification remains blocked until that private endpoint or its connectivity is corrected.

## Phase 4 — First vertical dashboard slice

**Status:** Planned

Deliver one approved dashboard end to end, including its data path, API, UI, authorization, and tests.

Later phases will be defined from validated requirements and feedback. No delivery dates are committed.
