import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { expect, it, vi } from "vitest";
import { PasswordRecoveryForm } from "./PasswordRecoveryForm";

it("submits only the username when recovery begins", async () => {
  const user = userEvent.setup();
  const onSubmit = vi.fn(async () => undefined);
  render(
    <PasswordRecoveryForm busy={false} onSubmit={onSubmit} onBack={vi.fn()} />,
  );

  await user.type(screen.getByLabelText("نام کاربری"), "admin");
  await user.click(screen.getByRole("button", { name: "دریافت کد بازیابی" }));

  expect(onSubmit).toHaveBeenCalledWith("admin");
});
