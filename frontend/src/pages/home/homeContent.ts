import type { TabDescriptor } from "../../navigation/tabTypes";

export type HomeRoleCode =
  | "SystemAdministrator"
  | "DepartmentManager"
  | "HumanResourcesManager"
  | "ChiefExecutiveOfficer";

const roleGuideText: Readonly<Record<HomeRoleCode, string>> = {
  SystemAdministrator:
    "در این فضا می‌توانید کاربران، واحدها و رویدادهای امنیتی سامانه را مدیریت کنید.",
  DepartmentManager:
    "در این فضا شاخص‌ها و اطلاعات مرتبط با واحد شما نمایش داده می‌شود.",
  HumanResourcesManager:
    "در این فضا اطلاعات و داشبوردهای منابع انسانی در اختیار شما قرار می‌گیرد.",
  ChiefExecutiveOfficer:
    "در این فضا نمای کلی شاخص‌های مدیریتی سازمان در اختیار شما قرار می‌گیرد.",
};

const roleGuideOrder: readonly HomeRoleCode[] = [
  "SystemAdministrator",
  "DepartmentManager",
  "HumanResourcesManager",
  "ChiefExecutiveOfficer",
];

const fallbackGuideText = "امکانات مجاز شما در ادامه نمایش داده می‌شود.";

export function getHomeGuideText(roleCodes: readonly string[]): string {
  const matchingRole = roleGuideOrder.find((roleCode) =>
    roleCodes.includes(roleCode),
  );

  return matchingRole ? roleGuideText[matchingRole] : fallbackGuideText;
}

export type HomeTargetMetadata = {
  readonly summary: string;
  readonly actionLabel: string;
};

export const homeTargetMetadata: Readonly<Record<string, HomeTargetMetadata>> =
  {
    "system-administration-dashboard": {
      summary: "نمای کلی مدیریت سامانه و رویدادهای امنیتی را ببینید.",
      actionLabel: "مشاهده داشبورد",
    },
    "administration-users": {
      summary: "حساب‌های کاربری و دسترسی‌های ثابت سامانه را مدیریت کنید.",
      actionLabel: "مدیریت کاربران",
    },
    "administration-departments": {
      summary: "ساختار واحدهای سازمانی را مدیریت و به‌روز کنید.",
      actionLabel: "مدیریت واحدها",
    },
    "administration-audit": {
      summary: "رویدادهای امنیتی و مدیریتی سامانه را بررسی کنید.",
      actionLabel: "مشاهده رویدادها",
    },
    "department-dashboard": {
      summary: "شاخص‌ها و اطلاعات مرتبط با واحد شما را ببینید.",
      actionLabel: "مشاهده داشبورد",
    },
    "human-resources-dashboard": {
      summary: "اطلاعات و داشبوردهای منابع انسانی را ببینید.",
      actionLabel: "مشاهده داشبورد",
    },
    "chief-executive-dashboard": {
      summary: "نمای کلی شاخص‌های مدیریتی سازمان را ببینید.",
      actionLabel: "مشاهده داشبورد",
    },
  };

const genericTargetMetadata: HomeTargetMetadata = {
  summary: "باز کردن این بخش از فضای کاری",
  actionLabel: "باز کردن",
};

export function getHomeTargetMetadata(routeId: string): HomeTargetMetadata {
  return homeTargetMetadata[routeId] ?? genericTargetMetadata;
}

export type HomeAlert = {
  readonly id: string;
  readonly title: string;
  readonly summary: string;
  readonly severity: "info" | "warning" | "error";
};

export const initialHomeAlerts: readonly HomeAlert[] = [];

export function selectRecentHomeTabs(
  tabs: readonly TabDescriptor[],
): TabDescriptor[] {
  return tabs.filter((tab) => tab.key !== "home").slice(0, 4);
}
