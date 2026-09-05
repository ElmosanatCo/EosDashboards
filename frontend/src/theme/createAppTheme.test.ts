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

  it("turns every accent card's top line gold on hover", () => {
    const theme = createAppTheme("dark", "teal");
    const paper = theme.components?.MuiPaper?.styleOverrides?.root as Record<
      string,
      Record<string, unknown>
    >;

    expect(paper["&.eos-accent-card"]).toMatchObject({
      transition: "border-top-color 175ms ease",
      "&:hover": { borderTopColor: "#E0A13A" },
    });
  });

  it("defines the shared compact management-list rhythm", () => {
    const theme = createAppTheme("dark", "teal");
    const baseline = theme.components?.MuiCssBaseline?.styleOverrides as Record<
      string,
      Record<string, string | number>
    >;

    expect(baseline[".eos-management-list-text"]).toMatchObject({
      fontFamily: 'Vazirmatn, "Segoe UI", sans-serif',
      fontSize: "0.875rem",
      lineHeight: 1.43,
    });
    expect(baseline[".eos-management-table .MuiTableCell-root"]).toMatchObject({
      paddingTop: 12,
      paddingBottom: 12,
    });
  });

  it("uses the Vazirmatn Farsi-digit family for numeric-bearing values", () => {
    const theme = createAppTheme("dark", "teal");
    const baseline = theme.components?.MuiCssBaseline?.styleOverrides as Record<
      string,
      unknown
    >;
    const fontFaces = baseline["@font-face"] as Array<Record<string, unknown>>;

    expect(fontFaces).toHaveLength(10);
    expect(fontFaces[0]).toMatchObject({ fontFamily: "Vazirmatn" });
    expect(fontFaces.slice(1)).toHaveLength(9);
    expect(
      fontFaces
        .slice(1)
        .every((fontFace) => fontFace.fontFamily === "Vazirmatn FD"),
    ).toBe(true);
    expect(
      baseline[".eos-persian-number, .eos-persian-number *"],
    ).toMatchObject({
      fontFamily:
        '"Vazirmatn FD", Vazirmatn, "Segoe UI", sans-serif !important',
    });
  });

  it("styles scrollbars from the active palette and appearance mode", () => {
    const darkTheme = createAppTheme("dark", "teal");
    const darkBaseline = darkTheme.components?.MuiCssBaseline
      ?.styleOverrides as Record<string, Record<string, string>>;

    expect(darkBaseline["*, *::before, *::after"]).toMatchObject({
      scrollbarColor: "#238C82 #0D1113",
      scrollbarWidth: "thin",
    });
    expect(darkBaseline["*::-webkit-scrollbar-thumb"]).toMatchObject({
      backgroundColor: "#238C82",
      border: "2px solid #0D1113",
    });

    const lightTheme = createAppTheme("light", "rose");
    const lightBaseline = lightTheme.components?.MuiCssBaseline
      ?.styleOverrides as Record<string, Record<string, string>>;

    expect(lightBaseline["*::-webkit-scrollbar-thumb"]).toMatchObject({
      backgroundColor: "#77263F",
      border: "2px solid #F2F5F3",
    });
  });
});
