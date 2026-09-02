import {
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
} from "@mui/material";
import HomeIcon from "@mui/icons-material/Home";

export const sidebarWidth = 240;

export function AppSidebar({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  return (
    <Drawer
      variant="persistent"
      anchor="right"
      open={open}
      sx={{
        width: open ? sidebarWidth : 0,
        flexShrink: 0,
        "& .MuiDrawer-paper": {
          width: sidebarWidth,
          top: 64,
          boxSizing: "border-box",
        },
      }}
    >
      <Toolbar
        sx={{ display: { xs: "none", sm: "block" }, minHeight: "0!important" }}
      />
      <List component="nav" aria-label="منوی اصلی">
        <ListItemButton selected onClick={onClose}>
          <ListItemIcon>
            <HomeIcon />
          </ListItemIcon>
          <ListItemText primary="خانه" />
        </ListItemButton>
      </List>
    </Drawer>
  );
}
