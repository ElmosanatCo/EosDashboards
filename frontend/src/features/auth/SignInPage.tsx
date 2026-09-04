import {
  Alert,
  Box,
  Button,
  Divider,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { useEffect, useState } from "react";
import type { Challenge } from "./authTypes";
import { CredentialForm } from "./CredentialForm";
import { GoogleSignInButton } from "./GoogleSignInButton";
import { OtpForm } from "./OtpForm";
import { PasswordRecoveryForm } from "./PasswordRecoveryForm";

const eosLogoUrl = `${import.meta.env.BASE_URL}generated-assets/brand/eos.svg`;
const authBackgroundUrl = `${import.meta.env.BASE_URL}generated-assets/brand/auth-background.jpg`;

export type SignInMode =
  "signIn" | "signInOtp" | "passwordReset" | "passwordResetOtp";

type Props = {
  mode: SignInMode;
  challenge: Challenge | null;
  busy: boolean;
  error?: string;
  notice?: string;
  googleAvailable: boolean;
  onStartSignIn: (username: string, password: string) => Promise<void>;
  onVerifyOtp: (code: string) => Promise<void>;
  onStartPasswordReset: (username: string) => Promise<void>;
  onCompletePasswordReset: (code: string, newPassword: string) => Promise<void>;
  onResendOtp: () => Promise<void>;
  onBack: () => void;
  onStartGoogleSignIn: () => void;
};

export function SignInPage({
  mode,
  challenge,
  busy,
  error,
  notice,
  googleAvailable,
  onStartSignIn,
  onVerifyOtp,
  onStartPasswordReset,
  onCompletePasswordReset,
  onResendOtp,
  onBack,
  onStartGoogleSignIn,
}: Props) {
  const [recoveryOpen, setRecoveryOpen] = useState(mode === "passwordReset");
  useEffect(() => {
    if (mode !== "signIn" && mode !== "passwordReset") setRecoveryOpen(false);
  }, [mode]);

  const recovery = recoveryOpen || mode === "passwordReset";
  const otpPurpose = mode === "passwordResetOtp" ? "passwordReset" : "signIn";
  const isOtp = mode === "signInOtp" || mode === "passwordResetOtp";
  const title = isOtp
    ? otpPurpose === "signIn"
      ? "تأیید ورود"
      : "تنظیم رمز جدید"
    : recovery
      ? "بازیابی رمز عبور"
      : "ورود به سامانه";

  return (
    <Box
      component="main"
      sx={{
        minHeight: "100dvh",
        display: "grid",
        gridTemplateColumns: {
          md: "minmax(360px, .9fr) minmax(440px, 1.1fr)",
        },
      }}
    >
      <Box
        component="aside"
        sx={{
          display: { xs: "none", md: "flex" },
          position: "relative",
          overflow: "hidden",
          bgcolor: "#061821",
          backgroundImage: `linear-gradient(rgba(3, 19, 29, .78), rgba(3, 19, 29, .9)), url('${authBackgroundUrl}')`,
          backgroundPosition: "center",
          backgroundSize: "cover",
          color: "#fff",
          p: { md: 5, lg: 8 },
          alignItems: "center",
        }}
      >
        <Stack spacing={3.5} sx={{ maxWidth: 420, position: "relative" }}>
          <Box
            component="img"
            src={eosLogoUrl}
            alt="EOS"
            sx={{ width: 92, height: 92, display: "block" }}
          />
          <Typography
            component="h1"
            sx={{
              fontSize: { md: "2.2rem", lg: "2.75rem" },
              fontWeight: 750,
              lineHeight: 1.3,
            }}
          >
            داشبوردهای علم و صنعت
          </Typography>
          <Typography
            sx={{
              color: "rgba(255,255,255,.78)",
              fontSize: "1.05rem",
              lineHeight: 2,
            }}
          >
            یک فضای کاری متمرکز برای مشاهده و پیگیری شاخص‌های سازمان.
          </Typography>
        </Stack>
      </Box>
      <Box
        sx={{
          display: "grid",
          placeItems: "center",
          p: { xs: 2, sm: 4, md: 6 },
          bgcolor: "background.default",
        }}
      >
        <Paper
          elevation={0}
          sx={{
            width: "min(100%, 430px)",
            border: "1px solid",
            borderColor: "divider",
            borderRadius: 3,
            px: { xs: 2.5, sm: 4 },
            py: { xs: 3, sm: 4 },
            boxShadow: "0 24px 64px rgba(7,31,57,.10)",
          }}
        >
          <Stack spacing={3}>
            <Stack
              direction="column"
              spacing={1.25}
              sx={{
                display: { xs: "flex", md: "none" },
                alignItems: "flex-start",
                width: "100%",
              }}
            >
              <Box
                component="span"
                data-testid="sign-in-card-brand"
                sx={{
                  width: 48,
                  height: 48,
                  display: "grid",
                  placeItems: "center",
                }}
              >
                <Box
                  component="img"
                  src={eosLogoUrl}
                  alt="EOS"
                  sx={{ width: 48, height: 48, display: "block" }}
                />
              </Box>
              <Typography sx={{ fontWeight: 700 }}>علم و صنعت</Typography>
            </Stack>
            <Box>
              <Typography
                component="h2"
                variant="h4"
                sx={{ fontSize: "1.8rem", fontWeight: 750 }}
              >
                {title}
              </Typography>
              <Typography
                color="text.secondary"
                sx={{ mt: 1, lineHeight: 1.9 }}
              >
                {isOtp
                  ? "برای ادامه، اطلاعات خواسته‌شده را وارد کنید."
                  : recovery
                    ? "دسترسی خود را به‌صورت امن بازیابی کنید."
                    : "با نام کاربری و رمز عبور خود وارد شوید."}
              </Typography>
            </Box>
            {error ? <Alert severity="error">{error}</Alert> : null}
            {notice ? <Alert severity="success">{notice}</Alert> : null}
            {isOtp && challenge ? (
              <OtpForm
                purpose={otpPurpose}
                maskedMobile={challenge.maskedMobile}
                expiresAt={challenge.expiresAt}
                resendAvailableAt={challenge.resendAvailableAt}
                busy={busy}
                error={error}
                onSubmit={(code, newPassword) =>
                  otpPurpose === "signIn"
                    ? onVerifyOtp(code)
                    : onCompletePasswordReset(code, newPassword!)
                }
                onResend={onResendOtp}
                onBack={onBack}
              />
            ) : recovery ? (
              <PasswordRecoveryForm
                busy={busy}
                onSubmit={onStartPasswordReset}
                onBack={() => {
                  setRecoveryOpen(false);
                  onBack();
                }}
              />
            ) : (
              <>
                <CredentialForm busy={busy} onSubmit={onStartSignIn} />
                {googleAvailable ? <Divider>یا</Divider> : null}
                <GoogleSignInButton
                  available={googleAvailable}
                  busy={busy}
                  onStart={onStartGoogleSignIn}
                />
                <Divider />
                <Button
                  type="button"
                  variant="text"
                  onClick={() => setRecoveryOpen(true)}
                  disabled={busy}
                  sx={{
                    alignSelf: "flex-start",
                    mt: 0.75,
                    px: 1.25,
                    py: 0.75,
                    borderRadius: 1.5,
                    "&:hover": { bgcolor: "action.hover" },
                  }}
                >
                  فراموشی رمز
                </Button>
              </>
            )}
          </Stack>
        </Paper>
      </Box>
    </Box>
  );
}
