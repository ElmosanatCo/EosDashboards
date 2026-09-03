import type { ApiProblem } from "../../lib/api/apiClient";

const eventNames: Record<string, string> = {
  UserCreated: "کاربر ایجاد شد",
  UserUpdated: "اطلاعات کاربر تغییر کرد",
  UserRolesChanged: "نقش‌های کاربر تغییر کرد",
  UserDepartmentChanged: "واحد کاربر تغییر کرد",
  UserDeactivated: "کاربر غیرفعال شد",
  UserActivated: "کاربر فعال شد",
  UserPasswordReset: "رمز عبور بازنشانی شد",
  DepartmentCreated: "واحد ایجاد شد",
  DepartmentUpdated: "واحد تغییر کرد",
  DepartmentDeleted: "واحد حذف شد",
  AuthSucceeded: "ورود موفق",
  SignInDenied: "تلاش ورود ناموفق",
  OtpVerificationFailed: "تأیید پیامکی ناموفق",
};

const problemMessages: Record<string, string> = {
  personnel_code_conflict: "کد پرسنلی تکراری است.",
  username_conflict: "نام کاربری تکراری است.",
  department_name_conflict: "نام واحد تکراری است.",
  department_not_empty: "ابتدا کاربران و زیرواحدها را جابه‌جا کنید.",
  department_hierarchy_invalid: "ساختار واحد انتخاب‌شده معتبر نیست.",
  last_system_administrator: "حداقل یک مدیر سامانهٔ فعال باید باقی بماند.",
  concurrency_conflict: "این رکورد تغییر کرده است؛ اطلاعات را تازه‌سازی کنید.",
};

export function eventLabel(code: string) {
  return eventNames[code] ?? code;
}

export function problemMessage(error: unknown) {
  const problem = error as Partial<ApiProblem>;
  return (
    problemMessages[problem.code ?? ""] ??
    "انجام عملیات ممکن نشد. دوباره تلاش کنید."
  );
}
