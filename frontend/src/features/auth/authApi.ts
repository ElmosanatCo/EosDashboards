import { apiFetch, readCookie } from "../../lib/api/apiClient";
import type { AuthResponse, Challenge, SignInProviders } from "./authTypes";

const csrfHeaders = () => ({ "X-CSRF-TOKEN": readCookie("Eos.Antiforgery") });

export const authApi = {
  getSignInProviders: () =>
    apiFetch<SignInProviders>("/api/v1/auth/providers", { method: "GET" }, false),
  startSignIn: (username: string, password: string) =>
    apiFetch<Challenge>(
      "/api/v1/auth/sign-in/challenges",
      { method: "POST", body: JSON.stringify({ username, password }) },
      false,
    ),
  verifyOtp: (challengeToken: string, code: string) =>
    apiFetch<AuthResponse>(
      `/api/v1/auth/sign-in/challenges/${encodeURIComponent(challengeToken)}/verify`,
      { method: "POST", body: JSON.stringify({ code }) },
      false,
    ),
  startPasswordReset: (username: string) =>
    apiFetch<Challenge>(
      "/api/v1/auth/password-reset/challenges",
      { method: "POST", body: JSON.stringify({ username }) },
      false,
    ),
  completePasswordReset: (
    challengeToken: string,
    code: string,
    newPassword: string,
  ) =>
    apiFetch<void>(
      `/api/v1/auth/password-reset/challenges/${encodeURIComponent(challengeToken)}/complete`,
      { method: "POST", body: JSON.stringify({ code, newPassword }) },
      false,
    ),
  changePassword: (currentPassword: string, newPassword: string) =>
    apiFetch<void>("/api/v1/auth/password", {
      method: "POST",
      body: JSON.stringify({ currentPassword, newPassword }),
    }),
  refresh: () =>
    apiFetch<AuthResponse>(
      "/api/v1/auth/refresh",
      { method: "POST", headers: csrfHeaders() },
      false,
    ),
  logout: () =>
    apiFetch<void>(
      "/api/v1/auth/logout",
      { method: "POST", headers: csrfHeaders() },
      false,
    ),
};
