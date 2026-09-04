import { ArrowBack } from "@mui/icons-material";
import { Button, Stack, TextField, Typography } from "@mui/material";
import { useState } from "react";

type Props = {
  busy: boolean;
  onSubmit: (username: string) => Promise<void>;
  onBack: () => void;
};

export function PasswordRecoveryForm({ busy, onSubmit, onBack }: Props) {
  const [username, setUsername] = useState("");
  return (
    <Stack
      component="form"
      spacing={2.25}
      onSubmit={(event) => {
        event.preventDefault();
        if (username.trim()) void onSubmit(username);
      }}
    >
      <Typography color="text.secondary" variant="body2">
        نام کاربری خود را وارد کنید. اگر حساب فعال داشته باشید، کد بازیابی ارسال
        می‌شود.
      </Typography>
      <TextField
        autoFocus
        autoComplete="username"
        className="eos-persian-number"
        disabled={busy}
        fullWidth
        label="نام کاربری"
        value={username}
        onChange={(event) => setUsername(event.target.value)}
      />
      <Button
        fullWidth
        size="large"
        type="submit"
        variant="contained"
        disabled={busy || !username.trim()}
      >
        دریافت کد بازیابی
      </Button>
      <Button
        startIcon={<ArrowBack />}
        type="button"
        variant="text"
        onClick={onBack}
        disabled={busy}
      >
        بازگشت به ورود
      </Button>
    </Stack>
  );
}
