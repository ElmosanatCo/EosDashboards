import { describe, expect, it } from "vitest";
import { paletteOptions, resolvePalette } from "./palettes";

describe("application palettes", () => {
  it("offers the approved interaction accents and includes the orange palette", () => {
    expect(paletteOptions.map((option) => option.id)).toEqual([
      "teal",
      "indigo",
      "emerald",
      "amber",
      "orange",
      "rose",
    ]);
    expect(
      paletteOptions.find((option) => option.id === "orange"),
    ).toMatchObject({
      label: "نارنجی",
      swatch: "#E65100",
    });
    expect(resolvePalette("orange", "dark").primary.main).toBe("#E65100");
    expect(resolvePalette("teal", "dark")).toMatchObject({
      primary: { main: "#38B8AA" },
      background: { default: "#0D1113", paper: "#13191C" },
      text: { primary: "#EDF2F0", secondary: "#96A4A6" },
      divider: "#2A3538",
    });
  });
});
