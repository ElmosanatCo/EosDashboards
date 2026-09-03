import ApartmentOutlinedIcon from "@mui/icons-material/ApartmentOutlined";
import GroupsOutlinedIcon from "@mui/icons-material/GroupsOutlined";
import SpaceDashboardOutlinedIcon from "@mui/icons-material/SpaceDashboardOutlined";
import AdminPanelSettingsOutlinedIcon from "@mui/icons-material/AdminPanelSettingsOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import type { SvgIconComponent } from "@mui/icons-material";
import type { ReactNode } from "react";
import type { TabDescriptor } from "./tabTypes";
import { ChiefExecutiveDashboardPage } from "../pages/ChiefExecutiveDashboardPage";
import { DepartmentDashboardPage } from "../pages/DepartmentDashboardPage";
import { HumanResourcesDashboardPage } from "../pages/HumanResourcesDashboardPage";
import { SystemAdministrationDashboardPage } from "../pages/SystemAdministrationDashboardPage";
import { SystemAuditPage } from "../pages/SystemAuditPage";
import { UserManagementPage } from "../pages/UserManagementPage";
import { DepartmentManagementPage } from "../pages/DepartmentManagementPage";

export type WorkspaceTarget = {
  routeId: string;
  pathname: string;
  title: string;
  keywords: readonly string[];
  requiredRoleCodes: readonly string[];
  Icon: SvgIconComponent;
  render: () => ReactNode;
};

export const workspaceTargets: readonly WorkspaceTarget[] = [
  {
    routeId: "system-administration-dashboard",
    pathname: "/system-administration-dashboard",
    title: "داشبورد مدیر سامانه",
    keywords: ["مدیر سامانه", "مدیریت سامانه", "امنیت"],
    requiredRoleCodes: ["SystemAdministrator"],
    Icon: AdminPanelSettingsOutlinedIcon,
    render: () => <SystemAdministrationDashboardPage />,
  },
  {
    routeId: "administration-users",
    pathname: "/users",
    title: "مدیریت کاربران",
    keywords: ["کاربر", "حساب", "نقش"],
    requiredRoleCodes: ["SystemAdministrator"],
    Icon: GroupsOutlinedIcon,
    render: () => <UserManagementPage />,
  },
  {
    routeId: "administration-departments",
    pathname: "/departments",
    title: "مدیریت واحدها",
    keywords: ["واحد", "سازمان", "بخش"],
    requiredRoleCodes: ["SystemAdministrator"],
    Icon: ApartmentOutlinedIcon,
    render: () => <DepartmentManagementPage />,
  },
  {
    routeId: "administration-audit",
    pathname: "/system-audit",
    title: "ممیزی سامانه",
    keywords: ["ممیزی", "لاگ", "رویداد", "ورود"],
    requiredRoleCodes: ["SystemAdministrator"],
    Icon: HistoryOutlinedIcon,
    render: () => <SystemAuditPage />,
  },
  {
    routeId: "department-dashboard",
    pathname: "/department-dashboard",
    title: "داشبورد بخش",
    keywords: ["بخش", "مدیر بخش"],
    requiredRoleCodes: ["DepartmentManager"],
    Icon: SpaceDashboardOutlinedIcon,
    render: () => <DepartmentDashboardPage />,
  },
  {
    routeId: "human-resources-dashboard",
    pathname: "/human-resources-dashboard",
    title: "داشبورد منابع انسانی",
    keywords: ["منابع انسانی", "مدیر منابع انسانی"],
    requiredRoleCodes: ["HumanResourcesManager"],
    Icon: GroupsOutlinedIcon,
    render: () => <HumanResourcesDashboardPage />,
  },
  {
    routeId: "chief-executive-dashboard",
    pathname: "/chief-executive-dashboard",
    title: "داشبورد مدیرعامل",
    keywords: ["مدیرعامل", "مدیر عامل"],
    requiredRoleCodes: ["ChiefExecutiveOfficer"],
    Icon: ApartmentOutlinedIcon,
    render: () => <ChiefExecutiveDashboardPage />,
  },
];

export function authorizedWorkspaceTargets(roleCodes: readonly string[]) {
  return workspaceTargets.filter((target) =>
    target.requiredRoleCodes.some((roleCode) => roleCodes.includes(roleCode)),
  );
}

export function createWorkspaceTab(target: WorkspaceTarget): TabDescriptor {
  return {
    key: target.routeId,
    routeId: target.routeId,
    pathname: target.pathname,
    search: "",
    title: target.title,
    closable: true,
  };
}

export function targetForRouteId(routeId: string) {
  return workspaceTargets.find((target) => target.routeId === routeId);
}

export function targetForPathname(pathname: string) {
  return workspaceTargets.find((target) => target.pathname === pathname);
}
