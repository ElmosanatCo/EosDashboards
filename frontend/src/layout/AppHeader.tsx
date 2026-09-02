import {
  AppBar,
  Box,
  IconButton,
  Menu,
  MenuItem,
  Stack,
  Toolbar,
  Typography,
} from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import LogoutIcon from "@mui/icons-material/Logout";
import DarkModeIcon from "@mui/icons-material/DarkMode";
import LightModeIcon from "@mui/icons-material/LightMode";
import AccountCircleIcon from "@mui/icons-material/AccountCircle";
import PasswordIcon from "@mui/icons-material/Password";
import { useState } from "react";
import { useAuth } from "../app/providers/AuthProvider";
import { ChangePasswordDialog } from "../features/auth/ChangePasswordDialog";
import { useUserPreferences } from "../app/providers/UserPreferenceProvider";

const eosLogoUrl = `${import.meta.env.BASE_URL}generated-assets/brand/eos.svg`;

export function AppHeader({ onMenu }: { onMenu: () => void }) {
  const { user, logout, changePassword } = useAuth();
  const { appearanceMode, updateAppearance } = useUserPreferences();
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
          onClick={() =>
            updateAppearance(appearanceMode === "dark" ? "light" : "dark")
          }
        >
          {appearanceMode === "dark" ? <LightModeIcon /> : <DarkModeIcon />}
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
