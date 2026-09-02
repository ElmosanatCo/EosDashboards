import { Box } from "@mui/material";
import { useState } from "react";
import { useTabWorkspace } from "../navigation/TabWorkspaceProvider";
import { renderTab } from "../navigation/routeRegistry";
import { AppHeader } from "./AppHeader";
import { AppSidebar, sidebarWidth } from "./AppSidebar";
import { StatusBar } from "./StatusBar";
import { WorkspaceTabs } from "./WorkspaceTabs";

export function AppShell() {
  const [sidebarOpen, setSidebarOpen] = useState(() => localStorage.getItem("eos.sidebar.collapsed") !== "true");
  const { tabs, activeKey } = useTabWorkspace();
  const active = tabs.find((tab) => tab.key === activeKey) ?? tabs[0];
  const toggleSidebar = () => setSidebarOpen((value) => {
    const next = !value;
    localStorage.setItem("eos.sidebar.collapsed", String(!next));
    return next;
  });
  return (
    <Box sx={{ height: "100dvh", display: "flex", flexDirection: "column", overflow: "hidden" }}>
      <AppHeader onMenu={toggleSidebar} />
      <Box sx={{ display: "flex", flex: 1, minHeight: 0 }}>
        <AppSidebar open={sidebarOpen} onClose={() => undefined} />
        <Box sx={{ flex: 1, minWidth: 0, width: sidebarOpen ? `calc(100% - ${sidebarWidth}px)` : "100%", display: "flex", flexDirection: "column" }}>
          <WorkspaceTabs />
          <Box component="main" sx={{ flex: 1, overflow: "auto", p: { xs: 2, md: 3 } }}>{renderTab(active)}</Box>
          <StatusBar />
        </Box>
      </Box>
    </Box>
  );
}
