import {
  Button,
  IconButton,
  InputAdornment,
  Stack,
  TextField,
} from "@mui/material";
import { Visibility, VisibilityOff } from "@mui/icons-material";
import { useState } from "react";

type Props = {
  busy: boolean;
  onSubmit: (username: string, password: string) => Promise<void>;
};

export function CredentialForm({ busy, onSubmit }: Props) {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  return (
    <Stack
      component="form"
      spacing={2.25}
      onSubmit={(event) => {
        event.preventDefault();
        if (username.trim() && password) void onSubmit(username, password);
      }}
    >
      <TextField
        autoFocus
        autoComplete="username"
        disabled={busy}
        fullWidth
        label="نام کاربری"
        value={username}
        onChange={(event) => setUsername(event.target.value)}
      />
      <TextField
        autoComplete="current-password"
        disabled={busy}
        fullWidth
        label="رمز عبور"
        type={showPassword ? "text" : "password"}
        value={password}
        slotProps={{
          input: {
            endAdornment: (
              <InputAdornment position="end">
                <IconButton
                  aria-label={
                    showPassword ? "پنهان کردن رمز عبور" : "نمایش رمز عبور"
                  }
                  edge="end"
                  onClick={() => setShowPassword((value) => !value)}
                >
                  {showPassword ? <VisibilityOff /> : <Visibility />}
                </IconButton>
              </InputAdornment>
            ),
          },
        }}
        onChange={(event) => setPassword(event.target.value)}
      />
      <Button
        fullWidth
        size="large"
        type="submit"
        variant="contained"
        disabled={busy || !username.trim() || !password}
      >
        ورود و دریافت کد تأیید
      </Button>
    </Stack>
  );
}
