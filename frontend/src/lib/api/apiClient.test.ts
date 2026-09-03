import { afterEach, describe, expect, it, vi } from "vitest";
import { apiFetch, registerRefreshHandler } from "./apiClient";
import { authTokenStore } from "./authTokenStore";

describe("api client", () => {
  afterEach(() => {
    authTokenStore.clear();
    registerRefreshHandler(null);
    vi.restoreAllMocks();
  });

  it("keeps the token in memory and serializes concurrent refresh", async () => {
    localStorage.clear();
    sessionStorage.clear();
    authTokenStore.set("access-token");
    let calls = 0;
    vi.stubGlobal(
      "fetch",
      vi.fn(async (_input, init) => {
        calls += 1;
        expect(new Headers(init?.headers).get("Authorization")).toBe(
          "Bearer access-token",
        );
        return calls <= 2
          ? new Response("{}", {
              status: 401,
              headers: { "Content-Type": "application/json" },
            })
          : new Response(JSON.stringify({ ok: true }), {
              status: 200,
              headers: { "Content-Type": "application/json" },
            });
      }),
    );
    const refresh = vi.fn(async () => true);
    registerRefreshHandler(refresh);

    await Promise.all([apiFetch("/one"), apiFetch("/two")]);

    expect(refresh).toHaveBeenCalledTimes(1);
    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });
});
