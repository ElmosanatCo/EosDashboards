# راهنمای اجرای Migration روی سرور شرکت

این راهنما برای انتشار نسخهٔ production برنامهٔ EosDashboards است. هدف آن این
است که توسعه‌دهنده بدون دسترسی مستقیم به دیتابیس شرکت، همراه هر نسخهٔ API،
تغییرات لازم دیتابیس را به‌صورت کنترل‌شده اعمال کند.

## ایدهٔ اصلی

Migrationها در کد پروژه و در مسیر زیر نگهداری می‌شوند:

```text
backend/src/EosDashboards.Infrastructure/Persistence/Migrations
```

برای هر Release، یک فایل اجرایی Migration Bundle ساخته می‌شود. ساخت این فایل
روی سیستم توسعه یا CI/CD انجام می‌شود و در این مرحله هیچ دیتابیسی تغییر نمی‌کند.
فایل اجرایی هنگام انتشار روی سرور شرکت با Connection String همان دیتابیس اجرا
می‌شود.

```text
ساخت Bundle از کد
        ↓
انتقال Bundle به سرور شرکت
        ↓
اجرای Bundle با اتصال دیتابیس شرکت
        ↓
اجرای فقط Migrationهای pending
        ↓
فعال‌سازی نسخهٔ جدید API
```

وضعیت Migrationهای اجراشده در جدول `__EFMigrationsHistory` ثبت می‌شود؛ بنابراین
یک Migration موفق دوباره اجرا نمی‌شود.

## ساخت Bundle

PowerShell را در ریشهٔ Repository باز کنید؛ یعنی پوشه‌ای که پوشه‌های `backend`
و `frontend` داخل آن هستند:

```powershell
Set-Location D:\Workspaces\ChatGpt\EosDashboards
New-Item -ItemType Directory -Force .\artifacts\production-migration | Out-Null

dotnet ef migrations bundle `
  --self-contained `
  --target-runtime win-x64 `
  --configuration Release `
  --project .\backend\src\EosDashboards.Infrastructure\EosDashboards.Infrastructure.csproj `
  --startup-project .\backend\src\EosDashboards.Api\EosDashboards.Api.csproj `
  --output .\artifacts\production-migration\EosDashboards.Migrations.exe
```

خروجی این دستور:

```text
artifacts/production-migration/EosDashboards.Migrations.exe
```

این فایل به دیتابیس توسعه یا شرکت وابسته نیست. اتصال دیتابیس در زمان اجرای
فایل روی سرور مشخص می‌شود.

## اجرای Bundle روی سرور شرکت

فایل Bundle را در پوشهٔ Release روی سرور قرار دهید؛ برای مثال:

```powershell
Set-Location C:\EosDashboards\Releases\20260910
```

### اتصال با حساب SQL Server

قالب Connection String:

```powershell
$dbConnection = "Server=SERVER_NAME;Database=DATABASE_NAME;User Id=DB_USER;Password=DB_PASSWORD;Encrypt=True;TrustServerCertificate=False"
```

اجرای Bundle:

```powershell
& .\EosDashboards.Migrations.exe --connection $dbConnection
```

### اتصال با حساب Windows یا Service Account

اگر سرویس انتشار با یک حساب Windows مجاز اجرا می‌شود:

```powershell
$dbConnection = "Server=SERVER_NAME;Database=EosDashboard;Integrated Security=True;Encrypt=True;TrustServerCertificate=False"
& .\EosDashboards.Migrations.exe --connection $dbConnection
```

در SQL Server دارای Instance نام‌دار، مقدار Server به این شکل نوشته می‌شود:

```text
Server=SERVER_NAME\INSTANCE_NAME;Database=EosDashboard;...
```

## ترتیب امن انتشار

1. از دیتابیس شرکت Backup تأییدشده بگیرید.
2. API و Migration Bundle را از یک Commit و یک Release بسازید.
3. Bundle را با حساب deployment و Connection String دیتابیس شرکت اجرا کنید.
4. نتیجهٔ اجرای Bundle و آخرین Migration ثبت‌شده را بررسی کنید.
5. فقط پس از موفقیت Migration، مسیر IIS را به API جدید تغییر دهید.
6. Health check، ورود، Refresh، خروج و مسیرهای اصلی را بررسی کنید.
7. در صورت شکست قبل از تغییر دیتابیس یا API، Release را متوقف کنید.

Rollback کردن فایل API به‌تنهایی، Migration دیتابیس را خودکار برنمی‌گرداند.
تغییرات دیتابیس باید تا حد امکان به‌صورت سازگار با نسخهٔ قبلی طراحی شوند؛ برای
تغییرات مخرب یا حذف ستون، مرحلهٔ جداگانه و برنامهٔ بازگشت لازم است.

## نکات امنیتی

- Connection String واقعی، رمز، کلیدها و Secretها نباید در Git، کد، Frontend،
  فایل Bundle یا لاگ ذخیره شوند.
- ترجیح production استفاده از `Encrypt=True` و `TrustServerCertificate=False`
  با گواهی معتبر SQL Server است.
- حساب اجرای API نباید مجوز تغییر ساختار دیتابیس داشته باشد.
- حساب deployment باید فقط به دیتابیس برنامه و اجرای تغییرات تأییدشده دسترسی
  داشته باشد.
- اگر Connection String ارائه نشود یا اعتبار آن مشخص نباشد، انتشار باید متوقف
  شود؛ نباید از Connection String محیط توسعه استفاده شود.

## گزینهٔ اسکریپت SQL

به‌جای Bundle می‌توان از اسکریپت idempotent نیز استفاده کرد:

```powershell
dotnet ef migrations script `
  --idempotent `
  --configuration Release `
  --project .\backend\src\EosDashboards.Infrastructure\EosDashboards.Infrastructure.csproj `
  --startup-project .\backend\src\EosDashboards.Api\EosDashboards.Api.csproj `
  --output .\artifacts\production-migration\EosDashboards.Migrations.sql
```

این فایل باید با ابزار مورد تأیید تیم زیرساخت مانند SQLCMD، SSMS یا CI/CD و با
هویت deployment اجرا شود. Bundle برای نصب روی سروری که نمی‌خواهیم SDK دات‌نت
روی آن نصب باشد، گزینهٔ ساده‌تری است.

## وضعیت فعلی پروژه

Migrationهای EF Core در Repository وجود دارند و اجرای مستقیم با `dotnet ef`
برای محیط توسعه استفاده می‌شود. فرآیند production شامل ساخت Bundle، نگهداری
Secret و اجرای خودکار آن توسط CI/CD یا ابزار انتشار سرور هنوز باید به‌صورت یک
قابلیت جداگانه پیاده‌سازی و با تیم زیرساخت شرکت هماهنگ شود.
