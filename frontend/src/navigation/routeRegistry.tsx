import type { ReactNode } from "react";
import { HomePage } from "../pages/HomePage";
import type { TabDescriptor } from "./tabTypes";
import { targetForRouteId } from "./workspaceTargets";

const routes: Record<string, () => ReactNode> = { home: () => <HomePage /> };

export function renderTab(tab: TabDescriptor) {
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
