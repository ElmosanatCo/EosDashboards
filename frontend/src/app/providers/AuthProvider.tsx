import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import type { PropsWithChildren } from "react";
import { Box, CircularProgress, Stack, Typography } from "@mui/material";
import { authApi } from "../../features/auth/authApi";
import { SignInPage } from "../../features/auth/SignInPage";
import type { SignInMode } from "../../features/auth/SignInPage";
import type {
  AuthenticatedUser,
  Challenge,
} from "../../features/auth/authTypes";
import { registerRefreshHandler } from "../../lib/api/apiClient";
import { authTokenStore } from "../../lib/api/authTokenStore";

type AuthContextValue = {
  user: AuthenticatedUser;
  logout: () => Promise<void>;
  changePassword: (
    currentPassword: string,
    newPassword: string,
  ) => Promise<void>;
};
const AuthContext = createContext<AuthContextValue | null>(null);
const eosLogoUrl = `${import.meta.env.BASE_URL}generated-assets/brand/eos.svg`;
let bootstrapRefreshPromise: Promise<
  Awaited<ReturnType<typeof authApi.refresh>>
> | null = null;

function refreshSession() {
  bootstrapRefreshPromise ??= authApi.refresh().finally(() => {
    bootstrapRefreshPromise = null;
  });
  return bootstrapRefreshPromise;
}

function SessionBootstrap() {
  return (
    <Box
      component="main"
      sx={{
        minHeight: "100dvh",
        display: "grid",
        placeItems: "center",
        px: 3,
        bgcolor: "background.default",
      }}
    >
      <Stack spacing={2.5} sx={{ alignItems: "center", textAlign: "center" }}>
        <Box
          component="img"
          src={eosLogoUrl}
          alt="EOS"
          sx={{ width: 76, height: 76, borderRadius: 2, bgcolor: "#fff", p: 1 }}
        />
        <Stack spacing={0.75} sx={{ alignItems: "center" }}>
          <Typography variant="h5" sx={{ fontWeight: 750 }}>
            داشبوردهای علم و صنعت
          </Typography>
          <Typography color="text.secondary">
            در حال آماده‌سازی سامانه…
          </Typography>
        </Stack>
        <CircularProgress
          aria-label="در حال آماده‌سازی سامانه"
          color="secondary"
          size={30}
        />
      </Stack>
    </Box>
  );
}

export function AuthProvider({
  children,
  onLogout,
}: PropsWithChildren<{ onLogout?: () => void }>) {
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const [challenge, setChallenge] = useState<Challenge | null>(null);
  const [mode, setMode] = useState<SignInMode>("signIn");
  const [busy, setBusy] = useState(true);
  const [error, setError] = useState<string>();
  const [notice, setNotice] = useState<string>();
  const [googleAvailable, setGoogleAvailable] = useState(false);
  const applyAuthentication = useCallback(
    (response: Awaited<ReturnType<typeof authApi.refresh>>) => {
      authTokenStore.set(response.accessToken);
      setUser(response.user);
      setError(undefined);
      setNotice(undefined);
      return true;
    },
    [],
  );
  const refresh = useCallback(async () => {
    try {
      return applyAuthentication(await refreshSession());
    } catch {
      authTokenStore.clear();
      setUser(null);
      return false;
    }
  }, [applyAuthentication]);
  useEffect(() => {
    registerRefreshHandler(refresh);
    let mounted = true;
    void authApi
      .getSignInProviders()
      .then((providers) => {
        if (mounted) setGoogleAvailable(providers.google);
      })
      .catch(() => {
        if (mounted) setGoogleAvailable(false);
      });
    void refresh().finally(() => setBusy(false));
    return () => {
      mounted = false;
      registerRefreshHandler(null);
    };
  }, [refresh]);
  useEffect(() => {
    const url = new URL(window.location.href);
    if (url.searchParams.get("authError") !== "google") return;

    setError(
      "ورود با Google انجام نشد. دوباره تلاش کنید یا با رمز عبور وارد شوید.",
    );
    url.searchParams.delete("authError");
    window.history.replaceState(
      null,
      "",
      `${url.pathname}${url.search}${url.hash}`,
    );
  }, []);
  const startSignIn = useCallback(
    async (username: string, password: string) => {
      setBusy(true);
      setError(undefined);
      setNotice(undefined);
      try {
        setChallenge(await authApi.startSignIn(username, password));
        setMode("signInOtp");
      } catch {
        setError(
          "نام کاربری یا رمز عبور معتبر نیست، یا ارسال کد فعلاً ممکن نیست.",
        );
      } finally {
        setBusy(false);
      }
    },
    [],
  );
  const verifyOtp = useCallback(
    async (code: string) => {
      if (!challenge) return;
      setBusy(true);
      setError(undefined);
      try {
        applyAuthentication(
          await authApi.verifyOtp(challenge.challengeToken, code),
        );
      } catch {
        setError("کد واردشده معتبر نیست یا منقضی شده است.");
      } finally {
        setBusy(false);
      }
    },
    [applyAuthentication, challenge],
  );
  const resendOtp = useCallback(async () => {
    if (!challenge) return;
    setBusy(true);
    setError(undefined);
    try {
      setChallenge(
        mode === "passwordResetOtp"
          ? await authApi.resendPasswordResetOtp(challenge.challengeToken)
          : await authApi.resendSignInOtp(challenge.challengeToken),
      );
      setNotice("کد جدید ارسال شد.");
    } catch {
      setError("ارسال مجدد کد فعلاً ممکن نیست. دوباره تلاش کنید.");
    } finally {
      setBusy(false);
    }
  }, [challenge, mode]);
  const startPasswordReset = useCallback(async (username: string) => {
    setBusy(true);
    setError(undefined);
    setNotice(undefined);
    try {
      setChallenge(await authApi.startPasswordReset(username));
      setMode("passwordResetOtp");
    } catch {
      setError("دریافت کد بازیابی فعلاً ممکن نیست. دوباره تلاش کنید.");
    } finally {
      setBusy(false);
    }
  }, []);
  const completePasswordReset = useCallback(
    async (code: string, newPassword: string) => {
      if (!challenge) return;
      setBusy(true);
      setError(undefined);
      try {
        await authApi.completePasswordReset(
          challenge.challengeToken,
          code,
          newPassword,
        );
        setChallenge(null);
        setMode("signIn");
        setNotice("رمز عبور جدید ثبت شد. اکنون وارد شوید.");
      } catch {
        setError("کد بازیابی معتبر نیست یا منقضی شده است.");
      } finally {
        setBusy(false);
      }
    },
    [challenge],
  );
  const back = useCallback(() => {
    setChallenge(null);
    setMode("signIn");
    setError(undefined);
  }, []);
  const startGoogleSignIn = useCallback(() => {
    window.location.assign(
      `${import.meta.env.VITE_API_BASE_URL ?? ""}/api/v1/auth/google/start`,
    );
  }, []);
  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } finally {
      authTokenStore.clear();
      setUser(null);
      setChallenge(null);
      setMode("signIn");
      onLogout?.();
    }
  }, [onLogout]);
  const changePassword = useCallback(
    async (currentPassword: string, newPassword: string) => {
      await authApi.changePassword(currentPassword, newPassword);
      authTokenStore.clear();
      setUser(null);
      setChallenge(null);
      setMode("signIn");
      onLogout?.();
    },
    [onLogout],
  );
  const value = useMemo(
    () => (user ? { user, logout, changePassword } : null),
    [changePassword, logout, user],
  );
  if (busy && !user && !challenge) return <SessionBootstrap />;
  if (!value)
    return (
      <SignInPage
        mode={mode}
        challenge={challenge}
        busy={busy}
        error={error}
        notice={notice}
        googleAvailable={googleAvailable}
        onStartSignIn={startSignIn}
        onVerifyOtp={verifyOtp}
        onStartPasswordReset={startPasswordReset}
        onCompletePasswordReset={completePasswordReset}
        onResendOtp={resendOtp}
        onBack={back}
        onStartGoogleSignIn={startGoogleSignIn}
      />
    );
  return <AuthContext value={value}>{children}</AuthContext>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error("useAuth must be used within AuthProvider");
  return value;
}
