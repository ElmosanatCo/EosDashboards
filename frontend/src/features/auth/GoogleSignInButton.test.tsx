import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { GoogleSignInButton } from "./GoogleSignInButton";

describe("GoogleSignInButton", () => {
  afterEach(cleanup);

  it("starts the redirect through an accessible, full-width action", async () => {
    const user = userEvent.setup();
    const onStart = vi.fn();
    render(<GoogleSignInButton available busy={false} onStart={onStart} />);

    await user.click(screen.getByRole("button", { name: "ورود با Google" }));

    expect(onStart).toHaveBeenCalledOnce();
    expect(screen.getByTestId("google-brand-g")).toBeInTheDocument();
  });

  it("does not render when Google sign-in is unavailable", () => {
    render(
      <GoogleSignInButton available={false} busy={false} onStart={vi.fn()} />,
    );

    expect(screen.queryByRole("button", { name: "ورود با Google" })).toBeNull();
  });
});
