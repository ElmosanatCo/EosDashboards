import { Box, useMediaQuery } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
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
import { PageHelpFrame } from "../components/PageHelpFrame";
import { ChangePasswordDialog } from "../features/auth/ChangePasswordDialog";
import { AppSidebar, sidebarWidth } from "./AppSidebar";
import { StatusBar } from "./StatusBar";
import { WorkspaceTabs } from "./WorkspaceTabs";
import { pageGuideFor } from "../components/pageGuides";

export function AppShell() {
  const { user, changePassword } = useAuth();
  const { sidebarCollapsed, updateSidebarCollapsed } = useUserPreferences();
  const theme = useTheme();
  const compactLayout = useMediaQuery(theme.breakpoints.down("md"));
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);
  const [pageHelpOpen, setPageHelpOpen] = useState(false);
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
    const hasSystemAdministration = user.roleCodes.includes(
      "SystemAdministrator",
    );
    for (const tab of tabs) {
      const isAdministrationForm = [
        "administration-user-create",
        "administration-user-edit",
        "department-form",
      ].includes(tab.routeId);
      if (
        tab.routeId !== "home" &&
        !authorizedRouteIds.has(tab.routeId) &&
        !(hasSystemAdministration && isAdministrationForm)
      ) {
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
        pageHelpTitle={pageGuideFor(active.routeId).title}
        onOpenPageHelp={() => setPageHelpOpen(true)}
        showBrand={!compactLayout}
      />
      <Box sx={{ display: "flex", flex: 1, minHeight: 0 }}>
        <AppSidebar
          open={sidebarOpen}
          temporary={compactLayout}
          activeRouteId={active.routeId}
          targets={targets}
          onOpenTarget={openTarget}
          onActivateHome={activateHome}
          showBrand={compactLayout}
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
            sx={{
              flex: 1,
              minHeight: 0,
              overflow: "auto",
              p: 2,
              backgroundImage: `linear-gradient(225deg, ${alpha(theme.palette.primary.main, 0.08)} 0%, ${alpha(theme.palette.primary.main, 0.025)} 14%, transparent 25%)`,
              backgroundRepeat: "no-repeat",
            }}
          >
            <PageHelpFrame
              routeId={active.routeId}
              open={pageHelpOpen}
              onClose={() => setPageHelpOpen(false)}
            >
              {active.routeId === "home" ||
              targetForRouteId(active.routeId) ||
              active.routeId === "administration-user-create" ||
              active.routeId === "administration-user-edit" ||
              active.routeId === "department-form"
                ? renderTab(active)
                : renderTab(tabs[0])}
            </PageHelpFrame>
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
