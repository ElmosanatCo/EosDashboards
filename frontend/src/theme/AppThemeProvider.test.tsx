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
    <Button onClick={() => setMode("dark")}>
      {mode}:{resolvedMode}:{palette}
    </Button>
  );
}

afterEach(() => cleanup());

describe("AppThemeProvider", () => {
  it("uses RTL, system default, and persists an explicit appearance", async () => {
    localStorage.clear();
    render(
      <AppThemeProvider>
        <Probe />
      </AppThemeProvider>,
    );

    expect(document.documentElement.dir).toBe("rtl");
    expect(screen.getByRole("button")).toHaveTextContent("system:light:amber");
    await userEvent.click(screen.getByRole("button"));
    expect(screen.getByRole("button")).toHaveTextContent("dark:dark:amber");
    expect(localStorage.getItem("eos.appearance.anonymous")).toBe("dark");
    expect(localStorage.getItem("eos.appearance.last-used")).toBe("dark");
  });

  it("uses the last selected appearance and palette before the user signs in", () => {
    localStorage.clear();
    localStorage.setItem("eos.appearance.last-used", "dark");
    localStorage.setItem("eos.palette.last-used", "turquoise");

    render(
      <AppThemeProvider>
        <Probe />
      </AppThemeProvider>,
    );

    expect(screen.getByRole("button")).toHaveTextContent("dark:dark:turquoise");
  });
});
