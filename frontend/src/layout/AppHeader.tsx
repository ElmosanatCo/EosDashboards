import { AppBar, Box, IconButton, Stack, Toolbar, Typography } from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import LogoutIcon from "@mui/icons-material/Logout";
import { useAuth } from "../app/providers/AuthProvider";

export function AppHeader({ onMenu }: { onMenu: () => void }) {
  const { user, logout } = useAuth();
  return (
    <AppBar position="static" component="header" elevation={1}>
      <Toolbar>
        <IconButton color="inherit" edge="start" aria-label="باز و بسته کردن منو" onClick={onMenu}><MenuIcon /></IconButton>
        <Stack direction="row" spacing={1} sx={{ flexGrow: 1, alignItems: "center" }}>
          <Box component="img" src="/generated-assets/brand/eos.svg" alt="EOS" sx={{ width: 38, height: 38 }}
            onError={(event) => { event.currentTarget.style.display = "none"; }} />
          <Typography variant="h6">علم و صنعت</Typography>
        </Stack>
        <Typography variant="body2" sx={{ display: { xs: "none", sm: "block" }, mx: 2 }}>
          {user.firstName} {user.lastName}
        </Typography>
        <IconButton color="inherit" aria-label="خروج" onClick={() => void logout()}><LogoutIcon /></IconButton>
      </Toolbar>
    </AppBar>
  );
}
