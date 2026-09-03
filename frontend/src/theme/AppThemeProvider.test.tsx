import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Button } from "@mui/material";
import { afterEach, describe, expect, it } from "vitest";
import {
  AppThemeProvider,
  useAppearance,
} from "../app/providers/AppThemeProvider";

function Probe() {
  const { mode, resolvedMode, palette, setMode } = useAppearance();
  return (
    <Button onClick={() => setMode("light")}>
      {mode}:{resolvedMode}:{palette}
    </Button>
  );
}

afterEach(() => cleanup());

describe("AppThemeProvider", () => {
  it("uses RTL, the dark teal operational default, and persists an explicit appearance", async () => {
    localStorage.clear();
    render(
      <AppThemeProvider>
        <Probe />
      </AppThemeProvider>,
    );

    expect(document.documentElement.dir).toBe("rtl");
    expect(screen.getByRole("button")).toHaveTextContent("dark:dark:teal");
    await userEvent.click(screen.getByRole("button"));
    expect(screen.getByRole("button")).toHaveTextContent("light:light:teal");
    expect(localStorage.getItem("eos.appearance.anonymous")).toBe("light");
    expect(localStorage.getItem("eos.appearance.last-used")).toBe("light");
  });

  it("uses the last selected appearance and maps a retired palette to teal before sign-in", () => {
    localStorage.clear();
    localStorage.setItem("eos.appearance.last-used", "dark");
    localStorage.setItem("eos.palette.last-used", "turquoise");

    render(
      <AppThemeProvider>
        <Probe />
      </AppThemeProvider>,
    );

    expect(screen.getByRole("button")).toHaveTextContent("dark:dark:teal");
  });
});
