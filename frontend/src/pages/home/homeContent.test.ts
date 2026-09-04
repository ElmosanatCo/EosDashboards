import { describe, expect, it } from "vitest";
import { workspaceTargets } from "../../navigation/workspaceTargets";
import type { TabDescriptor } from "../../navigation/tabTypes";
import {
  getHomeGuideText,
  getHomeTargetMetadata,
  homeTargetMetadata,
  initialHomeAlerts,
  selectRecentHomeTabs,
} from "./homeContent";

describe("Home content model", () => {
  it("returns the administrator guide copy with role precedence", () => {
    expect(getHomeGuideText(["DepartmentManager", "SystemAdministrator"])).toBe(
      "در این فضا می‌توانید کاربران، واحدها و رویدادهای امنیتی سامانه را مدیریت کنید.",
    );
  });

  it("returns the fallback guide copy for an unknown role", () => {
    expect(getHomeGuideText(["FutureRole"])).toBe(
      "امکانات مجاز شما در ادامه نمایش داده می‌شود.",
    );
  });

  it("provides metadata for every current target", () => {
    for (const target of workspaceTargets) {
      expect(homeTargetMetadata[target.routeId]).toEqual({
        summary: expect.any(String),
        actionLabel: expect.any(String),
      });
    }
  });

  it("returns generic metadata for an unknown route", () => {
    expect(getHomeTargetMetadata("unknown-route")).toEqual({
      summary: "باز کردن این بخش از فضای کاری",
      actionLabel: "باز کردن",
    });
  });

  it("starts with no alerts", () => {
    expect(initialHomeAlerts).toEqual([]);
  });

  it("excludes Home, preserves order, limits to four, and leaves input unchanged", () => {
    const tabs: TabDescriptor[] = [
      {
        key: "home",
        routeId: "home",
        pathname: "/",
        search: "",
        title: "خانه",
        closable: false,
      },
      ...["one", "two", "three", "four", "five"].map((key) => ({
        key,
        routeId: key,
        pathname: `/${key}`,
        search: "",
        title: key,
        closable: true,
      })),
    ];
    const originalTabs = [...tabs];

    expect(selectRecentHomeTabs(tabs).map((tab) => tab.key)).toEqual([
      "one",
      "two",
      "three",
      "four",
    ]);
    expect(tabs).toEqual(originalTabs);
  });
});
