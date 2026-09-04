# EosDashboards

سامانهٔ داشبوردهای سازمانی «علم و صنعت» با Backend مبتنی بر ASP.NET Core،
Frontend مبتنی بر React، SQL Server و میزبانی IIS.

## راهنمای مهم استقرار روی سرور شرکت

راهنمای فارسی اجرای Migrationهای دیتابیس، ساخت Migration Bundle، قالب
Connection String و ترتیب امن انتشار API در این فایل قرار دارد:

**[راهنمای مهاجرت دیتابیس روی سرور شرکت](docs/operations/production-database-migration-fa.md)**

خلاصهٔ فرآیند:

```text
ساخت API و Migration Bundle از یک Commit
        ↓
انتقال Release به سرور شرکت
        ↓
اجرای Bundle با حساب Deployment و دیتابیس مقصد
        ↓
فعال‌سازی API جدید فقط پس از موفقیت Migration
```

Migrationها هنگام Startup خود API اجرا نمی‌شوند. توسعه‌دهنده نیز به Connection
String یا دیتابیس شرکت نیاز ندارد؛ اجرای Migration باید توسط CI/CD یا ابزار
انتشار سرور و با هویت مجاز Deployment انجام شود.

## مستندات

- [راهنمای فارسی Migration روی سرور شرکت](docs/operations/production-database-migration-fa.md)
- [راهنمای IIS](docs/operations/iis-deployment.md)
- [حافظه و وضعیت پروژه](docs/project/current-state.md)
- [استانداردهای فنی و عملیاتی](docs/project/standards.md)

## ساختار Repository

- `backend/` — راهکار مستقل .NET و API
- `frontend/` — برنامهٔ React و workspace فرانت‌اند
- `docs/` — مستندات محصول، معماری، عملیات و تصمیم‌ها
- `scripts/` — ابزارهای پشتیبانی توسعه و انتشار محلی
