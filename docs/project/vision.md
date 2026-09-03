# Product Vision

**Last updated:** 2026-09-03

## Confirmed purpose

EosDashboards is a company web application that will provide multiple dashboards to managers.

## Confirmed users

- Department managers, who receive dashboards appropriate to the valuable data and responsibilities available in their departments.
- The CEO, who monitors valuable company-wide information to support management decisions.
- A system administrator, initially represented by one pre-provisioned account with full application access, who manages user accounts, access assignments, and company departments.
- An HR Manager, whose detailed authorization assignment remains subject to dashboard discovery.

Company departments may be independent or form a two-level hierarchy of parent departments and direct children. Specific department roles, permissions, dashboard assignments, audiences, metrics, workflows, and data sources remain unresolved. The approved role-content defaults define the intended manager-facing interface direction; they do not themselves authorize access or select dashboard data.

## Expected outcome

Managers can access the dashboards relevant to their responsibilities through one web application. The CEO can obtain a broader company-wide view for management monitoring and decision-making.

The dashboards, metrics, source systems, update frequency, and success measures are unresolved and require product discovery.

## Confirmed product qualities

- Persian, RTL-first, consistent, accessible management experience.
- A calm, technical, dense-but-readable workforce-operations workspace for manager-facing pages, with dark mode as the default and selectable interaction accents.
- A compact global command search that helps each user discover and open only the pages and operations they are permitted to use.
- Desktop-first responsive behavior, with usable essential flows on tablets and phones.
- Auditable, secure, maintainable, and suitable for IIS deployment inside the organization.
- Living technical documentation plus formal Persian documentation suitable for printing and organizational archive.

## Scope boundary

The first release starts with pre-provisioned local username/password sign-in plus mandatory SMS OTP. System-administrator user/access and department-hierarchy management are approved product scope but require a separate implementation design. Internet-facing or organizational-directory authentication, dashboard-specific reporting and alerting, data-entry workflows, and source-system integrations require later discovery and approval.
