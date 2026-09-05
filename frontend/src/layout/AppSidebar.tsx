import {
  Box,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Tooltip,
} from "@mui/material";
import { alpha } from "@mui/material/styles";
import CloseIcon from "@mui/icons-material/Close";
import HomeIcon from "@mui/icons-material/Home";
import type { WorkspaceTarget } from "../navigation/workspaceTargets";
import { appHeaderHeight } from "./AppHeader";
import { statusBarHeight } from "./StatusBar";
import { AppBrand } from "./AppBrand";

export const sidebarWidth = 240;

export function AppSidebar({
  open,
  onClose,
  temporary,
  activeRouteId,
  targets,
  onOpenTarget,
  onActivateHome,
  showBrand,
}: {
  open: boolean;
  onClose: () => void;
  temporary: boolean;
  activeRouteId: string;
  targets: readonly WorkspaceTarget[];
  onOpenTarget: (target: WorkspaceTarget) => void;
  onActivateHome: () => void;
  showBrand: boolean;
}) {
  return (
    <Drawer
      variant={temporary ? "temporary" : "persistent"}
      anchor="left"
      open={open}
      onClose={onClose}
      ModalProps={{
        keepMounted: true,
        sx: temporary
          ? { bottom: statusBarHeight, top: appHeaderHeight }
          : undefined,
      }}
      slotProps={{
        paper: {
          sx: {
            width: { xs: "min(280px, 86vw)", md: sidebarWidth },
            border: "none",
            top: appHeaderHeight,
            bottom: statusBarHeight,
            height: "auto",
            boxSizing: "border-box",
          },
        },
      }}
      sx={{
        width: temporary ? 0 : open ? sidebarWidth : 0,
        flexShrink: temporary ? undefined : 0,
      }}
    >
      {showBrand && (
        <Box
          sx={{
            px: 2,
            py: 1.5,
            borderBottom: "1px solid",
            borderColor: "divider",
          }}
        >
          <AppBrand />
        </Box>
      )}
      <Box
        data-testid="mobile-sidebar-close-slot"
        sx={{
          display: { xs: "block", md: "none" },
          position: "absolute",
          top: 8,
          "/* @noflip */ left": 8,
          zIndex: 1,
        }}
      >
        <Tooltip title="بستن منو">
          <IconButton aria-label="بستن منو" onClick={onClose}>
            <CloseIcon />
          </IconButton>
        </Tooltip>
      </Box>
      <List component="nav" aria-label="منوی اصلی">
        <ListItemButton
          selected={activeRouteId === "home"}
          onClick={onActivateHome}
          sx={(theme) => ({
            position: "relative",
            "&.Mui-selected": {
              backgroundColor: alpha(theme.palette.primary.main, 0.06),
              backgroundImage: `linear-gradient(90deg, transparent 0%, ${alpha(theme.palette.primary.main, 0.035)} 78%, ${alpha(theme.palette.primary.main, 0.13)} 100%)`,
            },
            "&:hover": {
              backgroundColor: alpha(theme.palette.primary.main, 0.045),
            },
            "&.Mui-selected:hover": {
              backgroundColor: alpha(theme.palette.primary.main, 0.085),
            },
          })}
        >
          <ListItemIcon>
            <HomeIcon />
          </ListItemIcon>
          <ListItemText primary="خانه" />
          {activeRouteId === "home" ? <SidebarActiveIndicator /> : null}
        </ListItemButton>
        {targets.map((target) => (
          <ListItemButton
            key={target.routeId}
            selected={activeRouteId === target.routeId}
            onClick={() => onOpenTarget(target)}
            sx={(theme) => ({
              position: "relative",
              "&.Mui-selected": {
                backgroundColor: alpha(theme.palette.primary.main, 0.06),
                backgroundImage: `linear-gradient(90deg, transparent 0%, ${alpha(theme.palette.primary.main, 0.035)} 78%, ${alpha(theme.palette.primary.main, 0.13)} 100%)`,
              },
              "&:hover": {
                backgroundColor: alpha(theme.palette.primary.main, 0.045),
              },
              "&.Mui-selected:hover": {
                backgroundColor: alpha(theme.palette.primary.main, 0.085),
              },
            })}
          >
            <ListItemIcon>
              <target.Icon />
            </ListItemIcon>
            <ListItemText primary={target.title} />
            {activeRouteId === target.routeId ? (
              <SidebarActiveIndicator />
            ) : null}
          </ListItemButton>
        ))}
      </List>
    </Drawer>
  );
}

function SidebarActiveIndicator() {
  return (
    <Box
      data-testid="sidebar-active-indicator"
      aria-hidden="true"
      sx={{
        position: "absolute",
        top: 6,
        bottom: 6,
        width: 3,
        borderRadius: "3px 0 0 3px",
        bgcolor: "primary.main",
        "/* @noflip */ right": 0,
      }}
    />
  );
}
