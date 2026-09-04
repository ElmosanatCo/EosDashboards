import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AppHeader } from "./AppHeader";

afterEach(cleanup);

vi.mock("../app/providers/AuthProvider", () => ({
  useAuth: () => ({
    user: { firstName: "کاربر", lastName: "آزمایشی" },
    logout: vi.fn(async () => undefined),
    changePassword: vi.fn(async () => undefined),
  }),
}));

vi.mock("../app/providers/UserPreferenceProvider", () => ({
  useUserPreferences: () => ({
    palette: "teal",
    resolvedAppearanceMode: "dark",
    toggleAppearance: vi.fn(),
    updatePalette: vi.fn(),
  }),
}));

describe("AppHeader", () => {
  it("keeps the brand in the header for the desktop layout", () => {
    render(
      <AppHeader
        onMenu={vi.fn()}
        targets={[]}
        onOpenTarget={vi.fn()}
        showBrand
      />,
    );

    expect(screen.getByRole("img", { name: "EOS" })).toBeInTheDocument();
    expect(screen.getByText("علم و صنعت")).toBeInTheDocument();
  });

  it("provides hover hints for header icon controls", () => {
    render(
      <AppHeader
        onMenu={vi.fn()}
        targets={[]}
        onOpenTarget={vi.fn()}
        showBrand
      />,
    );

    expect(
      screen.getByRole("button", { name: "باز و بسته کردن منو" }),
    ).toHaveAccessibleName("باز و بسته کردن منو");
    expect(
      screen.getByRole("button", { name: "منوی کاربر" }),
    ).toHaveAccessibleName("منوی کاربر");
  });

  it("hides the brand when the compact mobile layout is active", () => {
    render(
      <AppHeader
        onMenu={vi.fn()}
        targets={[]}
        onOpenTarget={vi.fn()}
        showBrand={false}
      />,
    );

    expect(screen.queryByRole("img", { name: "EOS" })).not.toBeInTheDocument();
    expect(screen.queryByText("علم و صنعت")).not.toBeInTheDocument();
  });
});
