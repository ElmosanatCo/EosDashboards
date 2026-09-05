import type { ApiProblem } from "./apiClient";

const problemMessages: Record<string, string> = {
  personnel_code_conflict: "کد پرسنلی تکراری است.",
  username_conflict: "نام کاربری تکراری است.",
  department_name_conflict: "نام واحد تکراری است.",
  department_not_empty: "ابتدا کاربران و زیرواحدها را جابه‌جا کنید.",
  department_hierarchy_invalid: "ساختار واحد انتخاب‌شده معتبر نیست.",
  last_system_administrator: "حداقل یک مدیر سامانهٔ فعال باید باقی بماند.",
  concurrency_conflict: "این رکورد تغییر کرده است؛ اطلاعات را تازه‌سازی کنید.",
  catalog_duplicate: "این نام قبلاً در کاتالوگ ثبت شده است.",
  catalog_inactive_duplicate:
    "این نام مربوط به یک مورد غیرفعال است؛ از فیلتر موارد غیرفعال آن را فعال کنید.",
  catalog_item_not_found: "مورد کاتالوگ پیدا نشد.",
  catalog_scope_forbidden: "برای این مورد کاتالوگ دسترسی ندارید.",
  invalid_catalog_request: "اطلاعات کاتالوگ معتبر نیست.",
  incomplete_job_description:
    "شرح وظیفه ناقص است؛ موارد تطبیق‌نشده و داده‌های ناقص را اصلاح و دوباره ارسال کنید.",
};

export function problemMessage(error: unknown) {
  const problem = error as Partial<ApiProblem>;
  return (
    problemMessages[problem.code ?? ""] ??
    "انجام عملیات ممکن نشد. دوباره تلاش کنید."
  );
}
