import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { AppThemeProvider } from "../app/providers/AppThemeProvider";
import { StatusBar } from "./StatusBar";

describe("StatusBar", () => {
  it("renders Persian-calendar date and time in separate labelled values", () => {
    render(
      <AppThemeProvider>
        <StatusBar />
      </AppThemeProvider>,
    );

    expect(screen.getByLabelText("تاریخ سیستم").textContent).not.toEqual("");
    expect(screen.getByLabelText("ساعت سیستم").textContent).not.toEqual("");
    expect(screen.getByLabelText("تاریخ سیستم").textContent).not.toContain("،");
  });
});
