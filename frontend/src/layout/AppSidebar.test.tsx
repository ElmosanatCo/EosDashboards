import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AppThemeProvider } from "../app/providers/AppThemeProvider";
import { AppSidebar } from "./AppSidebar";

describe("AppSidebar", () => {
  afterEach(cleanup);

  it("shows the company brand at the top of the mobile menu", () => {
    render(
      <AppSidebar
        open
        onClose={vi.fn()}
        temporary
        activeRouteId="home"
        targets={[]}
        onOpenTarget={vi.fn()}
        onActivateHome={vi.fn()}
        showBrand
      />,
    );

    expect(screen.getByRole("img", { name: "EOS" })).toBeInTheDocument();
    expect(screen.getByText("علم و صنعت")).toBeInTheDocument();
  });

  it("places the mobile close control in the empty upper-left corner", () => {
    render(
      <AppSidebar
        open
        onClose={vi.fn()}
        temporary
        activeRouteId="home"
        targets={[]}
        onOpenTarget={vi.fn()}
        onActivateHome={vi.fn()}
        showBrand
      />,
    );

    const closeSlot = screen.getByTestId("mobile-sidebar-close-slot");
    expect(closeSlot).toHaveStyle({
      position: "absolute",
      left: "8px",
      top: "8px",
    });
    expect(closeSlot.closest("nav")).toBeNull();
  });

  it("marks the active navigation item with a theme accent and right-edge indicator", () => {
    render(
      <AppThemeProvider>
        <AppSidebar
          open
          onClose={vi.fn()}
          temporary={false}
          activeRouteId="home"
          targets={[]}
          onOpenTarget={vi.fn()}
          onActivateHome={vi.fn()}
          showBrand={false}
        />
      </AppThemeProvider>,
    );

    const homeButton = screen.getByRole("button", { name: "خانه" });
    expect(homeButton).toHaveClass("Mui-selected");
    expect(getComputedStyle(homeButton).backgroundImage).toContain(
      "linear-gradient(90deg",
    );
    expect(screen.getByTestId("sidebar-active-indicator")).toBeInTheDocument();
  });
});
