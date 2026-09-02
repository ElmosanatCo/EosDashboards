import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { OtpForm } from "./OtpForm";

describe("OtpForm", () => {
  it("accepts six Persian digits and submits their normalized value", async () => {
    const submit = vi.fn(async () => undefined);
    render(
      <OtpForm
        maskedMobile="*******6789"
        expiresAtUtc={new Date(Date.now() + 300_000).toISOString()}
        busy={false}
        onSubmit={submit}
        onBack={() => undefined}
      />,
    );

    await userEvent.type(screen.getByLabelText("کد شش‌رقمی"), "۱۲۳۴۵۶");
    await userEvent.click(screen.getByRole("button", { name: "تأیید کد" }));

    expect(submit).toHaveBeenCalledWith("123456");
  });
});
