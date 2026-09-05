import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AppSidebar } from "./AppSidebar";

describe("AppSidebar", () => {
  afterEach(cleanup);

  it("shows the company brand at the top of the mobile menu", () => {
    render(
      <AppSidebar
        open
        onClose={vi.fn()}
        temporary
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
});
