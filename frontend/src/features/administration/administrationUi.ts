export { problemMessage } from "../../lib/api/problemMessage";

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
  OtpVerificationInvalid: "کد تأیید نامعتبر",
  OtpSendFailed: "ارسال کد تأیید ناموفق",
  OtpResendSendFailed: "ارسال مجدد کد تأیید ناموفق",
  AuthenticationSucceeded: "احراز هویت موفق",
  PasswordChanged: "رمز عبور تغییر کرد",
  PasswordResetSucceeded: "بازنشانی رمز موفق",
  PasswordResetOtpSendFailed: "ارسال کد بازنشانی رمز ناموفق",
  PasswordResetOtpResendFailed: "ارسال مجدد کد بازنشانی رمز ناموفق",
  SessionRefreshDenied: "تازه‌سازی نشست رد شد",
  GoogleAuthenticationSucceeded: "ورود Google موفق",
  GoogleAuthenticationDenied: "ورود Google رد شد",
  GoogleAuthenticationFailed: "ورود Google ناموفق",
  UserPreferenceChanged: "تنظیمات کاربر تغییر کرد",
  SystemAdministratorProvisioned: "مدیر سامانه ایجاد یا همگام شد",
};

export const eventOptions = Object.entries(eventNames).map(([code, label]) => ({
  code,
  label,
}));

export function eventLabel(code: string) {
  return eventNames[code] ?? code;
}
