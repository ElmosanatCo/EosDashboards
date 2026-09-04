import { describe, expect, it } from "vitest";
import { formatPersianNumber, toPersianDigits } from "./persianDigits";

describe("Persian number formatting", () => {
  it("formats user-facing numbers with Persian digits and grouping", () => {
    expect(formatPersianNumber(1234567)).toBe("۱٬۲۳۴٬۵۶۷");
  });

  it("converts digits in display strings without changing their punctuation", () => {
    expect(toPersianDigits("24 ساعت اخیر")).toBe("۲۴ ساعت اخیر");
  });
});
