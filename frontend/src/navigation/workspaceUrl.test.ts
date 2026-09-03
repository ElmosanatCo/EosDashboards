import { describe, expect, it } from "vitest";
import { toWorkspaceUrl } from "./workspaceUrl";

describe("toWorkspaceUrl", () => {
  it("keeps a workspace tab inside the IIS application base path", () => {
    expect(toWorkspaceUrl("/EosDashboards/", "/", "")).toBe("/EosDashboards/");
  });

  it("keeps development workspace URLs rooted at the Vite application", () => {
    expect(toWorkspaceUrl("/", "/reports", "?period=current")).toBe(
      "/reports?period=current",
    );
  });
});
