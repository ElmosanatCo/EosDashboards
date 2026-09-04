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

  it.each([
    [
      "DepartmentManager",
      "امکانات مدیریتی مرتبط با واحد شما پس از تصویب شاخص‌ها و اتصال منبع داده در این فضا تکمیل می‌شود.",
    ],
    [
      "HumanResourcesManager",
      "امکانات و داشبوردهای منابع انسانی پس از تصویب منبع داده در این فضا تکمیل می‌شوند.",
    ],
    [
      "ChiefExecutiveOfficer",
      "نمای مدیریتی سازمان پس از تصویب شاخص‌ها و اتصال منبع داده در این فضا تکمیل می‌شود.",
    ],
  ])("uses truthful future-oriented guide copy for %s", (roleCode, guide) => {
    expect(getHomeGuideText([roleCode])).toBe(guide);
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

  it("filters unauthorized tabs before the four-tab cap while preserving order", () => {
    const tabs: TabDescriptor[] = [
      {
        key: "home",
        routeId: "home",
        pathname: "/",
        search: "",
        title: "خانه",
        closable: false,
      },
      {
        key: "stale-administration-users",
        routeId: "administration-users",
        pathname: "/users",
        search: "",
        title: "مدیریت کاربران",
        closable: true,
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

    expect(
      selectRecentHomeTabs(
        tabs,
        ["one", "two", "three", "four", "five"],
        ["DepartmentManager"],
      ).map((tab) => tab.key),
    ).toEqual(["one", "two", "three", "four"]);
    expect(tabs).toEqual(originalTabs);
  });

  it("preserves administrator form tabs only for System Administrators", () => {
    const formTabs: TabDescriptor[] = [
      "administration-user-create",
      "administration-user-edit",
      "department-form",
    ].map((routeId) => ({
      key: routeId,
      routeId,
      pathname: `/${routeId}`,
      search: "",
      title: routeId,
      closable: true,
    }));

    expect(
      selectRecentHomeTabs(
        formTabs,
        ["administration-users"],
        ["SystemAdministrator"],
      ),
    ).toEqual(formTabs);
    expect(
      selectRecentHomeTabs(
        formTabs,
        ["department-dashboard"],
        ["DepartmentManager"],
      ),
    ).toEqual([]);
  });
});
