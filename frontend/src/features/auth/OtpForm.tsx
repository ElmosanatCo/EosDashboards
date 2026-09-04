import { ArrowBack, Visibility, VisibilityOff } from "@mui/icons-material";
import {
  Box,
  Button,
  IconButton,
  InputAdornment,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useEffect, useState } from "react";
import { toPersianDigits } from "../../lib/format/persianDigits";

type Props = {
  purpose: "signIn" | "passwordReset";
  maskedMobile?: string;
  expiresAt: string;
  resendAvailableAt: string;
  busy: boolean;
  error?: string;
  onSubmit: (code: string, newPassword?: string) => Promise<void>;
  onResend: () => Promise<void>;
  onBack: () => void;
};

export function OtpForm({
  purpose,
  maskedMobile,
  expiresAt,
  resendAvailableAt,
  busy,
  error,
  onSubmit,
  onResend,
  onBack,
}: Props) {
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [now, setNow] = useState(Date.now());

  useEffect(() => {
    setNow(Date.now());
    const timer = window.setInterval(() => setNow(Date.now()), 1_000);
    return () => window.clearInterval(timer);
  }, [expiresAt, resendAvailableAt]);

  const secondsLeft = secondsUntil(expiresAt, now);
  const resendSecondsLeft = secondsUntil(resendAvailableAt, now);
  const codeExpired = secondsLeft === 0;

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
        {purpose === "signIn" ? (
          <>
            کد ارسال‌شده به{" "}
            <Box
              component="bdi"
              data-testid="masked-mobile"
              dir="ltr"
              sx={{
                display: "inline-block",
                fontVariantNumeric: "tabular-nums",
              }}
            >
              {maskedMobile}
            </Box>{" "}
            را وارد کنید.
          </>
        ) : (
          "کد بازیابی و رمز عبور جدید را وارد کنید."
        )}
      </Typography>
      <TextField
        autoFocus
        label="کد شش‌رقمی"
        value={code}
        disabled={busy || codeExpired}
        error={Boolean(error)}
        helperText={
          error ??
          (codeExpired
            ? "زمان اعتبار کد تمام شده است."
            : "کد تا ۵ دقیقه معتبر است.")
        }
        slotProps={{
          htmlInput: {
            inputMode: "numeric",
            autoComplete: "one-time-code",
            maxLength: 6,
            dir: "ltr",
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
              htmlInput: { dir: "ltr" },
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
            sx={{ "& input": { textAlign: "left" } }}
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
            slotProps={{ htmlInput: { dir: "ltr" } }}
            sx={{ "& input": { textAlign: "left" } }}
          />
        </>
      ) : null}
      <Button
        type="submit"
        variant="contained"
        disabled={
          busy ||
          codeExpired ||
          code.length !== 6 ||
          (purpose === "passwordReset" &&
            (newPassword.length < 8 || newPassword !== confirmPassword))
        }
      >
        {purpose === "signIn" ? "تأیید کد" : "ثبت رمز جدید"}
      </Button>
      <Button
        type="button"
        variant="text"
        onClick={() => void onResend()}
        disabled={busy || codeExpired || resendSecondsLeft > 0}
      >
        {resendSecondsLeft > 0
          ? `ارسال مجدد کد تا ${toPersianDigits(resendSecondsLeft)} ثانیه دیگر`
          : "ارسال مجدد کد"}
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

function secondsUntil(value: string, now = Date.now()) {
  return Math.max(0, Math.ceil((new Date(value).getTime() - now) / 1_000));
}

function normalizeDigits(value: string) {
  return value.replace(/[۰-۹]/g, (digit) =>
    String("۰۱۲۳۴۵۶۷۸۹".indexOf(digit)),
  );
}
