import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import type { PropsWithChildren } from "react";
import { authApi } from "../../features/auth/authApi";
import { SignInPage } from "../../features/auth/SignInPage";
import type { AuthenticatedUser, Challenge } from "../../features/auth/authTypes";
import { registerRefreshHandler } from "../../lib/api/apiClient";
import { authTokenStore } from "../../lib/api/authTokenStore";

type AuthContextValue = {
  user: AuthenticatedUser;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children, onLogout }: PropsWithChildren<{ onLogout?: () => void }>) {
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const [challenge, setChallenge] = useState<Challenge | null>(null);
  const [busy, setBusy] = useState(true);
  const [error, setError] = useState<string>();

  const applyAuthentication = useCallback((response: Awaited<ReturnType<typeof authApi.refresh>>) => {
    authTokenStore.set(response.accessToken);
    setUser(response.user);
    setError(undefined);
    return true;
  }, []);

  const refresh = useCallback(async () => {
    try {
      return applyAuthentication(await authApi.refresh());
    } catch {
      authTokenStore.clear();
      setUser(null);
      return false;
    }
  }, [applyAuthentication]);

  useEffect(() => {
    registerRefreshHandler(refresh);
    void refresh().finally(() => setBusy(false));
    return () => registerRefreshHandler(null);
  }, [refresh]);

  const startSignIn = useCallback(async () => {
    setBusy(true);
    setError(undefined);
    try { setChallenge(await authApi.startSignIn()); }
    catch { setError("ورود سازمانی انجام نشد. دوباره تلاش کنید."); }
    finally { setBusy(false); }
  }, []);

  const verifyOtp = useCallback(async (code: string) => {
    if (!challenge) return;
    setBusy(true);
    setError(undefined);
    try { applyAuthentication(await authApi.verifyOtp(challenge.challengeToken, code)); }
    catch { setError("کد واردشده معتبر نیست یا منقضی شده است."); }
    finally { setBusy(false); }
  }, [applyAuthentication, challenge]);

  const logout = useCallback(async () => {
    try { await authApi.logout(); } finally {
      authTokenStore.clear();
      setUser(null);
      setChallenge(null);
      onLogout?.();
    }
  }, [onLogout]);

  const value = useMemo(() => user ? { user, logout } : null, [logout, user]);
  if (busy && !user && !challenge) return <div role="status">در حال بررسی نشست…</div>;
  if (!value) {
    return <SignInPage challenge={challenge} busy={busy} error={error} onStart={startSignIn} onVerify={verifyOtp} onBack={() => setChallenge(null)} />;
  }
  return <AuthContext value={value}>{children}</AuthContext>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error("useAuth must be used within AuthProvider");
  return value;
}
