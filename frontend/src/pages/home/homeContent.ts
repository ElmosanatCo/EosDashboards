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
    "امکانات مدیریتی مرتبط با واحد شما پس از تصویب شاخص‌ها و اتصال منبع داده در این فضا تکمیل می‌شود.",
  HumanResourcesManager:
    "امکانات و داشبوردهای منابع انسانی پس از تصویب منبع داده در این فضا تکمیل می‌شوند.",
  ChiefExecutiveOfficer:
    "نمای مدیریتی سازمان پس از تصویب شاخص‌ها و اتصال منبع داده در این فضا تکمیل می‌شود.",
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
    "department-job-descriptions": {
      summary: "شرح وظایف پرسنل را ثبت، بررسی و برای منابع انسانی ارسال کنید.",
      actionLabel: "مدیریت شرح وظایف",
    },
    "department-skill-catalog": {
      summary: "مهارت‌های عمومی و اختصاصی بخش‌ها را یکسان‌سازی کنید.",
      actionLabel: "کاتالوگ مهارت‌ها",
    },
    "department-task-catalog": {
      summary:
        "وظایف استاندارد بخش را تعریف و مهارت‌های لازم آن‌ها را تعیین کنید.",
      actionLabel: "کاتالوگ وظایف",
    },
    "human-resources-dashboard": {
      summary: "اطلاعات و داشبوردهای منابع انسانی را ببینید.",
      actionLabel: "مشاهده داشبورد",
    },
    "human-resources-job-description-review": {
      summary:
        "شرح وظایف ارسالی، نسخه‌های تأییدشده و مهارت‌های عمومی را مدیریت کنید.",
      actionLabel: "مدیریت شرح وظایف",
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

const systemAdministratorFormRouteIds = new Set([
  "administration-user-create",
  "administration-user-edit",
  "department-form",
]);

export function selectRecentHomeTabs(
  tabs: readonly TabDescriptor[],
  authorizedTargetRouteIds: readonly string[],
  roleCodes: readonly string[],
): TabDescriptor[] {
  const authorizedRouteIds = new Set(authorizedTargetRouteIds);
  const isSystemAdministrator = roleCodes.includes("SystemAdministrator");

  return tabs
    .filter(
      (tab) =>
        tab.key !== "home" &&
        (authorizedRouteIds.has(tab.routeId) ||
          (isSystemAdministrator &&
            systemAdministratorFormRouteIds.has(tab.routeId))),
    )
    .slice(0, 4);
}
