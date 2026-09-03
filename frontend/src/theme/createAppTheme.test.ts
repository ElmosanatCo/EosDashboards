import { describe, expect, it } from "vitest";
import { createAppTheme } from "./createAppTheme";

describe("createAppTheme", () => {
  it("uses compact flat operational components with short functional motion", () => {
    const theme = createAppTheme("dark", "teal");
    const paper = theme.components?.MuiPaper?.styleOverrides?.root as Record<
      string,
      string
    >;

    expect(theme.shape.borderRadius).toBe(6);
    expect(theme.transitions.duration.standard).toBe(200);
    expect(paper).toMatchObject({
      border: "1px solid #2A3538",
      boxShadow: "none",
    });
  });

  it("overrides every Chromium autofill state with the active theme surface", () => {
    const theme = createAppTheme("dark", "teal");
    const baseline = theme.components?.MuiCssBaseline?.styleOverrides as Record<
      string,
      Record<string, string>
    >;
    const autofill =
      baseline[
        "input:-webkit-autofill, input:-webkit-autofill:hover, input:-webkit-autofill:focus, input:-webkit-autofill:active"
      ];

    expect(autofill).toMatchObject({
      WebkitBoxShadow: "0 0 0 1000px #13191C inset !important",
      WebkitTextFillColor: "#EDF2F0 !important",
      caretColor: "#EDF2F0",
    });
  });
});
