import { apiFetch, readCookie } from "../../lib/api/apiClient";
import type { AuthResponse, Challenge } from "./authTypes";

const csrfHeaders = () => ({ "X-CSRF-TOKEN": readCookie("Eos.Antiforgery") });

export const authApi = {
  startSignIn: () => apiFetch<Challenge>("/api/v1/auth/challenges", { method: "POST" }, false),
  verifyOtp: (challengeToken: string, code: string) => apiFetch<AuthResponse>(
    `/api/v1/auth/challenges/${encodeURIComponent(challengeToken)}/verify`,
    { method: "POST", body: JSON.stringify({ code }) },
    false,
  ),
  refresh: () => apiFetch<AuthResponse>(
    "/api/v1/auth/refresh",
    { method: "POST", headers: csrfHeaders() },
    false,
  ),
  logout: () => apiFetch<void>(
    "/api/v1/auth/logout",
    { method: "POST", headers: csrfHeaders() },
    false,
  ),
};
