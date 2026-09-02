import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { expect, it, vi } from "vitest";
import { ChangePasswordDialog } from "./ChangePasswordDialog";

it("requires matching 8-character new password before submission", async () => {
  const user = userEvent.setup();
  const submit = vi.fn(async () => undefined);
  render(
    <ChangePasswordDialog
      open
      busy={false}
      onClose={vi.fn()}
      onSubmit={submit}
    />,
  );

  await user.type(screen.getByLabelText("رمز فعلی"), "old pass");
  await user.type(screen.getByLabelText("رمز جدید"), "new pass");
  await user.type(screen.getByLabelText("تکرار رمز جدید"), "different");

  expect(screen.getByRole("button", { name: "ثبت رمز جدید" })).toBeDisabled();
});
