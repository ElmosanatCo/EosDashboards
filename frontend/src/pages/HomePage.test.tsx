import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { HomePage } from "./HomePage";

describe("HomePage", () => {
  it("presents an operational no-data workspace instead of a generic welcome card", () => {
    render(<HomePage />);

    expect(
      screen.getByRole("heading", { name: "فضای کاری مدیریت" }),
    ).toBeInTheDocument();
    expect(
      screen.getByText("داده‌ای برای نمایش وجود ندارد."),
    ).toBeInTheDocument();
  });
});
