import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ChangePasswordDialog } from "./ChangePasswordDialog";

describe("ChangePasswordDialog", () => {
  it("keeps password input text left-to-right in the RTL dialog", () => {
    render(
      <ChangePasswordDialog
        open
        busy={false}
        onClose={() => undefined}
        onSubmit={vi.fn(async () => undefined)}
      />,
    );

    expect(screen.getByLabelText("رمز فعلی")).toHaveAttribute("dir", "ltr");
    expect(screen.getByLabelText("رمز جدید")).toHaveAttribute("dir", "ltr");
    expect(screen.getByLabelText("تکرار رمز جدید")).toHaveAttribute(
      "dir",
      "ltr",
    );
  });

  it("cannot be dismissed when a temporary password must be changed", () => {
    render(
      <ChangePasswordDialog
        open
        required
        busy={false}
        onClose={() => undefined}
        onSubmit={vi.fn(async () => undefined)}
      />,
    );

    expect(
      screen.queryByRole("button", { name: "انصراف" }),
    ).not.toBeInTheDocument();
  });
});
