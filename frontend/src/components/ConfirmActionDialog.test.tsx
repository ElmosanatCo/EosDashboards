import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ConfirmActionDialog } from "./ConfirmActionDialog";

afterEach(cleanup);

describe("ConfirmActionDialog", () => {
  it("only invokes the destructive action after explicit confirmation", async () => {
    const user = userEvent.setup();
    const onConfirm = vi.fn();
    const onClose = vi.fn();

    render(
      <ConfirmActionDialog
        open
        title="تأیید حذف"
        message="آیا مطمئن هستید؟"
        onClose={onClose}
        onConfirm={onConfirm}
      />,
    );

    await user.click(screen.getByRole("button", { name: "انصراف" }));
    expect(onConfirm).not.toHaveBeenCalled();
    expect(onClose).toHaveBeenCalledOnce();

    await user.click(screen.getByRole("button", { name: "تأیید حذف" }));
    expect(onConfirm).toHaveBeenCalledOnce();
  });
});
