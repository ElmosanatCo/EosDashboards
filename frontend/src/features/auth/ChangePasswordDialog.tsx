import { Visibility, VisibilityOff } from "@mui/icons-material";
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  InputAdornment,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useState } from "react";

type Props = {
  open: boolean;
  busy: boolean;
  error?: string;
  onClose: () => void;
  onSubmit: (currentPassword: string, newPassword: string) => Promise<void>;
};

export function ChangePasswordDialog({
  open,
  busy,
  error,
  onClose,
  onSubmit,
}: Props) {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmation, setConfirmation] = useState("");
  const [showPasswords, setShowPasswords] = useState(false);
  const valid =
    Boolean(currentPassword) &&
    newPassword.length >= 8 &&
    newPassword === confirmation;
  const inputType = showPasswords ? "text" : "password";
  const visibility = (
    <InputAdornment position="end">
      <IconButton
        aria-label={showPasswords ? "پنهان کردن رمزها" : "نمایش رمزها"}
        edge="end"
        onClick={() => setShowPasswords((value) => !value)}
      >
        {showPasswords ? <VisibilityOff /> : <Visibility />}
      </IconButton>
    </InputAdornment>
  );
  return (
    <Dialog
      open={open}
      onClose={busy ? undefined : onClose}
      fullWidth
      maxWidth="xs"
    >
      <DialogTitle>تغییر رمز عبور</DialogTitle>
      <DialogContent>
        <Stack
          component="form"
          spacing={2}
          sx={{ pt: 1 }}
          onSubmit={(event) => {
            event.preventDefault();
            if (valid) void onSubmit(currentPassword, newPassword);
          }}
        >
          <Typography color="text.secondary" variant="body2">
            پس از ثبت رمز جدید، برای حفظ امنیت از سامانه خارج می‌شوید.
          </Typography>
          <TextField
            autoFocus
            autoComplete="current-password"
            disabled={busy}
            fullWidth
            label="رمز فعلی"
            type={inputType}
            value={currentPassword}
            slotProps={{ input: { endAdornment: visibility } }}
            onChange={(event) => setCurrentPassword(event.target.value)}
          />
          <TextField
            autoComplete="new-password"
            disabled={busy}
            fullWidth
            label="رمز جدید"
            type={inputType}
            value={newPassword}
            helperText={
              newPassword && newPassword.length < 8
                ? "رمز عبور باید حداقل ۸ کاراکتر باشد."
                : undefined
            }
            slotProps={{ input: { endAdornment: visibility } }}
            onChange={(event) => setNewPassword(event.target.value)}
          />
          <TextField
            autoComplete="new-password"
            disabled={busy}
            fullWidth
            label="تکرار رمز جدید"
            type={inputType}
            value={confirmation}
            error={Boolean(confirmation) && newPassword !== confirmation}
            helperText={
              confirmation && newPassword !== confirmation
                ? "دو رمز عبور یکسان نیستند."
                : undefined
            }
            onChange={(event) => setConfirmation(event.target.value)}
          />
          {error ? (
            <Typography color="error" variant="body2">
              {error}
            </Typography>
          ) : null}
          <DialogActions sx={{ px: 0 }}>
            <Button type="button" disabled={busy} onClick={onClose}>
              انصراف
            </Button>
            <Button type="submit" variant="contained" disabled={busy || !valid}>
              ثبت رمز جدید
            </Button>
          </DialogActions>
        </Stack>
      </DialogContent>
    </Dialog>
  );
}
