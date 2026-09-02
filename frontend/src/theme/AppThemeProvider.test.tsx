import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Button } from "@mui/material";
import { describe, expect, it } from "vitest";
import { AppThemeProvider, useAppearance } from "../app/providers/AppThemeProvider";

function Probe() {
  const { mode, resolvedMode, setMode } = useAppearance();
  return <Button onClick={() => setMode("dark")}>{mode}:{resolvedMode}</Button>;
}

describe("AppThemeProvider", () => {
  it("uses RTL, system default, and persists an explicit appearance", async () => {
    localStorage.clear();
    render(<AppThemeProvider><Probe /></AppThemeProvider>);

    expect(document.documentElement.dir).toBe("rtl");
    expect(screen.getByRole("button")).toHaveTextContent("system:light");
    await userEvent.click(screen.getByRole("button"));
    expect(screen.getByRole("button")).toHaveTextContent("dark:dark");
    expect(localStorage.getItem("eos.appearance.anonymous")).toBe("dark");
  });
});
