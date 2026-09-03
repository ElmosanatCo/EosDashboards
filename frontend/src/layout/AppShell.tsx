import { Box, useMediaQuery } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { useEffect, useMemo, useState } from "react";
import { useAuth } from "../app/providers/AuthProvider";
import { useUserPreferences } from "../app/providers/UserPreferenceProvider";
import { useTabWorkspace } from "../navigation/TabWorkspaceProvider";
import { renderTab } from "../navigation/routeRegistry";
import {
  authorizedWorkspaceTargets,
  createWorkspaceTab,
  targetForPathname,
  targetForRouteId,
  type WorkspaceTarget,
} from "../navigation/workspaceTargets";
import { AppHeader } from "./AppHeader";
import { ChangePasswordDialog } from "../features/auth/ChangePasswordDialog";
import { AppSidebar, sidebarWidth } from "./AppSidebar";
import { StatusBar } from "./StatusBar";
import { WorkspaceTabs } from "./WorkspaceTabs";

export function AppShell() {
  const { user, changePassword } = useAuth();
  const { sidebarCollapsed, updateSidebarCollapsed } = useUserPreferences();
  const theme = useTheme();
  const compactLayout = useMediaQuery(theme.breakpoints.down("md"));
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);
  const [passwordBusy, setPasswordBusy] = useState(false);
  const [passwordError, setPasswordError] = useState<string>();
  const sidebarOpen = compactLayout ? mobileSidebarOpen : !sidebarCollapsed;
  const { tabs, activeKey, dispatch } = useTabWorkspace();
  const targets = useMemo(
    () => authorizedWorkspaceTargets(user.roleCodes),
    [user.roleCodes],
  );
  useEffect(() => {
    const authorizedRouteIds = new Set(targets.map((target) => target.routeId));
    for (const tab of tabs) {
      if (tab.routeId !== "home" && !authorizedRouteIds.has(tab.routeId)) {
        dispatch({ type: "close", key: tab.key, confirmed: true });
      }
    }
    const pathname =
      location.pathname.replace(
        import.meta.env.BASE_URL.replace(/\/$/, ""),
        "",
      ) || "/";
    const requestedTarget = targetForPathname(pathname);
    if (requestedTarget && !authorizedRouteIds.has(requestedTarget.routeId)) {
      dispatch({ type: "activate", key: "home" });
    }
  }, [dispatch, tabs, targets]);
  const active = tabs.find((tab) => tab.key === activeKey) ?? tabs[0];
  const toggleSidebar = () => {
    if (compactLayout) {
      setMobileSidebarOpen((open) => !open);
      return;
    }
    updateSidebarCollapsed(!sidebarCollapsed);
  };
  const openTarget = (target: WorkspaceTarget) => {
    dispatch({ type: "open", tab: createWorkspaceTab(target) });
    if (compactLayout) setMobileSidebarOpen(false);
  };
  const activateHome = () => {
    dispatch({ type: "activate", key: "home" });
  };
  return (
    <Box
      sx={{
        height: "100dvh",
        display: "flex",
        flexDirection: "column",
        overflow: "hidden",
        bgcolor: "background.default",
      }}
    >
      <AppHeader
        onMenu={toggleSidebar}
        targets={targets}
        onOpenTarget={openTarget}
      />
      <Box sx={{ display: "flex", flex: 1, minHeight: 0 }}>
        <AppSidebar
          open={sidebarOpen}
          temporary={compactLayout}
          targets={targets}
          onOpenTarget={openTarget}
          onActivateHome={activateHome}
          onClose={() => {
            if (compactLayout) setMobileSidebarOpen(false);
            else updateSidebarCollapsed(true);
          }}
        />
        <Box
          sx={{
            flex: 1,
            minWidth: 0,
            width:
              !compactLayout && sidebarOpen
                ? `calc(100% - ${sidebarWidth}px)`
                : "100%",
            display: "flex",
            flexDirection: "column",
          }}
        >
          <WorkspaceTabs />
          <Box
            component="main"
            sx={{ flex: 1, overflow: "auto", p: { xs: 2, md: 3 } }}
          >
            {active.routeId === "home" || targetForRouteId(active.routeId)
              ? renderTab(active)
              : renderTab(tabs[0])}
          </Box>
        </Box>
      </Box>
      <StatusBar />
      <ChangePasswordDialog
        open={user.mustChangePassword}
        required
        busy={passwordBusy}
        error={passwordError}
        onClose={() => undefined}
        onSubmit={async (currentPassword, newPassword) => {
          setPasswordBusy(true);
          setPasswordError(undefined);
          try {
            await changePassword(currentPassword, newPassword);
          } catch {
            setPasswordError("ثبت رمز جدید ممکن نشد. دوباره تلاش کنید.");
          } finally {
            setPasswordBusy(false);
          }
        }}
      />
    </Box>
  );
}
