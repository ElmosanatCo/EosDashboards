# Decision 0030: Human Resources dashboard and public-skill merge

**Date:** 2026-09-05  
**Status:** Accepted

## Decision

Human Resources receives an organization-level dashboard with an explicit
`همه بخش‌ها` or one-department selector. The dashboard presents approved
database-backed workforce and workflow cards, per-department change counts,
and retained job-description change history.

The existing Human Resources review target is renamed to `مدیریت شرح وظایف`
and contains three tabs: pending review, approved descriptions, and public
skills. Approved descriptions can be viewed, downloaded, and compared with
the immediately previous retained revision. Rejection requires a reason.

Public skills are global and therefore have no department selector in their
management UI. A merge explicitly identifies the source and surviving skill
in the confirmation modal. On confirmation, all existing references move to
the surviving skill, duplicate links collapse, the source is soft-deactivated,
and the operation is audited; retained history is never deleted.

## Rationale

This keeps the HR operator's work in one coherent management surface, makes
the review decision evidence-based through detail/download/compare actions,
and prevents an ambiguous global-skill merge from silently choosing the
surviving name or losing historical traceability.
