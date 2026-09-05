import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AppThemeProvider } from "../app/providers/AppThemeProvider";
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
        pageHelpTitle="راهنمای صفحه"
        onOpenPageHelp={vi.fn()}
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
        pageHelpTitle="راهنمای صفحه"
        onOpenPageHelp={vi.fn()}
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
        pageHelpTitle="راهنمای صفحه"
        onOpenPageHelp={vi.fn()}
        showBrand={false}
      />,
    );

    expect(screen.queryByRole("img", { name: "EOS" })).not.toBeInTheDocument();
    expect(screen.queryByText("علم و صنعت")).not.toBeInTheDocument();
  });

  it("opens the active-page help from the header beside the user menu", async () => {
    const onOpenPageHelp = vi.fn();
    const user = userEvent.setup();

    render(
      <AppHeader
        onMenu={vi.fn()}
        targets={[]}
        onOpenTarget={vi.fn()}
        pageHelpTitle="راهنمای مدیریت کاربران"
        onOpenPageHelp={onOpenPageHelp}
        showBrand
      />,
    );

    const helpButton = screen.getByRole("button", {
      name: "راهنمای مدیریت کاربران",
    });
    expect(helpButton).toBeInTheDocument();
    await user.click(helpButton);
    expect(onOpenPageHelp).toHaveBeenCalledOnce();
  });

  it("uses the active theme color as a solid header background", () => {
    render(
      <AppThemeProvider>
        <AppHeader
          onMenu={vi.fn()}
          targets={[]}
          onOpenTarget={vi.fn()}
          pageHelpTitle="راهنمای صفحه"
          onOpenPageHelp={vi.fn()}
          showBrand
        />
      </AppThemeProvider>,
    );

    const header = screen.getByRole("banner");
    expect(getComputedStyle(header).backgroundImage).toBe("none");
    expect(getComputedStyle(header).backgroundColor).toBe("rgb(56, 184, 170)");
    expect(getComputedStyle(header).color).toBe("rgb(7, 19, 18)");
    expect(
      getComputedStyle(screen.getByRole("textbox", { name: "جست‌وجوی سراسری" }))
        .color,
    ).toBe(getComputedStyle(header).color);
  });
});
