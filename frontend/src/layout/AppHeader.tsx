import {
  AppBar,
  Box,
  IconButton,
  Stack,
  Toolbar,
  Typography,
} from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import LogoutIcon from "@mui/icons-material/Logout";
import DarkModeIcon from "@mui/icons-material/DarkMode";
import LightModeIcon from "@mui/icons-material/LightMode";
import { useAuth } from "../app/providers/AuthProvider";
import { useUserPreferences } from "../app/providers/UserPreferenceProvider";

const eosLogoUrl = `${import.meta.env.BASE_URL}generated-assets/brand/eos.svg`;

export function AppHeader({ onMenu }: { onMenu: () => void }) {
  const { user, logout } = useAuth();
  const { appearanceMode, updateAppearance } = useUserPreferences();
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
        <Typography
          variant="body2"
          sx={{ display: { xs: "none", sm: "block" }, mx: 2 }}
        >
          {user.firstName} {user.lastName}
        </Typography>
        <IconButton
          color="inherit"
          aria-label="تغییر حالت نمایش"
          onClick={() =>
            updateAppearance(appearanceMode === "dark" ? "light" : "dark")
          }
        >
          {appearanceMode === "dark" ? <LightModeIcon /> : <DarkModeIcon />}
        </IconButton>
        <IconButton
          color="inherit"
          aria-label="خروج"
          onClick={() => void logout()}
        >
          <LogoutIcon />
        </IconButton>
      </Toolbar>
    </AppBar>
  );
}
