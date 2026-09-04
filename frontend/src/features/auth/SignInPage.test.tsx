import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { SignInPage } from "./SignInPage";

describe("SignInPage", () => {
  afterEach(cleanup);

  it("uses the approved EOS logo in the sign-in card brand", () => {
    render(
      <SignInPage
        mode="signIn"
        challenge={null}
        busy={false}
        googleAvailable={false}
        onStartSignIn={vi.fn(async () => undefined)}
        onVerifyOtp={vi.fn(async () => undefined)}
        onStartPasswordReset={vi.fn(async () => undefined)}
        onCompletePasswordReset={vi.fn(async () => undefined)}
        onResendOtp={vi.fn(async () => undefined)}
        onBack={vi.fn()}
        onStartGoogleSignIn={vi.fn()}
      />,
    );

    expect(
      screen.getByTestId("sign-in-card-brand").querySelector('img[alt="EOS"]'),
    ).not.toBeNull();
    expect(screen.queryByTestId("compact-sign-in-lock")).toBeNull();
  });

  it("submits username and password before showing OTP", async () => {
    const user = userEvent.setup();
    const onStartSignIn = vi.fn(async () => undefined);
    render(
      <SignInPage
        mode="signIn"
        challenge={null}
        busy={false}
        googleAvailable={false}
        onStartSignIn={onStartSignIn}
        onVerifyOtp={vi.fn(async () => undefined)}
        onStartPasswordReset={vi.fn(async () => undefined)}
        onCompletePasswordReset={vi.fn(async () => undefined)}
        onResendOtp={vi.fn(async () => undefined)}
        onBack={vi.fn()}
        onStartGoogleSignIn={vi.fn()}
      />,
    );

    await user.type(screen.getByLabelText("نام کاربری"), "admin");
    await user.type(screen.getByLabelText("رمز عبور"), "simple pass");
    await user.click(
      screen.getByRole("button", { name: "ورود و دریافت کد تأیید" }),
    );

    expect(onStartSignIn).toHaveBeenCalledWith("admin", "simple pass");
  });

  it("opens recovery without claiming that an account exists", async () => {
    const user = userEvent.setup();
    render(
      <SignInPage
        mode="signIn"
        challenge={null}
        busy={false}
        googleAvailable={false}
        onStartSignIn={vi.fn(async () => undefined)}
        onVerifyOtp={vi.fn(async () => undefined)}
        onStartPasswordReset={vi.fn(async () => undefined)}
        onCompletePasswordReset={vi.fn(async () => undefined)}
        onResendOtp={vi.fn(async () => undefined)}
        onBack={vi.fn()}
        onStartGoogleSignIn={vi.fn()}
      />,
    );

    await user.click(screen.getByRole("button", { name: "فراموشی رمز" }));

    expect(screen.getByText("بازیابی رمز عبور")).toBeVisible();
  });
});
