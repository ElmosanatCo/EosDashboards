import { Box, useMediaQuery } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { useState } from "react";
import { useUserPreferences } from "../app/providers/UserPreferenceProvider";
import { useTabWorkspace } from "../navigation/TabWorkspaceProvider";
import { renderTab } from "../navigation/routeRegistry";
import { AppHeader } from "./AppHeader";
import { AppSidebar, sidebarWidth } from "./AppSidebar";
import { StatusBar } from "./StatusBar";
import { WorkspaceTabs } from "./WorkspaceTabs";

export function AppShell() {
  const { sidebarCollapsed, updateSidebarCollapsed } = useUserPreferences();
  const theme = useTheme();
  const compactLayout = useMediaQuery(theme.breakpoints.down("md"));
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);
  const sidebarOpen = compactLayout ? mobileSidebarOpen : !sidebarCollapsed;
  const { tabs, activeKey } = useTabWorkspace();
  const active = tabs.find((tab) => tab.key === activeKey) ?? tabs[0];
  const toggleSidebar = () => {
    if (compactLayout) {
      setMobileSidebarOpen((open) => !open);
      return;
    }
    updateSidebarCollapsed(!sidebarCollapsed);
  };
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
        <AppSidebar
          open={sidebarOpen}
          temporary={compactLayout}
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
            {renderTab(active)}
          </Box>
        </Box>
      </Box>
      <StatusBar />
    </Box>
  );
}
