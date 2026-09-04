# راهنمای مرحله‌به‌مرحله نصب EosDashboards روی سرور شرکت

این راهنما برای سروری است که چندین Website، API و Application Pool روی IIS
دارد. هدف آن نصب مستقل EosDashboards بدون تغییر یا ایجاد اختلال در سایت‌های
دیگر است.

مقادیر داخل `<>` نمونه هستند و باید با مقادیر تأییدشدهٔ تیم زیرساخت جایگزین
شوند. هیچ Connection String، رمز، کلید، Certificate یا اطلاعات شخصی واقعی را
در Git یا این سند قرار ندهید.

## معماری پیشنهادی روی سرور چندسایته

برای این برنامه یک IIS Site یا یک محدودهٔ Application مستقل در نظر بگیرید و
API و UI را در Application Poolهای جدا اجرا کنید:

| جزء | مقدار نمونه | نکته |
| --- | --- | --- |
| Site | `EosDashboardsSite` | نامی مستقل از سایت‌های دیگر |
| HTTPS binding | `dashboards.<company-domain>` | Host Name یکتا و Certificate مربوط به همین نام |
| UI application | `/EosDashboards` | پوشهٔ استاتیک React |
| API application | `/EosDashboardsApi` | برنامهٔ ASP.NET Core |
| UI pool | `EosDashboardsUiPool` | فقط دسترسی خواندن فایل‌های UI |
| API pool | `EosDashboardsApiPool` | دسترسی اجرای API و کلیدهای API |
| Database | `<company-database>` | فقط از Secret امن انتشار خوانده شود |

اگر شرکت برای هر سامانه Host Name جداگانه دارد، از همان الگو استفاده کنید.
استفاده از پورت 443 بین چند سایت مشکلی ندارد، به شرطی که HTTPS Binding شامل
Host Name/SNI یکتای هر سایت باشد. قبل از ایجاد Binding، آن را با تیم زیرساخت
بررسی کنید؛ Binding تکراری می‌تواند سایت موجود را تحت تأثیر قرار دهد.

## مرحلهٔ ۱ — اطلاعاتی که قبل از نصب باید دریافت شود

قبل از هر تغییر، این موارد را از تیم زیرساخت بگیرید و در فرم تغییرات ثبت کنید:

- نام قطعی DNS و HTTPS Host Name برنامه؛
- Certificate و تاریخ انقضای آن؛
- نام Site، Application و Application Poolهای اختصاصی؛
- حساب Windows یا Service Account مربوط به API و حساب Deployment؛
- محل امن نگهداری Connection String و Secretها؛
- نام SQL Server و دیتابیس مقصد؛
- مسیر Backup و روش تأیید موفقیت Restore؛
- نسخهٔ ASP.NET Core Hosting Bundle نصب‌شده؛
- مسیرهای مجاز روی دیسک و قواعد Firewall؛
- فرد مسئول تأیید Migration و Rollback.

نباید از Site، Pool، Certificate، پورت یا مسیر فایل یکی از سامانه‌های دیگر
استفاده شود.

## مرحلهٔ ۲ — آماده‌سازی Release خارج از سرور

API و UI را روی سیستم Build یا CI/CD، از یک Commit مشخص بسازید. روی سرور
production سورس کد، `node_modules` یا SDK لازم برای Build قرار ندهید.

از ریشهٔ Repository:

```powershell
Set-Location D:\Workspaces\ChatGpt\EosDashboards

dotnet publish `
  .\backend\src\EosDashboards.Api\EosDashboards.Api.csproj `
  -c Release `
  -o .\artifacts\release\api

npm ci --prefix frontend
npm --prefix frontend run build:iis
```

خروجی UI در `frontend\dist` قرار می‌گیرد. این Build برای مسیرهای فعلی
`/EosDashboards/` و `/EosDashboardsApi` تنظیم شده است. اگر تیم زیرساخت Host یا
Application Path دیگری تصویب کند، UI باید با Base Path و API Base جدید دوباره
Build شود؛ `web.config` تولیدشده را دستی و بدون بررسی تغییر ندهید.

همراه همین Release، Migration Bundle را طبق
[راهنمای Migration دیتابیس](production-database-migration-fa.md) بسازید. API،
UI و Bundle باید از یک Commit ساخته شده باشند.

پیش از انتقال، این موارد را بررسی کنید:

- وجود `EosDashboards.Api.dll` و `web.config` در Artifact API؛
- وجود `index.html` و `web.config` در Artifact UI؛
- درست بودن Base Path و API Base در Bundle نهایی UI؛
- وجود فایل `EosDashboards.Migrations.exe`؛
- ثبت Commit، نام Release و Hash Artifact؛
- نبودن Secret، رمز، Connection String یا Source Map ناخواسته در Artifact.

## مرحلهٔ ۳ — آماده‌سازی IIS و Windows Server

این مرحله را تیم زیرساخت با دسترسی Administrator انجام دهد:

1. ASP.NET Core Hosting Bundle سازگار با Target Framework برنامه را نصب یا
   بررسی کنید.
2. IIS و قابلیت‌های لازم برای HTTPS و ASP.NET Core را فعال کنید.
3. Certificate معتبر Host Name را در Certificate Store سرور نصب کنید.
4. DNS Host Name را به IP سرور وصل کنید و دسترسی Firewall را بررسی کنید.
5. برای UI و API دو Application Pool مستقل بسازید.
6. برای هر دو Pool مقدار Managed Runtime را `No Managed Code` و Pipeline را
   `Integrated` قرار دهید.
7. Pool API را با حساب اختصاصی API اجرا کنید؛ از حساب Administrator استفاده
   نکنید.
8. Pool UI را فقط با دسترسی خواندن و اجرای فایل‌های UI اجرا کنید.
9. دسترسی API به پوشهٔ Key Ring، لاگ‌های مجاز و دیتابیس را محدود و مستند کنید.

نمونهٔ مسیرها:

```text
D:\IIS\EosDashboards\Ui\releases\<release-id>
D:\IIS\EosDashboards\Api\releases\<release-id>
D:\IIS\EosDashboards\Deployments\<release-id>
C:\ProgramData\EosDashboards\keys
```

هر Release باید در پوشهٔ جدا قرار گیرد. Artifact نسخهٔ قبلی را حذف نکنید؛ برای
Rollback لازم است.

## مرحلهٔ ۴ — ایجاد Site و Binding بدون تداخل

در IIS Manager:

1. در بخش **Sites** یک Site اختصاصی با نام غیرتکراری بسازید یا Site تأییدشدهٔ
   برنامه را انتخاب کنید.
2. Physical Path ریشهٔ Site را به پوشهٔ اختصاصی EosDashboards وصل کنید؛ از
   مسیر فیزیکی Siteهای دیگر استفاده نکنید.
3. در **Bindings** یک Binding از نوع `https` با Host Name یکتای شرکت و
   Certificate تأییدشده اضافه کنید.
4. اگر از دو Application زیر یک Site استفاده می‌شود، Applicationهای زیر را
   بسازید:

   - `/EosDashboards` → پوشهٔ Release UI و `EosDashboardsUiPool`؛
   - `/EosDashboardsApi` → پوشهٔ Release API و `EosDashboardsApiPool`.

5. مطمئن شوید هر Application با Pool درست وصل است.
6. در API، `web.config` Artifact را نگه دارید؛ حذف تنظیمات WebDAV آن باعث می‌شود
   درخواست‌های `PUT` و `DELETE` توسط IIS قبل از رسیدن به API با خطای 405 متوقف
   شوند.
7. در UI، `web.config` تولیدشده را نگه دارید تا Refresh مسیرهای داخلی SPA به
   Application UI برگردد.

اگر سیاست شرکت UI و API را روی دو Site جدا قرار می‌دهد، همین جداسازی Pool و
مجوزها را حفظ کنید و UI را با API Base و CORS مربوط به Host Name جدید Build
کنید.

## مرحلهٔ ۵ — تنظیمات production

تنظیمات production باید فقط روی سرور یا Secret Store نگهداری شوند، نه در Git.
حداقل این موارد را تنظیم و بررسی کنید:

- Connection String دیتابیس شرکت؛
- تنظیمات SMS و Endpointهای خارجی؛
- کلیدهای JWT و Data Protection؛
- مسیر Key Ring پایدار خارج از Web Root؛
- Origin دقیق UI برای CORS؛
- نام محیط production؛
- سطح و مسیر لاگ‌های مجاز.

Key Ring را با هر Release حذف نکنید. اگر کلیدهای Data Protection از بین بروند،
Session/Refresh Cookieها و داده‌های رمزگذاری‌شدهٔ محافظت‌شده ممکن است دیگر قابل
استفاده نباشند.

Runtime API نباید مجوز تغییر Schema داشته باشد. مجوز Schema فقط به حساب
Deployment داده شود و Connection String آن از Secret امن خوانده شود.

## مرحلهٔ ۶ — Backup و اجرای Migration

قبل از تغییر دیتابیس:

1. Backup تأییدشده بگیرید و Restore آن را طبق رویهٔ شرکت قابل بازیابی بودنش
   بررسی کنید.
2. Bundle همان Release را به پوشهٔ Deployment روی سرور منتقل کنید.
3. PowerShell را در همان پوشهٔ Release باز کنید:

```powershell
Set-Location C:\EosDashboards\Deployments\<release-id>
```

4. Connection String را از Secret Store یا سازوکار امن CI/CD دریافت کنید؛ آن
   را در فایل، History ترمینال یا Log چاپ نکنید.
5. Bundle را اجرا کنید:

```powershell
& .\EosDashboards.Migrations.exe --connection $dbConnection
```

6. فقط در صورت موفقیت کامل، آخرین Migration ثبت‌شده در
   `__EFMigrationsHistory` را بررسی کنید.

جزئیات قالب Connection String و Bundle در
[راهنمای فارسی Migration](production-database-migration-fa.md) آمده است.
اگر Migration شکست خورد، API جدید را فعال نکنید و علت را با تیم زیرساخت/DBA
بررسی کنید.

## مرحلهٔ ۷ — فعال‌سازی API و UI

پس از موفقیت Migration:

1. Application Pool API را به‌صورت کنترل‌شده Stop یا Drain کنید.
2. Application API را به پوشهٔ Release جدید وصل کنید.
3. Application Pool API را Start کنید.
4. پاسخ‌های زیر را بررسی کنید:

```text
https://<host>/EosDashboardsApi/health/live
https://<host>/EosDashboardsApi/health/ready
```

هر دو باید HTTP 200 برگردانند.

5. Application UI را به پوشهٔ Release جدید وصل کنید.
6. Application Pool UI را Start یا Recycle کنترل‌شده کنید.
7. صفحهٔ اصلی UI و یک مسیر داخلی SPA را با Refresh مستقیم باز کنید.

## مرحلهٔ ۸ — Smoke Test واقعی

با یک حساب آزمایشی مجاز، بدون ثبت اطلاعات واقعی غیرضروری، این موارد را
بررسی کنید:

- باز شدن UI با HTTPS و Certificate صحیح؛
- نمایش نام و برند صحیح برنامه؛
- ورود با نام کاربری و رمز؛
- ارسال و تأیید SMS OTP؛
- باز شدن یک صفحهٔ مجاز؛
- Refresh نشست احراز‌شده؛
- خروج؛
- یک درخواست خواندن و یک mutation مجاز؛
- API readiness پس از چند دقیقه؛
- نبودن خطای مهم در Event Viewer، لاگ IIS و لاگ API.

هیچ رمز، OTP، Token یا Connection String را در گزارش Smoke Test ثبت نکنید.

## مرحلهٔ ۹ — Rollback

اگر UI یا API مشکل داشت:

1. تغییرات جدید را متوقف کنید و علت را ثبت کنید.
2. API و UI را به پوشهٔ Release قبلی برگردانید.
3. Poolها را کنترل‌شده Restart کنید.
4. Health check و Smoke Test حداقلی را دوباره اجرا کنید.
5. اگر Migration دیتابیس موفق شده است، صرفاً Rollback کردن فایل API کافی نیست؛
   نسخهٔ قبلی باید با Schema جدید سازگار باشد یا DBA باید طبق برنامهٔ تأییدشده
   اصلاح/بازگردانی دیتابیس را انجام دهد.

Migrationهای مخرب مانند حذف ستون یا تغییر ناسازگار داده را در همان Release با
Rollback ساده ترکیب نکنید. برای آن‌ها از الگوی مرحله‌ای سازگار با نسخهٔ قبلی
و سپس پاک‌سازی در Release بعدی استفاده کنید.

## چک‌لیست تحویل نهایی

- [ ] Site، Binding، Certificate و DNS یکتا و تأیید شده‌اند.
- [ ] UI و API Pool جدا و با مجوز حداقلی هستند.
- [ ] Hosting Bundle سازگار نصب است.
- [ ] API، UI و Migration Bundle از یک Commit ساخته شده‌اند.
- [ ] Backup دیتابیس تأیید شده است.
- [ ] Migration با حساب Deployment موفق شده است.
- [ ] Connection String و Secretها در Git یا Log نیستند.
- [ ] Key Ring پایدار و خارج از Web Root است.
- [ ] API و UI به Release versioned جدید وصل شده‌اند.
- [ ] Health، Login، OTP، Refresh، Logout و مسیر داخلی SPA بررسی شده‌اند.
- [ ] Release قبلی برای Rollback نگه داشته شده است.
- [ ] Commit، Release، Migration، نتیجهٔ Smoke Test و مسئول انتشار ثبت شده‌اند.

## وضعیت این راهنما

این سند الگوی production را مشخص می‌کند. نام واقعی Host، مسیرها، حساب‌ها،
Certificate، Secret Store و مسئولیت‌های عملیاتی باید قبل از اولین انتشار با تیم
زیرساخت شرکت تأیید شوند.
