import {
  AppBar,
  Box,
  IconButton,
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
import PasswordIcon from "@mui/icons-material/Password";
import PaletteIcon from "@mui/icons-material/Palette";
import { useState } from "react";
import { useAuth } from "../app/providers/AuthProvider";
import { ChangePasswordDialog } from "../features/auth/ChangePasswordDialog";
import { useUserPreferences } from "../app/providers/UserPreferenceProvider";
import { paletteOptions } from "../theme/palettes";

const eosLogoUrl = `${import.meta.env.BASE_URL}generated-assets/brand/eos.svg`;

export function AppHeader({ onMenu }: { onMenu: () => void }) {
  const { user, logout, changePassword } = useAuth();
  const { palette, resolvedAppearanceMode, toggleAppearance, updatePalette } =
    useUserPreferences();
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const [passwordDialogOpen, setPasswordDialogOpen] = useState(false);
  const [passwordBusy, setPasswordBusy] = useState(false);
  const [passwordError, setPasswordError] = useState<string>();
  return (
    <AppBar position="static" component="header" elevation={1}>
      <Toolbar>
        <IconButton
          color="inherit"
          edge="start"
          aria-label="باز و بسته کردن منو"
          onClick={onMenu}
        >
          <MenuIcon />
        </IconButton>
        <Stack
          direction="row"
          spacing={1}
          sx={{ flexGrow: 1, alignItems: "center" }}
        >
          <Box
            component="img"
            src={eosLogoUrl}
            alt="EOS"
            sx={{ width: 38, height: 38 }}
            onError={(event) => {
              event.currentTarget.style.display = "none";
            }}
          />
          <Typography variant="h6">علم و صنعت</Typography>
        </Stack>
        <IconButton
          color="inherit"
          aria-label="منوی کاربر"
          onClick={(event) => setAnchorEl(event.currentTarget)}
        >
          <AccountCircleIcon />
        </IconButton>
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
        <Menu
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={() => setAnchorEl(null)}
        >
          <MenuItem disabled>
            {user.firstName} {user.lastName}
          </MenuItem>
          <MenuItem
            onClick={() => {
              setAnchorEl(null);
              setPasswordDialogOpen(true);
            }}
          >
            <PasswordIcon fontSize="small" sx={{ ml: 1 }} />
            تغییر رمز
          </MenuItem>
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
                      boxShadow:
                        palette === option.id
                          ? "0 0 0 2px rgba(255,255,255,.78)"
                          : "none",
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
          <MenuItem onClick={() => void logout()}>
            <LogoutIcon fontSize="small" sx={{ ml: 1 }} />
            خروج
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
