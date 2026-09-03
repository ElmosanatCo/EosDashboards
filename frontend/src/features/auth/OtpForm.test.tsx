import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { OtpForm } from "./OtpForm";

afterEach(cleanup);

describe("OtpForm", () => {
  it("accepts six Persian digits and submits their normalized value", async () => {
    const submit = vi.fn(async () => undefined);
    render(
      <OtpForm
        purpose="signIn"
        maskedMobile="*******6789"
        expiresAt={new Date(Date.now() + 300_000).toISOString()}
        resendAvailableAt={new Date(Date.now() + 60_000).toISOString()}
        busy={false}
        onSubmit={submit}
        onResend={vi.fn(async () => undefined)}
        onBack={() => undefined}
      />,
    );

    await userEvent.type(screen.getByLabelText("کد شش‌رقمی"), "۱۲۳۴۵۶");
    await userEvent.click(screen.getByRole("button", { name: "تأیید کد" }));

    expect(submit).toHaveBeenCalledWith("123456");
  });

  it("renders the masked mobile number left-to-right inside RTL copy", () => {
    render(
      <OtpForm
        purpose="signIn"
        maskedMobile="*******6789"
        expiresAt={new Date(Date.now() + 300_000).toISOString()}
        resendAvailableAt={new Date(Date.now() + 60_000).toISOString()}
        busy={false}
        onSubmit={vi.fn(async () => undefined)}
        onResend={vi.fn(async () => undefined)}
        onBack={() => undefined}
      />,
    );

    expect(screen.getByTestId("masked-mobile")).toHaveAttribute("dir", "ltr");
  });

  it("offers resend after the independent 60-second cooldown", () => {
    render(
      <OtpForm
        purpose="signIn"
        maskedMobile="*******6789"
        expiresAt={new Date(Date.now() + 300_000).toISOString()}
        resendAvailableAt={new Date(Date.now() - 1_000).toISOString()}
        busy={false}
        onSubmit={vi.fn(async () => undefined)}
        onResend={vi.fn(async () => undefined)}
        onBack={() => undefined}
      />,
    );

    expect(screen.getByRole("button", { name: "ارسال مجدد کد" })).toBeEnabled();
    expect(screen.getByText(/کد تا/)).toBeVisible();
  });
});
