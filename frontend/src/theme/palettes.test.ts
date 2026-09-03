import { describe, expect, it } from "vitest";
import { paletteOptions, resolvePalette } from "./palettes";

describe("application palettes", () => {
  it("offers exactly six named choices and uses forest green in dark mode", () => {
    expect(paletteOptions).toHaveLength(6);
    expect(resolvePalette("forestGreen", "dark").primary.main).toMatch(
      /^#1B5E20$/i,
    );
  });
});
