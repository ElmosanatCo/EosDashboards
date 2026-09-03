import { ArrowBack, Visibility, VisibilityOff } from "@mui/icons-material";
import {
  Button,
  IconButton,
  InputAdornment,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useEffect, useState } from "react";

type Props = {
  purpose: "signIn" | "passwordReset";
  maskedMobile?: string;
  expiresAtUtc: string;
  busy: boolean;
  error?: string;
  onSubmit: (code: string, newPassword?: string) => Promise<void>;
  onBack: () => void;
};

export function OtpForm({
  purpose,
  maskedMobile,
  expiresAtUtc,
  busy,
  error,
  onSubmit,
  onBack,
}: Props) {
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [secondsLeft, setSecondsLeft] = useState(() =>
    secondsUntil(expiresAtUtc),
  );

  useEffect(() => {
    const timer = window.setInterval(
      () => setSecondsLeft(secondsUntil(expiresAtUtc)),
      1_000,
    );
    return () => window.clearInterval(timer);
  }, [expiresAtUtc]);

  return (
    <Stack
      component="form"
      spacing={2}
      onSubmit={(event) => {
        event.preventDefault();
        if (
          code.length === 6 &&
          (purpose === "signIn" ||
            (newPassword.length >= 8 && newPassword === confirmPassword))
        )
          if (purpose === "passwordReset") void onSubmit(code, newPassword);
          else void onSubmit(code);
      }}
    >
      <Typography color="text.secondary" variant="body2">
        {purpose === "signIn"
          ? `کد ارسال‌شده به ${maskedMobile} را وارد کنید.`
          : "کد بازیابی و رمز عبور جدید را وارد کنید."}
      </Typography>
      <TextField
        autoFocus
        label="کد شش‌رقمی"
        value={code}
        disabled={busy || secondsLeft === 0}
        error={Boolean(error)}
        helperText={error ?? `${toPersianDigits(secondsLeft)} ثانیه باقی مانده`}
        slotProps={{
          htmlInput: {
            inputMode: "numeric",
            autoComplete: "one-time-code",
            maxLength: 6,
          },
        }}
        onChange={(event) =>
          setCode(
            normalizeDigits(event.target.value).replace(/\D/g, "").slice(0, 6),
          )
        }
      />
      {purpose === "passwordReset" ? (
        <>
          <TextField
            autoComplete="new-password"
            label="رمز عبور جدید"
            type={showPassword ? "text" : "password"}
            value={newPassword}
            disabled={busy}
            helperText={
              newPassword && newPassword.length < 8
                ? "رمز عبور باید حداقل ۸ کاراکتر باشد."
                : undefined
            }
            slotProps={{
              input: {
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton
                      aria-label={
                        showPassword ? "پنهان کردن رمز جدید" : "نمایش رمز جدید"
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
            onChange={(event) => setNewPassword(event.target.value)}
          />
          <TextField
            autoComplete="new-password"
            label="تکرار رمز عبور جدید"
            type={showPassword ? "text" : "password"}
            value={confirmPassword}
            disabled={busy}
            error={Boolean(confirmPassword) && newPassword !== confirmPassword}
            helperText={
              confirmPassword && newPassword !== confirmPassword
                ? "دو رمز عبور یکسان نیستند."
                : undefined
            }
            onChange={(event) => setConfirmPassword(event.target.value)}
          />
        </>
      ) : null}
      <Button
        type="submit"
        variant="contained"
        disabled={
          busy ||
          code.length !== 6 ||
          (purpose === "passwordReset" &&
            (newPassword.length < 8 || newPassword !== confirmPassword))
        }
      >
        {purpose === "signIn" ? "تأیید کد" : "ثبت رمز جدید"}
      </Button>
      <Button
        startIcon={<ArrowBack />}
        type="button"
        onClick={onBack}
        disabled={busy}
      >
        بازگشت
      </Button>
    </Stack>
  );
}

function secondsUntil(value: string) {
  return Math.max(
    0,
    Math.ceil((new Date(value).getTime() - Date.now()) / 1_000),
  );
}

function normalizeDigits(value: string) {
  return value.replace(/[۰-۹]/g, (digit) =>
    String("۰۱۲۳۴۵۶۷۸۹".indexOf(digit)),
  );
}

function toPersianDigits(value: number) {
  return value.toLocaleString("fa-IR", { useGrouping: false });
}
