import { describe, expect, it } from "vitest";
import { paletteOptions, resolvePalette } from "./palettes";

describe("application palettes", () => {
  it("offers the five approved interaction accents and uses the operational teal dark tokens", () => {
    expect(paletteOptions.map((option) => option.id)).toEqual([
      "teal",
      "indigo",
      "emerald",
      "amber",
      "rose",
    ]);
    expect(resolvePalette("teal", "dark")).toMatchObject({
      primary: { main: "#38B8AA" },
      background: { default: "#0D1113", paper: "#13191C" },
      text: { primary: "#EDF2F0", secondary: "#96A4A6" },
      divider: "#2A3538",
    });
  });
});
