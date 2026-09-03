import { Box } from "@mui/material";
import { Alert } from "@mui/material";
import { useUserPreferences } from "../app/providers/UserPreferenceProvider";
import { useTabWorkspace } from "../navigation/TabWorkspaceProvider";
import { renderTab } from "../navigation/routeRegistry";
import { AppHeader } from "./AppHeader";
import { AppSidebar, sidebarWidth } from "./AppSidebar";
import { StatusBar } from "./StatusBar";
import { WorkspaceTabs } from "./WorkspaceTabs";

export function AppShell() {
  const { sidebarCollapsed, updateSidebarCollapsed, saveFailed } =
    useUserPreferences();
  const sidebarOpen = !sidebarCollapsed;
  const { tabs, activeKey } = useTabWorkspace();
  const active = tabs.find((tab) => tab.key === activeKey) ?? tabs[0];
  const toggleSidebar = () => updateSidebarCollapsed(!sidebarCollapsed);
  return (
    <Box
      sx={{
        height: "100dvh",
        display: "flex",
        flexDirection: "column",
        overflow: "hidden",
      }}
    >
      <AppHeader onMenu={toggleSidebar} />
      <Box sx={{ display: "flex", flex: 1, minHeight: 0 }}>
        <AppSidebar open={sidebarOpen} onClose={() => undefined} />
        <Box
          sx={{
            flex: 1,
            minWidth: 0,
            width: sidebarOpen ? `calc(100% - ${sidebarWidth}px)` : "100%",
            display: "flex",
            flexDirection: "column",
          }}
        >
          <WorkspaceTabs />
          {saveFailed ? (
            <Alert severity="warning">
              ذخیره تنظیمات انجام نشد و مقدار قبلی بازگردانده شد.
            </Alert>
          ) : null}
          <Box
            component="main"
            sx={{ flex: 1, overflow: "auto", p: { xs: 2, md: 3 } }}
          >
            {renderTab(active)}
          </Box>
          <StatusBar />
        </Box>
      </Box>
    </Box>
  );
}
