import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import App from "./App";
import { AppProviders } from "./app/providers/AppProviders";

describe("application", () => {
  it("shows a branded loading state while checking the existing session", () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(
        async () =>
          new Response(JSON.stringify({ title: "unauthorized" }), {
            status: 401,
            headers: { "Content-Type": "application/json" },
          }),
      ),
    );
    render(
      <AppProviders>
        <App />
      </AppProviders>,
    );
    expect(
      screen.getByRole("progressbar", { name: "در حال آماده‌سازی سامانه" }),
    ).toBeInTheDocument();
    expect(screen.getByText("داشبوردهای علم و صنعت")).toBeInTheDocument();
  });
});
