import { LockOutlined } from "@mui/icons-material";
import {
  Alert,
  Box,
  Button,
  Divider,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { ThemeProvider } from "@mui/material/styles";
import { useEffect, useMemo, useState } from "react";
import { createAppTheme } from "../../theme/createAppTheme";
import type { Challenge } from "./authTypes";
import { CredentialForm } from "./CredentialForm";
import { OtpForm } from "./OtpForm";
import { PasswordRecoveryForm } from "./PasswordRecoveryForm";
import { GoogleSignInButton } from "./GoogleSignInButton";

const eosLogoUrl = `${import.meta.env.BASE_URL}generated-assets/brand/eos.svg`;

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
  onBack,
  onStartGoogleSignIn,
}: Props) {
  const loginTheme = useMemo(() => createAppTheme("light"), []);
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
    <ThemeProvider theme={loginTheme}>
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
            bgcolor: "primary.dark",
            color: "primary.contrastText",
            p: { md: 5, lg: 8 },
            alignItems: "center",
          }}
        >
          <Box
            sx={{
              position: "absolute",
              width: 460,
              height: 460,
              border: "1px solid rgba(78, 186, 170, .38)",
              borderRadius: "50%",
              left: -230,
              bottom: -170,
            }}
          />
          <Box
            sx={{
              position: "absolute",
              width: 330,
              height: 330,
              border: "44px solid rgba(0, 137, 123, .18)",
              transform: "rotate(28deg)",
              top: -160,
              right: -110,
            }}
          />
          <Stack spacing={3.5} sx={{ maxWidth: 420, position: "relative" }}>
            <Box
              sx={{
                bgcolor: "#fff",
                borderRadius: 2.5,
                p: 1.5,
                width: "fit-content",
                lineHeight: 0,
              }}
            >
              <Box
                component="img"
                src={eosLogoUrl}
                alt="EOS"
                sx={{ width: 92, height: 92 }}
              />
            </Box>
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
                direction="row"
                spacing={1.25}
                sx={{ display: { md: "none" }, alignItems: "center" }}
              >
                <Box
                  sx={{
                    bgcolor: "primary.dark",
                    color: "#fff",
                    width: 36,
                    height: 36,
                    borderRadius: 1.5,
                    display: "grid",
                    placeItems: "center",
                  }}
                >
                  <LockOutlined fontSize="small" />
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
                  expiresAtUtc={challenge.expiresAtUtc}
                  busy={busy}
                  error={error}
                  onSubmit={(code, newPassword) =>
                    otpPurpose === "signIn"
                      ? onVerifyOtp(code)
                      : onCompletePasswordReset(code, newPassword!)
                  }
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
                  <Button
                    type="button"
                    variant="text"
                    onClick={() => setRecoveryOpen(true)}
                    disabled={busy}
                    sx={{ alignSelf: "flex-start", px: 0 }}
                  >
                    فراموشی رمز
                  </Button>
                </>
              )}
            </Stack>
          </Paper>
        </Box>
      </Box>
    </ThemeProvider>
  );
}
