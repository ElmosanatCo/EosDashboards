import {
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
} from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import HomeIcon from "@mui/icons-material/Home";
import type { WorkspaceTarget } from "../navigation/workspaceTargets";
import { appHeaderHeight } from "./AppHeader";
import { statusBarHeight } from "./StatusBar";

export const sidebarWidth = 240;

export function AppSidebar({
  open,
  onClose,
  temporary,
  targets,
  onOpenTarget,
  onActivateHome,
}: {
  open: boolean;
  onClose: () => void;
  temporary: boolean;
  targets: readonly WorkspaceTarget[];
  onOpenTarget: (target: WorkspaceTarget) => void;
  onActivateHome: () => void;
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
      <Toolbar
        sx={{
          display: { xs: "flex", md: "none" },
          justifyContent: "flex-start",
          minHeight: "56px!important",
        }}
      >
        <IconButton aria-label="بستن منو" onClick={onClose}>
          <CloseIcon />
        </IconButton>
      </Toolbar>
      <Toolbar
        sx={{ display: { xs: "none", sm: "block" }, minHeight: "0!important" }}
      />
      <List component="nav" aria-label="منوی اصلی">
        <ListItemButton onClick={onActivateHome}>
          <ListItemIcon>
            <HomeIcon />
          </ListItemIcon>
          <ListItemText primary="خانه" />
        </ListItemButton>
        {targets.map((target) => (
          <ListItemButton
            key={target.routeId}
            onClick={() => onOpenTarget(target)}
          >
            <ListItemIcon>
              <target.Icon />
            </ListItemIcon>
            <ListItemText primary={target.title} />
          </ListItemButton>
        ))}
      </List>
    </Drawer>
  );
}
