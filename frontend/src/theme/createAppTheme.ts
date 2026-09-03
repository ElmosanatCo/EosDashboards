import { createTheme } from "@mui/material/styles";
import { faIR } from "@mui/material/locale";
import type { PaletteMode } from "@mui/material";
import { resolvePalette } from "./palettes";
import type { PaletteId } from "./palettes";

const generatedAssetsBaseUrl = `${import.meta.env.BASE_URL}generated-assets/`;

export function createAppTheme(mode: PaletteMode, palette: PaletteId) {
  const paletteColors = resolvePalette(palette, mode);
  return createTheme(
    {
      direction: "rtl",
      palette: {
        mode,
        ...paletteColors,
      },
      typography: {
        fontFamily: 'Vazirmatn, "Segoe UI", sans-serif',
      },
      shape: { borderRadius: 6 },
      transitions: {
        duration: {
          shortest: 150,
          shorter: 150,
          short: 175,
          standard: 200,
          complex: 200,
          enteringScreen: 200,
          leavingScreen: 175,
        },
      },
      components: {
        MuiAppBar: {
          styleOverrides: {
            root: {
              backgroundColor: paletteColors.background.paper,
              borderBottom: `1px solid ${paletteColors.divider}`,
              boxShadow: "none",
              color: paletteColors.text.primary,
            },
          },
        },
        MuiButton: {
          styleOverrides: {
            root: {
              borderRadius: 4,
              transition: "background-color 175ms ease, color 175ms ease",
              "&.MuiButton-contained.MuiButton-colorPrimary:hover": {
                color: paletteColors.primary.contrastText,
              },
            },
          },
        },
        MuiPaper: {
          styleOverrides: {
            root: {
              backgroundImage: "none",
              border: `1px solid ${paletteColors.divider}`,
              boxShadow: "none",
              "&.eos-accent-card": {
                transition: "border-top-color 175ms ease",
                "&:hover": { borderTopColor: "#E0A13A" },
              },
            },
          },
        },
        MuiTab: {
          styleOverrides: {
            root: {
              minHeight: 42,
              minWidth: 72,
              padding: "0 12px",
              transition: "color 175ms ease, background-color 175ms ease",
            },
          },
        },
        MuiTabs: {
          styleOverrides: {
            root: { minHeight: 42 },
            indicator: { height: 2, backgroundColor: "#E0A13A" },
          },
        },
        MuiToolbar: {
          styleOverrides: {
            root: { minHeight: "56px !important" },
          },
        },
        MuiCssBaseline: {
          styleOverrides: {
            "@font-face": {
              fontFamily: "Vazirmatn",
              fontStyle: "normal",
              fontDisplay: "swap",
              fontWeight: "100 900",
              src: `url('${generatedAssetsBaseUrl}fonts/Vazirmatn[wght].woff2') format('woff2')`,
            },
            body: { margin: 0, minWidth: 320 },
            "input:-webkit-autofill, input:-webkit-autofill:hover, input:-webkit-autofill:focus, input:-webkit-autofill:active":
              {
                WebkitBoxShadow: `0 0 0 1000px ${paletteColors.background.paper} inset !important`,
                WebkitTextFillColor: `${paletteColors.text.primary} !important`,
                caretColor: paletteColors.text.primary,
              },
            "*:focus-visible": {
              outline: `3px solid ${paletteColors.primary.light}`,
              outlineOffset: 2,
            },
            "@media (prefers-reduced-motion: reduce)": {
              "*, *::before, *::after": {
                animationDuration: "0.01ms !important",
                transitionDuration: "0.01ms !important",
              },
            },
          },
        },
      },
    },
    faIR,
  );
}
