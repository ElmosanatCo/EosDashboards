import { describe, expect, it } from "vitest";
import { problemMessage } from "./problemMessage";

describe("problemMessage", () => {
  it("explains an incomplete job description response", () => {
    expect(problemMessage({ code: "incomplete_job_description" })).toContain(
      "ناقص",
    );
  });
});
