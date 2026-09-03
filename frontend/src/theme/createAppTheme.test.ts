import { describe, expect, it } from "vitest";
import { createAppTheme } from "./createAppTheme";

describe("createAppTheme", () => {
  it("overrides every Chromium autofill state with the active theme surface", () => {
    const theme = createAppTheme("dark", "amber");
    const baseline = theme.components?.MuiCssBaseline?.styleOverrides as Record<
      string,
      Record<string, string>
    >;
    const autofill =
      baseline[
        "input:-webkit-autofill, input:-webkit-autofill:hover, input:-webkit-autofill:focus, input:-webkit-autofill:active"
      ];

    expect(autofill).toMatchObject({
      WebkitBoxShadow: "0 0 0 1000px #2A2111 inset !important",
      WebkitTextFillColor: "#FCF7EF !important",
      caretColor: "#FCF7EF",
    });
  });
});
