import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import App from "./App";

describe("frontend scaffold", () => {
  it("renders the generated application entry point", () => {
    render(<App />);

    expect(
      screen.getByRole("heading", { name: "Get started" }),
    ).toBeInTheDocument();
  });
});
