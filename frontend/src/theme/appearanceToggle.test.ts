import { describe, expect, it } from "vitest";
import { nextAppearanceForToggle } from "./appearanceToggle";

describe("nextAppearanceForToggle", () => {
  it("changes the resolved system appearance on the first click", () => {
    expect(nextAppearanceForToggle("dark")).toBe("light");
    expect(nextAppearanceForToggle("light")).toBe("dark");
  });
});
