import { describe, expect, it } from "vitest";
import {
  authorizedWorkspaceTargets,
  createWorkspaceTab,
} from "./workspaceTargets";

describe("workspace targets", () => {
  it("only exposes targets authorized by the user's stable role codes", () => {
    expect(
      authorizedWorkspaceTargets(["SystemAdministrator"]).map(
        (target) => target.routeId,
      ),
    ).toEqual([
      "system-administration-dashboard",
      "administration-users",
      "administration-departments",
      "administration-audit",
    ]);
    expect(
      authorizedWorkspaceTargets(["DepartmentManager"]).map(
        (target) => target.title,
      ),
    ).toEqual(["داشبورد بخش"]);
    expect(
      authorizedWorkspaceTargets([
        "DepartmentManager",
        "HumanResourcesManager",
        "ChiefExecutiveOfficer",
      ]).map((target) => target.title),
    ).toEqual(["داشبورد بخش", "داشبورد منابع انسانی", "داشبورد مدیرعامل"]);
  });

  it("creates a closable workspace tab for an authorized target", () => {
    const target = authorizedWorkspaceTargets(["DepartmentManager"])[0]!;

    expect(createWorkspaceTab(target)).toMatchObject({
      key: "department-dashboard",
      pathname: "/department-dashboard",
      closable: true,
    });
  });
});
