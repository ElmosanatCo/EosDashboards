import type { PaletteMode } from "@mui/material";

export const paletteOptions = [
  { id: "forestGreen", label: "سبز جنگلی", swatch: "#1B5E20" },
  { id: "navyTeal", label: "سرمه‌ای", swatch: "#123A63" },
  { id: "turquoise", label: "فیروزه‌ای", swatch: "#00796B" },
  { id: "plum", label: "ارغوانی", swatch: "#6A1B5A" },
  { id: "amber", label: "کهربایی", swatch: "#A65F00" },
  { id: "burgundy", label: "زرشکی", swatch: "#7B1E32" },
] as const;

export type PaletteId = (typeof paletteOptions)[number]["id"];
export const defaultPaletteId: PaletteId = "amber";

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

const paletteThemes: Record<PaletteId, Record<PaletteMode, PaletteColors>> = {
  forestGreen: {
    light: {
      primary: {
        main: "#176B3A",
        light: "#4C956C",
        dark: "#0E4726",
        contrastText: "#FFFFFF",
      },
      secondary: {
        main: "#A65A2E",
        light: "#D8885E",
        dark: "#783719",
        contrastText: "#FFFFFF",
      },
      background: { default: "#F7FAF7", paper: "#FFFFFF" },
      text: { primary: "#14231A", secondary: "#536158" },
      divider: "rgba(20, 35, 26, .16)",
    },
    dark: {
      primary: {
        main: "#1B5E20",
        light: "#65A66A",
        dark: "#0B3711",
        contrastText: "#FFFFFF",
      },
      secondary: {
        main: "#C97945",
        light: "#E6AB7D",
        dark: "#80411F",
        contrastText: "#101712",
      },
      background: { default: "#0C130E", paper: "#152019" },
      text: { primary: "#F0F7F0", secondary: "#B7C8B9" },
      divider: "rgba(183, 200, 185, .22)",
    },
  },
  navyTeal: {
    light: {
      primary: {
        main: "#123A63",
        light: "#46668F",
        dark: "#071F39",
        contrastText: "#FFFFFF",
      },
      secondary: {
        main: "#00897B",
        light: "#4EBAAA",
        dark: "#005B4F",
        contrastText: "#FFFFFF",
      },
      background: { default: "#F7F9FC", paper: "#FFFFFF" },
      text: { primary: "#10243A", secondary: "#586878" },
      divider: "rgba(16, 36, 58, .16)",
    },
    dark: {
      primary: {
        main: "#74A9D4",
        light: "#A9D1F2",
        dark: "#1F527E",
        contrastText: "#071F39",
      },
      secondary: {
        main: "#4EBAAA",
        light: "#86D7CC",
        dark: "#00897B",
        contrastText: "#071F39",
      },
      background: { default: "#0E1720", paper: "#172431" },
      text: { primary: "#F4F7FA", secondary: "#B5C4D4" },
      divider: "rgba(181, 196, 212, .24)",
    },
  },
  turquoise: {
    light: {
      primary: {
        main: "#00796B",
        light: "#3E9C91",
        dark: "#00544B",
        contrastText: "#FFFFFF",
      },
      secondary: {
        main: "#276C9D",
        light: "#6499C0",
        dark: "#174B71",
        contrastText: "#FFFFFF",
      },
      background: { default: "#F4FAF9", paper: "#FFFFFF" },
      text: { primary: "#112622", secondary: "#526963" },
      divider: "rgba(17, 38, 34, .16)",
    },
    dark: {
      primary: {
        main: "#35A99B",
        light: "#7DD1C5",
        dark: "#00685C",
        contrastText: "#06221E",
      },
      secondary: {
        main: "#6BA8D6",
        light: "#A4CEF0",
        dark: "#387AAE",
        contrastText: "#082339",
      },
      background: { default: "#0B1715", paper: "#122522" },
      text: { primary: "#F0F8F6", secondary: "#B8CBC6" },
      divider: "rgba(184, 203, 198, .23)",
    },
  },
  plum: {
    light: {
      primary: {
        main: "#6A1B5A",
        light: "#985486",
        dark: "#49113E",
        contrastText: "#FFFFFF",
      },
      secondary: {
        main: "#457A92",
        light: "#7AA9BB",
        dark: "#285466",
        contrastText: "#FFFFFF",
      },
      background: { default: "#FBF7FA", paper: "#FFFFFF" },
      text: { primary: "#2A1628", secondary: "#6C5969" },
      divider: "rgba(42, 22, 40, .16)",
    },
    dark: {
      primary: {
        main: "#B56AA1",
        light: "#D8A1CB",
        dark: "#7A3F6C",
        contrastText: "#301128",
      },
      secondary: {
        main: "#78B2CA",
        light: "#A7D4E6",
        dark: "#427A91",
        contrastText: "#102A36",
      },
      background: { default: "#180F17", paper: "#281725" },
      text: { primary: "#FAF4F9", secondary: "#D1C1CE" },
      divider: "rgba(209, 193, 206, .23)",
    },
  },
  amber: {
    light: {
      primary: {
        main: "#A65F00",
        light: "#D18E38",
        dark: "#744000",
        contrastText: "#FFFFFF",
      },
      secondary: {
        main: "#496F52",
        light: "#7D9E82",
        dark: "#2D4934",
        contrastText: "#FFFFFF",
      },
      background: { default: "#FFFAF3", paper: "#FFFFFF" },
      text: { primary: "#2A210F", secondary: "#6D614A" },
      divider: "rgba(42, 33, 15, .16)",
    },
    dark: {
      primary: {
        main: "#D8942F",
        light: "#F2BD69",
        dark: "#9A5B09",
        contrastText: "#2B1B03",
      },
      secondary: {
        main: "#87AD8D",
        light: "#B4CFB9",
        dark: "#507758",
        contrastText: "#102415",
      },
      background: { default: "#19140C", paper: "#2A2111" },
      text: { primary: "#FCF7EF", secondary: "#D5C8AF" },
      divider: "rgba(213, 200, 175, .23)",
    },
  },
  burgundy: {
    light: {
      primary: {
        main: "#7B1E32",
        light: "#AB5568",
        dark: "#551020",
        contrastText: "#FFFFFF",
      },
      secondary: {
        main: "#3E7182",
        light: "#75A2AF",
        dark: "#234D5A",
        contrastText: "#FFFFFF",
      },
      background: { default: "#FCF7F8", paper: "#FFFFFF" },
      text: { primary: "#2C161B", secondary: "#705860" },
      divider: "rgba(44, 22, 27, .16)",
    },
    dark: {
      primary: {
        main: "#C46A7D",
        light: "#E4A5B2",
        dark: "#8C3C4F",
        contrastText: "#321019",
      },
      secondary: {
        main: "#76ACBD",
        light: "#A4CED9",
        dark: "#447689",
        contrastText: "#0D2931",
      },
      background: { default: "#190E12", paper: "#2A171D" },
      text: { primary: "#FBF4F5", secondary: "#D5C1C6" },
      divider: "rgba(213, 193, 198, .23)",
    },
  },
};

export function resolvePalette(id: PaletteId, mode: PaletteMode) {
  return paletteThemes[id][mode];
}
