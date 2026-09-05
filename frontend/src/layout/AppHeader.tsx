import {
  AppBar,
  Box,
  IconButton,
  Divider,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Stack,
  Toolbar,
  Tooltip,
  Typography,
} from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import LogoutIcon from "@mui/icons-material/Logout";
import DarkModeIcon from "@mui/icons-material/DarkMode";
import LightModeIcon from "@mui/icons-material/LightMode";
import AccountCircleIcon from "@mui/icons-material/AccountCircle";
import HelpOutlineOutlinedIcon from "@mui/icons-material/HelpOutlineOutlined";
import PasswordIcon from "@mui/icons-material/Password";
import PaletteIcon from "@mui/icons-material/Palette";
import { useState } from "react";
import { useAuth } from "../app/providers/AuthProvider";
import { ChangePasswordDialog } from "../features/auth/ChangePasswordDialog";
import { useUserPreferences } from "../app/providers/UserPreferenceProvider";
import { paletteOptions } from "../theme/palettes";
import type { WorkspaceTarget } from "../navigation/workspaceTargets";
import { CommandSearch } from "./CommandSearch";
import { AppBrand } from "./AppBrand";

export const appHeaderHeight = 58;

export function AppHeader({
  onMenu,
  targets,
  onOpenTarget,
  pageHelpTitle,
  onOpenPageHelp,
  showBrand,
}: {
  onMenu: () => void;
  targets: readonly WorkspaceTarget[];
  onOpenTarget: (target: WorkspaceTarget) => void;
  pageHelpTitle: string;
  onOpenPageHelp: () => void;
  showBrand: boolean;
}) {
  const { user, logout, changePassword } = useAuth();
  const { palette, resolvedAppearanceMode, toggleAppearance, updatePalette } =
    useUserPreferences();
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const [passwordDialogOpen, setPasswordDialogOpen] = useState(false);
  const [passwordBusy, setPasswordBusy] = useState(false);
  const [passwordError, setPasswordError] = useState<string>();
  return (
    <AppBar
      position="static"
      component="header"
      elevation={0}
      sx={{ height: appHeaderHeight, boxSizing: "border-box", flexShrink: 0 }}
    >
      <Toolbar
        sx={{
          height: "100%",
          minHeight: "100% !important",
          px: { xs: 1, sm: 2 },
          gap: 0.5,
        }}
      >
        <Tooltip title="باز و بسته کردن منو">
          <IconButton
            color="inherit"
            edge="start"
            aria-label="باز و بسته کردن منو"
            onClick={onMenu}
          >
            <MenuIcon />
          </IconButton>
        </Tooltip>
        {showBrand && <AppBrand />}
        <Box
          sx={{ flexGrow: 1, display: "flex", justifyContent: "center", px: 1 }}
        >
          <CommandSearch targets={targets} onSelect={onOpenTarget} />
        </Box>
        <Tooltip title="منوی کاربر">
          <IconButton
            color="inherit"
            aria-label="منوی کاربر"
            onClick={(event) => setAnchorEl(event.currentTarget)}
          >
            <AccountCircleIcon />
          </IconButton>
        </Tooltip>
        <Tooltip title={pageHelpTitle}>
          <IconButton
            color="inherit"
            aria-label={pageHelpTitle}
            onClick={onOpenPageHelp}
          >
            <HelpOutlineOutlinedIcon />
          </IconButton>
        </Tooltip>
        <Tooltip title="تغییر حالت نمایش">
          <IconButton
            color="inherit"
            aria-label="تغییر حالت نمایش"
            onClick={toggleAppearance}
          >
            {resolvedAppearanceMode === "dark" ? (
              <LightModeIcon />
            ) : (
              <DarkModeIcon />
            )}
          </IconButton>
        </Tooltip>
        <Menu
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={() => setAnchorEl(null)}
          slotProps={{
            paper: {
              sx: {
                mt: 1,
                minWidth: 248,
                border: "1px solid",
                borderColor: "divider",
              },
            },
          }}
        >
          <Box sx={{ px: 2, py: 1.5, bgcolor: "action.hover" }}>
            <Stack direction="row" spacing={1.25} sx={{ alignItems: "center" }}>
              <AccountCircleIcon color="primary" />
              <Box>
                <Typography
                  className="eos-persian-number"
                  sx={{ fontWeight: 700 }}
                >
                  {user.firstName} {user.lastName}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  حساب کاربری
                </Typography>
              </Box>
            </Stack>
          </Box>
          <Divider />
          <MenuItem
            onClick={() => {
              setAnchorEl(null);
              setPasswordDialogOpen(true);
            }}
            sx={{ gap: 1.25, py: 1.25 }}
          >
            <ListItemIcon sx={{ minWidth: 32 }}>
              <PasswordIcon fontSize="small" />
            </ListItemIcon>
            <ListItemText
              primary="تغییر رمز"
              secondary="به‌روزرسانی رمز ورود"
            />
          </MenuItem>
          <Divider />
          <Box component="li" sx={{ listStyle: "none", px: 2, py: 1.25 }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
              <PaletteIcon fontSize="small" />
              <Typography variant="body2">رنگ‌بندی</Typography>
            </Stack>
            <Box
              aria-label="انتخاب رنگ‌بندی"
              sx={{
                display: "flex",
                flexWrap: "wrap",
                gap: 0.75,
                mt: 1.25,
                maxWidth: 196,
              }}
            >
              {paletteOptions.map((option) => (
                <Tooltip key={option.id} title={option.label}>
                  <IconButton
                    aria-label={`انتخاب رنگ‌بندی ${option.label}`}
                    aria-pressed={palette === option.id}
                    size="small"
                    onClick={() => updatePalette(option.id)}
                    sx={{
                      bgcolor: option.swatch,
                      border: "2px solid",
                      borderColor:
                        palette === option.id ? "text.primary" : "transparent",
                      outline: palette === option.id ? "2px solid" : "none",
                      outlineColor: "text.primary",
                      outlineOffset: 2,
                      color: "transparent",
                      "&:hover": { bgcolor: option.swatch, opacity: 0.82 },
                    }}
                  >
                    <PaletteIcon fontSize="inherit" />
                  </IconButton>
                </Tooltip>
              ))}
            </Box>
          </Box>
          <MenuItem onClick={() => void logout()} sx={{ gap: 1.25, py: 1.25 }}>
            <ListItemIcon sx={{ minWidth: 32 }}>
              <LogoutIcon fontSize="small" />
            </ListItemIcon>
            <ListItemText primary="خروج" secondary="پایان نشست کاربری" />
          </MenuItem>
        </Menu>
        <ChangePasswordDialog
          open={passwordDialogOpen}
          busy={passwordBusy}
          error={passwordError}
          onClose={() => {
            setPasswordDialogOpen(false);
            setPasswordError(undefined);
          }}
          onSubmit={async (currentPassword, newPassword) => {
            setPasswordBusy(true);
            setPasswordError(undefined);
            try {
              await changePassword(currentPassword, newPassword);
              setPasswordDialogOpen(false);
            } catch {
              setPasswordError("رمز فعلی درست نیست یا ثبت رمز جدید ممکن نشد.");
            } finally {
              setPasswordBusy(false);
            }
          }}
        />
      </Toolbar>
    </AppBar>
  );
}
