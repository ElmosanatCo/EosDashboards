import type { PaletteMode } from "@mui/material";

export function nextAppearanceForToggle(resolvedMode: PaletteMode) {
  return resolvedMode === "dark" ? "light" : "dark";
}
