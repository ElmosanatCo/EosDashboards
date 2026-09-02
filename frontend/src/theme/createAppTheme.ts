import { createTheme } from "@mui/material/styles";
import { faIR } from "@mui/material/locale";
import type { PaletteMode } from "@mui/material";
import { navyTealPalette } from "./palettes";

export function createAppTheme(mode: PaletteMode) {
  return createTheme(
    {
      direction: "rtl",
      palette: {
        mode,
        ...navyTealPalette,
        ...(mode === "dark"
          ? { background: { default: "#0E1720", paper: "#172431" } }
          : {}),
      },
      typography: {
        fontFamily: 'Vazirmatn, "Segoe UI", sans-serif',
      },
      shape: { borderRadius: 10 },
      components: {
        MuiCssBaseline: {
          styleOverrides: {
            "@font-face": {
              fontFamily: "Vazirmatn",
              fontStyle: "normal",
              fontDisplay: "swap",
              fontWeight: "100 900",
              src: "url('/generated-assets/fonts/Vazirmatn[wght].woff2') format('woff2')",
            },
            body: { margin: 0, minWidth: 320 },
            "*:focus-visible": {
              outline: "3px solid #00897B",
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
