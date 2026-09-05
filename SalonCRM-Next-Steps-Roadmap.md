# SalonCRM / CustomerReturnCRM
## نقشه راه ادامه توسعه

تاریخ تهیه: ۳ سپتامبر ۲۰۲۶

این سند پیشنهاد اجرایی برای ادامه توسعه پروژه CustomerReturnCRM بر اساس
اسناد محصول، معماری MVP و وضعیت فعلی کد است.

## وضعیت فعلی

چرخه اصلی محصول تا بخش Return Analysis و Smart Lists پیاده‌سازی شده است:

```text
Customer
  -> Appointment
  -> Complete Appointment
  -> Visit
  -> VisitService Snapshot
  -> Return Analysis
  -> Smart Lists
```

ساختار چهارلایه پروژه نیز ایجاد شده است:

```text
Domain
Application
Infrastructure
API
```

Migration مربوط به `Visit` و `VisitService` نیز اضافه شده و Build پروژه
با موفقیت انجام شده است.

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

Dashboard باید یک Query ترکیبی و action-oriented باشد، نه یک Entity جدید.

### خروجی پیشنهادی

- Appointmentهای امروز
- مشتریان Due Soon
- مشتریان Overdue
- Reminderهای Pending
- آخرین Visitها
- تعداد مشتریان فعال

### Endpoint پیشنهادی

```text
GET /api/businesses/{businessId}/dashboard
```

### معیار تکمیل

- Dashboard پاسخ سریع و قابل استفاده برای یک کسب‌وکار کوچک داشته باشد.
- خروجی آن از Queryهای موجود استفاده کند.
- جدول یا Snapshot جداگانه برای Dashboard ایجاد نشود.

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

این موارد بعد از تثبیت MVP بررسی شوند:

1. ServiceTemplate برای Onboarding
2. پروفایل کامل‌تر مشتری
3. گزارش‌های پایه
4. تخصیص Reminder به User
5. Permissionهای دقیق‌تر
6. SMS و کانال‌های ارتباطی

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
- بررسی خطاهای API، Pagination و Validation

## ترتیب نهایی پیشنهادی

```text
Database Update
  -> اصلاح Return Analysis
  -> Reminder
  -> Dashboard
  -> API Quality
  -> ServiceTemplate و قابلیت‌های بعدی
  -> Smoke Tests
  -> Integration Tests
```

## اولین کار پیشنهادی

اولین گام عملی این است:

1. اجرای `dotnet ef database update`
2. ثبت یک داده آزمایشی از مسیر کامل
3. بررسی چهار Smart List
4. تبدیل یکی از نتایج به Reminder پس از پیاده‌سازی آن

تا زمانی که این مسیر کامل و تست‌شده نباشد، توسعه قابلیت‌های جانبی توصیه
نمی‌شود.

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
  امروز، Reminderهای باز، Smart Listها، Visitهای اخیر و تعداد مشتریان فعال.
- ServiceTemplate با Seed اولیه و اتصال اختیاری به Business Setup اضافه شده است.
- Staff management شامل مشاهده، ایجاد، ویرایش و غیرفعال‌سازی اضافه شده است.
