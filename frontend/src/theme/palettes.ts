import type { PaletteMode } from "@mui/material";

export const paletteOptions = [
  { id: "teal", label: "فیروزه‌ای", swatch: "#38B8AA" },
  { id: "indigo", label: "نیلی", swatch: "#7C83E8" },
  { id: "emerald", label: "زمردی", swatch: "#3CAF76" },
  { id: "amber", label: "کهربایی", swatch: "#E0A13A" },
  { id: "rose", label: "رز", swatch: "#D87591" },
] as const;

export type PaletteId = (typeof paletteOptions)[number]["id"];
export const defaultPaletteId: PaletteId = "teal";

const legacyPaletteIds = new Set([
  "forestGreen",
  "navyTeal",
  "turquoise",
  "plum",
  "burgundy",
]);

export function normalizePaletteId(
  value: string | null | undefined,
): PaletteId {
  if (paletteOptions.some((option) => option.id === value)) {
    return value as PaletteId;
  }
  return legacyPaletteIds.has(value ?? "")
    ? defaultPaletteId
    : defaultPaletteId;
}

type PaletteColors = {
  primary: { main: string; light: string; dark: string; contrastText: string };
  secondary: {
    main: string;
    light: string;
    dark: string;
    contrastText: string;
  };
  background: { default: string; paper: string };
  text: { primary: string; secondary: string };
  divider: string;
};

const lightBase: Omit<PaletteColors, "primary"> = {
  secondary: {
    main: "#2C6A88",
    light: "#5D98B5",
    dark: "#18465D",
    contrastText: "#FFFFFF",
  },
  background: { default: "#F2F5F3", paper: "#FBFCFA" },
  text: { primary: "#17201F", secondary: "#5C6B69" },
  divider: "#D8E0DC",
};

const darkBase: Omit<PaletteColors, "primary"> = {
  secondary: {
    main: "#54A9C3",
    light: "#85C7DB",
    dark: "#2C7289",
    contrastText: "#07151A",
  },
  background: { default: "#0D1113", paper: "#13191C" },
  text: { primary: "#EDF2F0", secondary: "#96A4A6" },
  divider: "#2A3538",
};

const accentThemes: Record<
  PaletteId,
  Record<PaletteMode, PaletteColors["primary"]>
> = {
  teal: {
    light: {
      main: "#198F84",
      light: "#4FB7AD",
      dark: "#0C625B",
      contrastText: "#FFFFFF",
    },
    dark: {
      main: "#38B8AA",
      light: "#72D2C8",
      dark: "#238C82",
      contrastText: "#071312",
    },
  },
  indigo: {
    light: {
      main: "#4F56B5",
      light: "#7F86D3",
      dark: "#30367C",
      contrastText: "#FFFFFF",
    },
    dark: {
      main: "#7C83E8",
      light: "#A9AEFF",
      dark: "#545AB5",
      contrastText: "#11122A",
    },
  },
  emerald: {
    light: {
      main: "#16744B",
      light: "#53A87B",
      dark: "#0B4A2F",
      contrastText: "#FFFFFF",
    },
    dark: {
      main: "#3CAF76",
      light: "#75CD9D",
      dark: "#218653",
      contrastText: "#07170E",
    },
  },
  amber: {
    light: {
      main: "#A76A09",
      light: "#D59A41",
      dark: "#764806",
      contrastText: "#FFFFFF",
    },
    dark: {
      main: "#E0A13A",
      light: "#F2C36E",
      dark: "#AF761D",
      contrastText: "#211504",
    },
  },
  rose: {
    light: {
      main: "#A93F60",
      light: "#CF718C",
      dark: "#77263F",
      contrastText: "#FFFFFF",
    },
    dark: {
      main: "#D87591",
      light: "#E9A5B7",
      dark: "#A84A65",
      contrastText: "#250B13",
    },
  },
};

const paletteThemes: Record<
  PaletteId,
  Record<PaletteMode, PaletteColors>
> = Object.fromEntries(
  paletteOptions.map(({ id }) => [
    id,
    {
      light: { ...lightBase, primary: accentThemes[id].light },
      dark: { ...darkBase, primary: accentThemes[id].dark },
    },
  ]),
) as Record<PaletteId, Record<PaletteMode, PaletteColors>>;

export function resolvePalette(id: PaletteId, mode: PaletteMode) {
  return paletteThemes[id][mode];
}
