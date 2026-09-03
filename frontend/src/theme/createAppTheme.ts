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
      shape: { borderRadius: 10 },
      components: {
        MuiButton: {
          styleOverrides: {
            root: {
              "&.MuiButton-contained.MuiButton-colorPrimary:hover": {
                color: "#FFFFFF",
              },
            },
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
