import { authTokenStore } from "./authTokenStore";

export type ApiProblem = Error & { status: number; code?: string; traceId?: string };

let refreshHandler: (() => Promise<boolean>) | null = null;
let refreshPromise: Promise<boolean> | null = null;

export function registerRefreshHandler(handler: (() => Promise<boolean>) | null) {
  refreshHandler = handler;
}

async function parseResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    return response.status === 204 ? (undefined as T) : response.json() as Promise<T>;
  }
  const body = await response.json().catch(() => ({})) as Record<string, unknown>;
  const problem = new Error(typeof body.title === "string" ? body.title : "درخواست انجام نشد") as ApiProblem;
  problem.status = response.status;
  problem.code = typeof body.code === "string" ? body.code : undefined;
  problem.traceId = typeof body.traceId === "string" ? body.traceId : undefined;
  throw problem;
}

export async function apiFetch<T>(
  path: string,
  init: RequestInit = {},
  retryAfterRefresh = true,
): Promise<T> {
  const headers = new Headers(init.headers);
  const token = authTokenStore.get();
  if (token) headers.set("Authorization", `Bearer ${token}`);
  if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");

  const response = await fetch(`${import.meta.env.VITE_API_BASE_URL ?? ""}${path}`, {
    ...init,
    headers,
    credentials: "include",
  });
  if (response.status === 401 && retryAfterRefresh && refreshHandler) {
    refreshPromise ??= refreshHandler().finally(() => { refreshPromise = null; });
    if (await refreshPromise) return apiFetch<T>(path, init, false);
  }
  return parseResponse<T>(response);
}

export function readCookie(name: string) {
  const prefix = `${encodeURIComponent(name)}=`;
  const value = document.cookie.split("; ").find((part) => part.startsWith(prefix));
  return value ? decodeURIComponent(value.slice(prefix.length)) : "";
}
