import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CredentialForm } from "./CredentialForm";

afterEach(cleanup);

describe("CredentialForm", () => {
  it("shows credential fields with Persian-number display while submitting unchanged values", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn(async () => undefined);
    render(<CredentialForm busy={false} onSubmit={onSubmit} />);

    const username = screen.getByLabelText("نام کاربری");
    const password = screen.getByLabelText("رمز عبور");
    await user.type(username, "user2");
    await user.type(password, "pass3");
    await user.click(screen.getByRole("button", { name: "نمایش رمز عبور" }));

    expect(username.closest(".eos-persian-number")).not.toBeNull();
    expect(password.closest(".eos-persian-number")).not.toBeNull();
    expect(password).toHaveValue("pass3");

    await user.click(
      screen.getByRole("button", { name: "ورود و دریافت کد تأیید" }),
    );

    expect(onSubmit).toHaveBeenCalledWith("user2", "pass3");
  });
});
