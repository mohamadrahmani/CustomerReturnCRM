# SalonCRM / CustomerReturnCRM
## نقشه راه ادامه توسعه

تاریخ تهیه: ۵ سپتامبر ۲۰۲۶

این سند پیشنهاد اجرایی برای ادامه توسعه پروژه CustomerReturnCRM بر اساس
اسناد محصول، معماری MVP و وضعیت فعلی کد است.

## وضعیت فعلی

چرخه اصلی محصول تا بخش Dashboard عملیاتی پیاده‌سازی شده است:

```text
Customer
  -> Appointment
  -> Complete Appointment
  -> Visit
  -> VisitService Snapshot
  -> Return Analysis
  -> Smart Lists
  -> Reminder
  -> Operational Dashboard
```

ساختار چهارلایه پروژه نیز ایجاد شده است:

```text
Domain
Application
Infrastructure
API
```

Migration مربوط به `Visit` و `VisitService` و همچنین Migration مربوط به
`ServiceTemplate` اضافه شده‌اند.

## اصل راهبردی

هر قابلیت جدید باید به چرخه زیر کمک کند:

```text
شناخت مشتری
  -> تشخیص زمان احتمالی بازگشت
  -> پیشنهاد مشتری مناسب برای پیگیری
  -> ثبت اقدام انسانی
  -> بازگشت مشتری
```

از اضافه‌کردن قابلیت‌هایی که خارج از محدوده MVP هستند خودداری شود؛ از جمله
پرداخت، حسابداری، انبار، SMS، کمپین، شعب و اپلیکیشن موبایل.

## اصلاحات انجام‌شده پیش از Frontend

### Customer List

Customer API اکنون در `CustomerResult` دو مقدار query-derived ارائه می‌کند:

```text
LastVisitDate
TotalVisits
```

`LastVisitDate` آخرین `Visit.VisitAt` مشتری در همان Business است و
`TotalVisits` تعداد Visitهای واقعی همان مشتری در همان Business است.
برای مشتری بدون Visit مقدار `LastVisitDate` برابر `null` و مقدار
`TotalVisits` برابر `0` است.

این اطلاعات به Entity مشتری اضافه نشده‌اند و در زمان Query محاسبه می‌شوند.
در نتیجه تاریخچه Visit به‌عنوان منبع حقیقت باقی می‌ماند و داده summary
تکراری در Customer ذخیره نمی‌شود.

### ServiceTemplate

ServiceTemplate بررسی شد و مشخص شد که قبلاً واقعاً پیاده‌سازی شده است:

- Entity و EF Core model وجود دارد.
- Migration `AddServiceTemplates` وجود دارد.
- Seed اولیه برای `General` و `BeautySalon` وجود دارد.
- `GET /api/service-templates?businessType=...` پیاده‌سازی شده است.
- Business Setup می‌تواند `serviceTemplateId` دریافت کند.
- Template انتخاب‌شده به‌صورت یک Service معمولی در Business کپی می‌شود.
- Template پس از Copy به Business Service متصل باقی نمی‌ماند.

بنابراین تناقض موجود در README اصلاح شد و ServiceTemplate اکنون در وضعیت
Implemented قرار دارد.

## مرحله اول: تثبیت وضعیت فعلی

### کارها

1. اجرای Migration روی دیتابیس محلی:

   ```powershell
   dotnet ef database update `
     --project src/CustomerReturnCRM.Infrastructure/CustomerReturnCRM.Infrastructure.csproj `
     --startup-project src/CustomerReturnCRM.API/CustomerReturnCRM.API.csproj
   ```

2. بررسی سریع Build و Migrationهای تولیدشده:

   ```text
   Register
   -> Login
   -> Create Business
   -> Select ServiceTemplate (optional)
   -> Create Customer
   -> Create Service
   -> Create Appointment
   -> Complete Appointment
   -> View Visit
   -> View Return Analysis
   -> View Smart Lists
   ```

3. اضافه‌کردن `.gitignore` مناسب برای:

   ```text
   bin/
   obj/
   .vs/
   ```

4. بررسی تغییرات untracked و generated قبل از ثبت در Git.

### معیار تکمیل

- دیتابیس خالی با اجرای Migration بدون خطا ساخته شود.
- مسیر اصلی محصول از نظر کد و Migration کامل باشد.
- هیچ داده‌ای از یک Business در Business دیگر نمایش داده نشود.

## مرحله دوم: اصلاح منطق Return Analysis

این مرحله قبل از تست نهایی انجام می‌شود، چون Return Analysis قلب ارزش
محصول است.

### موارد لازم برای تست

- آخرین Visit هر `Customer + Service`
- چند سرویس برای یک Visit
- تغییر قیمت Service پس از ثبت Visit
- تغییر `SuggestedReturnDays` پس از ثبت Visit
- سرویس بدون `SuggestedReturnDays`
- مشتری بدون Visit
- مشتری دارای Appointment آینده
- مشتری دارای Appointment لغوشده یا NoShow
- جلوگیری از Complete دوباره Appointment
- جداسازی کامل داده‌ها بین Businessها

### اصلاحات پیشنهادی

1. Appointment آینده فقط باید آیتم مربوط به همان `Customer + Service` را
   از Smart List حذف کند، نه الزاماً تمام سرویس‌های مشتری را.

2. تعریف این دسته‌ها باید ثابت و تست‌شده باشد:

   - `DueSoon`: تاریخ بازگشت امروز تا هفت روز آینده
   - `Overdue`: تاریخ بازگشت گذشته، اما در محدوده قبل از At Risk
   - `AtRisk`: تأخیر بیشتر از آستانه At Risk
   - `NoRecentVisit`: بدون Visit در بازه مشخص

3. تصمیم مربوط به مشتری‌ای که هیچ Visit نداشته است صریح شود:

   - در `NoRecentVisit` نمایش داده شود؛ یا
   - تا اولین Visit از تحلیل حذف شود.

4. Time Zone محاسبات تاریخ مشخص و در تمام APIها یکسان شود.

### معیار تکمیل

- نتایج Smart List برای تاریخ‌های مرزی مشخص و مستند باشد.
- Return Analysis به جدول یا Entity ذخیره‌شده وابسته نباشد.

## مرحله سوم: پیاده‌سازی Reminder

Reminder اولین قابلیت بعد از تحلیل است که خروجی تحلیلی را به اقدام عملی
تبدیل می‌کند.

### مدل پیشنهادی

فیلدهای اصلی:

```text
Id
BusinessId
CustomerId
ServiceId nullable
Title
DueAt
Status
Note nullable
CreatedByUserId
CompletedAt nullable
CreatedAt
UpdatedAt
```

وضعیت‌ها:

```text
Pending
Completed
Cancelled
```

### Endpointهای پیشنهادی

```text
GET  /api/businesses/{businessId}/reminders
POST /api/businesses/{businessId}/reminders
POST /api/businesses/{businessId}/reminders/{id}/complete
POST /api/businesses/{businessId}/reminders/{id}/cancel
```

### قواعد مهم

- Reminder به‌صورت خودکار ساخته نشود.
- کاربر از Smart List تصمیم بگیرد و Reminder بسازد.
- Reminder باید به Business و Customer معتبر تعلق داشته باشد.
- تکمیل یا لغو Reminder باید عملیات تکرارشونده امنی داشته باشد.
- حذف سخت Reminder در MVP ضروری نیست.

### معیار تکمیل

- کاربر بتواند یک مشتری Smart List را به Reminder تبدیل کند.
- Reminderهای باز در API قابل مشاهده باشند.
- تکمیل و لغو Reminder تاریخ و وضعیت مناسب ثبت کند.

## مرحله چهارم: Dashboard عملیاتی

**وضعیت: پیاده‌سازی و تکمیل شد.**

Dashboard به‌صورت یک Query ترکیبی و action-oriented پیاده‌سازی شده و Entity یا
Snapshot جداگانه‌ای برای آن ایجاد نشده است.

### Backend

Endpoint فعال:

```text
GET /api/businesses/{businessId}/dashboard
```

خروجی فعلی شامل موارد زیر است:

- Appointmentهای امروز همراه با مشتری، زمان، وضعیت و خدمات
- Reminderهای Pending همراه با مشتری و عنوان پیگیری
- Due Soon
- Overdue
- At Risk
- No Recent Visit
- آخرین Visitهای مشتریان فعال
- تعداد مشتریان فعال

داده‌ها مستقیماً از Queryهای عملیاتی و `IReturnAnalysisService` خوانده می‌شوند
و برای Dashboard جدول یا cache اختصاصی ساخته نشده است.

### Frontend

صفحه `/dashboard` اکنون شامل موارد زیر است:

- کارت‌های KPI برای مشتریان فعال، نوبت‌های امروز، پیگیری‌های باز و موارد نیازمند اقدام
- لیست اولویت‌دار مشتریان نیازمند اقدام
- نمایش نوبت‌های امروز
- نمایش Reminderهای باز
- نمایش آخرین Visitها
- لینک مستقیم به بخش‌های عملیاتی مربوط
- حالت‌های Loading، Empty و Error
- طراحی responsive برای Desktop و Mobile
- امکان تکمیل سریع Reminder از خود Dashboard و Refresh داده‌های Dashboard پس از آن

### معیار تکمیل

- Dashboard پاسخ سریع و قابل استفاده برای یک کسب‌وکار کوچک داشته باشد.
- خروجی آن از Queryهای موجود استفاده کند.
- جدول یا Snapshot جداگانه برای Dashboard ایجاد نشود.
- Dashboard فقط گزارش‌دهنده نباشد و حداقل یک اقدام عملی مستقیم داشته باشد.

## مرحله پنجم: تکمیل کیفیت API

پس از کامل‌شدن چرخه عملیاتی، این موارد انجام شوند:

- مدیریت مرکزی Exceptionها
- پاسخ خطای استاندارد
- Validation یکسان برای Requestها
- Pagination برای لیست‌های بزرگ
- مرتب‌سازی و فیلتر در Customers، Appointments، Visits و Smart Lists
- تکمیل Swagger/OpenAPI
- Logging مناسب
- کنترل Roleها در نقاط لازم
- بررسی هم‌پوشانی Appointmentها
- محدودیت‌های دقیق برای ویرایش Appointment تکمیل‌شده
- تست Migration روی دیتابیس کاملاً خالی

## مرحله ششم: قابلیت‌های بعدی

ServiceTemplate و Customer List summary دیگر در این بخش نیستند؛ هر دو قبل
از شروع Frontend تثبیت شده‌اند.

موارد باقی‌مانده بعد از تثبیت MVP:

1. پروفایل کامل‌تر مشتری
2. گزارش‌های پایه
3. تخصیص Reminder به User
4. Permissionهای دقیق‌تر
5. SMS و کانال‌های ارتباطی

هیچ‌کدام از این موارد نباید قبل از تثبیت چرخه MVP وارد توسعه اصلی شوند.

## مرحله پایانی: تست و کنترل کیفیت

پس از تکمیل قابلیت‌های MVP انجام شود:

- اجرای Migration روی دیتابیس خالی
- Smoke Test مسیر کامل محصول
- Integration Test برای Authentication و Tenant isolation
- تست Complete Appointment و ساخت Visit
- تست Return Analysis و چهار Smart List
- تست Dismiss و Restore آیتم‌های Smart List
- تست Reminder و Dashboard
- تست Customer List شامل `LastVisitDate` و `TotalVisits`
- تست ServiceTemplate در Business Setup
- بررسی خطاهای API، Pagination و Validation

## ترتیب نهایی پیشنهادی

```text
Customer List summary
  -> ServiceTemplate verification
  -> Database Update
  -> اصلاح Return Analysis
  -> Reminder
  -> Dashboard
  -> API Quality
  -> Smoke Tests
  -> Integration Tests
  -> Frontend
```

## وضعیت اصلاحات انجام‌شده

- منطق Appointment آینده برای هر `Customer + Service` محاسبه می‌شود.
- آستانه‌های Smart List از تنظیمات پروژه استفاده می‌کنند.
- خروج دستی از Smart List با `POST /smart-lists/dismiss` اضافه شده است.
- بازگردانی آیتم با `POST /smart-lists/restore` اضافه شده است.
- Dismissal به Visit و Expected Return فعلی متصل است و با Visit جدید،
  چرخه جدید دوباره قابل تحلیل خواهد بود.
- Reminder به‌عنوان اقدام دستی کاربر اضافه شده است؛ شامل ایجاد، فهرست،
  تکمیل و لغو.
- Dashboard عملیاتی به‌صورت Query ترکیبی اضافه شده است؛ شامل نوبت‌های
  امروز، Reminderهای باز، چهار Smart List، Visitهای اخیر و تعداد مشتریان فعال.
- Dashboard در Frontend به یک نقطه اقدام روزانه تبدیل شده و تکمیل سریع Reminder
  نیز از داخل آن ممکن است.
- ServiceTemplate با Seed اولیه و اتصال اختیاری به Business Setup اضافه شده
  و وضعیت آن از `Not implemented` به `Implemented` اصلاح شده است.
- CustomerResult اکنون `LastVisitDate` و `TotalVisits` را به‌صورت query-derived
  برای Customer List و Customer Detail ارائه می‌کند.
- Staff management شامل مشاهده، ایجاد، ویرایش و غیرفعال‌سازی اضافه شده است.
- Visit API و Frontend با نام مشتری و اطلاعات Staff غنی‌سازی شده‌اند.
- Visits به Navigation اصلی برنامه اضافه شده است.
- CI برای Backend و Frontend به Workflow پروژه اضافه شده و CORS نیز برای توسعه
  Frontend محلی تنظیم شده است.
