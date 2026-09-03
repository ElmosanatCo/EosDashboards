import type { ReactNode } from "react";
import { HomePage } from "../pages/HomePage";
import { DepartmentFormPage } from "../pages/DepartmentFormPage";
import { UserFormPage } from "../pages/UserFormPage";
import type { TabDescriptor } from "./tabTypes";
import { targetForRouteId } from "./workspaceTargets";

const routes: Record<string, () => ReactNode> = { home: () => <HomePage /> };

export function renderTab(tab: TabDescriptor) {
  if (
    tab.routeId === "administration-user-create" ||
    tab.routeId === "administration-user-edit"
  ) {
    const value = tab.state?.id;
    return (
      <UserFormPage userId={typeof value === "number" ? value : undefined} />
    );
  }
  if (tab.routeId === "department-form") {
    const value = tab.state?.id;
    return (
      <DepartmentFormPage
        departmentId={typeof value === "number" ? value : undefined}
      />
    );
  }
  return (
    routes[tab.routeId] ??
    targetForRouteId(tab.routeId)?.render ??
    routes.home
  )();
}

export function createTabKey(
  routeId: string,
  parameters: Record<string, string> = {},
) {
  const suffix = Object.entries(parameters)
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([key, value]) => `${key}=${encodeURIComponent(value)}`)
    .join("&");
  return suffix ? `${routeId}?${suffix}` : routeId;
}
