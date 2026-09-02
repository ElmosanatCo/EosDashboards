import { Button, Stack, TextField, Typography } from "@mui/material";
import { useEffect, useState } from "react";

type Props = {
  maskedMobile: string;
  expiresAtUtc: string;
  busy: boolean;
  error?: string;
  onSubmit: (code: string) => Promise<void>;
  onBack: () => void;
};

export function OtpForm({ maskedMobile, expiresAtUtc, busy, error, onSubmit, onBack }: Props) {
  const [code, setCode] = useState("");
  const [secondsLeft, setSecondsLeft] = useState(() => secondsUntil(expiresAtUtc));

  useEffect(() => {
    const timer = window.setInterval(() => setSecondsLeft(secondsUntil(expiresAtUtc)), 1_000);
    return () => window.clearInterval(timer);
  }, [expiresAtUtc]);

  return (
    <Stack component="form" spacing={2} onSubmit={(event) => {
      event.preventDefault();
      if (code.length === 6) void onSubmit(code);
    }}>
      <Typography>کد ارسال‌شده به {maskedMobile} را وارد کنید.</Typography>
      <TextField
        autoFocus
        label="کد شش‌رقمی"
        value={code}
        disabled={busy || secondsLeft === 0}
        error={Boolean(error)}
        helperText={error ?? `${toPersianDigits(secondsLeft)} ثانیه باقی مانده`}
        slotProps={{ htmlInput: { inputMode: "numeric", autoComplete: "one-time-code", maxLength: 6 } }}
        onChange={(event) => setCode(normalizeDigits(event.target.value).replace(/\D/g, "").slice(0, 6))}
      />
      <Button type="submit" variant="contained" disabled={busy || code.length !== 6}>تأیید کد</Button>
      <Button type="button" onClick={onBack} disabled={busy}>بازگشت</Button>
    </Stack>
  );
}

function secondsUntil(value: string) {
  return Math.max(0, Math.ceil((new Date(value).getTime() - Date.now()) / 1_000));
}

function normalizeDigits(value: string) {
  return value.replace(/[۰-۹]/g, (digit) => String("۰۱۲۳۴۵۶۷۸۹".indexOf(digit)));
}

function toPersianDigits(value: number) {
  return value.toLocaleString("fa-IR", { useGrouping: false });
}
