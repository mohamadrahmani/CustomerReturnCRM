# CustomerReturnCRM UI Implementation Specification

## 1. هدف

این سند مشخصات اجرای رابط کاربری برای Backend پروژه CustomerReturnCRM است.
هدف UI کمک به صاحب کسب‌وکار برای انجام کار روزانه و بازگرداندن مشتریان است؛
نه ساخت یک نرم‌افزار صرفاً تقویمی.

## 2. فرض‌های فنی

در صورت نبود تصمیم دیگری، UI با این پشته ساخته شود:

- Next.js با TypeScript
- React Query برای Server State
- React Hook Form برای فرم‌ها
- Tailwind CSS یا سیستم طراحی معادل
- پشتیبانی کامل RTL
- زبان اصلی فارسی
- طراحی Responsive برای Desktop، Tablet و Mobile

تمام تاریخ‌ها در UI به تقویم و Locale فارسی نمایش داده شوند، اما مقدار
ارسالی به API به‌صورت ISO 8601 باشد.

## 3. اصول تجربه کاربری

1. صفحه اصلی باید پاسخ دهد: «امروز چه کاری باید انجام دهم؟»
2. Smart Listها باید قابل اقدام باشند.
3. از نمایش اطلاعات تحلیلی بدون Action پرهیز شود.
4. فرم‌ها کوتاه و مناسب کسب‌وکار یک‌نفره باشند.
5. کاربر نباید مجبور باشد مفاهیم فنی مانند Tenant یا Snapshot را بداند.
6. وضعیت Loading، Empty، Error و Success برای تمام صفحات الزامی است.
7. حذف اطلاعات عملیاتی به‌صورت Hard Delete انجام نشود؛ از غیرفعال‌سازی
   استفاده شود.

## 4. احراز هویت و Session

### صفحات

```text
/login
/register
```

### Register

فیلدها:

- Email
- Password
- تأیید Password

پس از موفقیت:

1. Token در یک روش امن ذخیره شود.
2. Businessهای برگشتی بررسی شوند.
3. کاربر به `/onboarding/business` هدایت شود، مگر این‌که قبلاً Business داشته باشد.

### Login

پس از Login:

- اگر Business وجود ندارد: `/onboarding/business`
- اگر یک Business وجود دارد: `/dashboard`
- اگر چند Business وجود دارد: `/select-business`

در تمام درخواست‌های محافظت‌شده:

```text
Authorization: Bearer <token>
```

## 5. Onboarding

### مسیر

```text
/onboarding/business
  -> /onboarding/service
  -> /dashboard
```

### فرم Business

فیلدها:

- نام کسب‌وکار
- نوع کسب‌وکار
- موبایل
- آدرس اختیاری
- شهر اختیاری
- نام صاحب کسب‌وکار
- نام خانوادگی صاحب کسب‌وکار
- موبایل Staff اختیاری

Business Type بهتر است با مقدار قابل توسعه ارسال شود؛ برای سالن زیبایی
از `BeautySalon` استفاده شود.

### انتخاب Service Template

قبل از ساخت اولین Service:

```http
GET /api/service-templates?businessType=BeautySalon
```

Templateها به‌صورت Card نمایش داده شوند و شامل این موارد باشند:

- عنوان سرویس
- مدت زمان
- بازه پیشنهادی بازگشت

پس از انتخاب، شناسه Template در درخواست ساخت Business ارسال شود:

```json
{
  "name": "سالن نمونه",
  "businessType": "BeautySalon",
  "mobile": "09120000000",
  "firstName": "مریم",
  "lastName": "احمدی",
  "serviceTemplateId": "template-id"
}
```

سرویس ایجادشده قیمت پیش‌فرض `0` دارد؛ بلافاصله پس از Onboarding امکان
ویرایش قیمت سرویس نمایش داده شود.

## 6. Shell اصلی برنامه

### Desktop

Sidebar:

```text
داشبورد
تقویم نوبت‌ها
مشتریان
خدمات
کارکنان
پیگیری‌ها
گزارش بازگشت مشتری
تنظیمات
خروج
```

Header:

- نام Business فعلی
- انتخاب Business در صورت وجود چند Business
- تاریخ امروز
- منوی کاربر

### Mobile

- Header فشرده
- Bottom Navigation برای Dashboard، Appointments، Customers و More
- Sidebar به Drawer تبدیل شود.

## 7. Dashboard

### Route

```text
/dashboard
```

### API

```http
GET /api/businesses/{businessId}/dashboard
```

### Layout

ردیف اول KPI:

- مشتریان فعال
- نوبت‌های امروز
- Reminderهای باز
- مشتریان نیازمند پیگیری

بخش اصلی:

1. نوبت‌های امروز
2. Reminderهای باز
3. Due Soon
4. Overdue و At Risk
5. Visitهای اخیر

### Actionها

- کلیک روی مشتری: `/customers/{customerId}`
- کلیک روی نوبت: `/appointments/{appointmentId}`
- کلیک روی Reminder: `/reminders/{reminderId}`
- ساخت Reminder از Smart List
- Complete کردن Reminder
- رفتن به فهرست کامل هر بخش

### Empty State

اگر داده‌ای وجود ندارد، متن کاربردی نمایش داده شود:

```text
امروز نوبت یا پیگیری باز ندارید.
```

## 8. Customers

### Routes

```text
/customers
/customers/new
/customers/{customerId}
/customers/{customerId}/edit
```

### API

```http
GET    /api/businesses/{businessId}/customers
GET    /api/businesses/{businessId}/customers/{customerId}
POST   /api/businesses/{businessId}/customers
PUT    /api/businesses/{businessId}/customers/{customerId}
DELETE /api/businesses/{businessId}/customers/{customerId}
```

### List

ستون‌ها:

- نام
- موبایل
- آخرین مراجعه
- تعداد مراجعات
- وضعیت بازگشت
- Action

فیلترها:

- جست‌وجوی نام یا موبایل
- فعال / غیرفعال

اطلاعاتی مانند LastVisitDate و TotalVisits از API فعلی CustomerResult
برنمی‌گردد؛ UI باید برای نمایش این موارد از اطلاعات Profile و تحلیل
بازگشت استفاده کند و آن‌ها را در Client به‌عنوان منبع مستقل ذخیره نکند.

### Customer Profile

بخش‌ها:

1. اطلاعات تماس
2. سرویس‌های دریافت‌شده
3. تحلیل بازگشت برای هر Service
4. تاریخچه Visit
5. نوبت‌های آینده
6. Reminderهای مشتری

Actionهای اصلی:

- ساخت Appointment
- ثبت Visit مستقیم
- ساخت Reminder
- ویرایش مشتری

## 9. Services

### Routes

```text
/services
/services/new
/services/{serviceId}/edit
```

### API

```http
GET    /api/businesses/{businessId}/services
GET    /api/businesses/{businessId}/services/{serviceId}
POST   /api/businesses/{businessId}/services
PUT    /api/businesses/{businessId}/services/{serviceId}
DELETE /api/businesses/{businessId}/services/{serviceId}
```

### فرم Service

- عنوان
- توضیحات اختیاری
- قیمت پیش‌فرض
- مدت زمان به دقیقه
- روزهای پیشنهادی بازگشت
- فعال / غیرفعال

قیمت با جداکننده هزارگان نمایش داده شود، اما به‌صورت عدد ارسال شود.
از نمایش واحد پولی ساختگی خودداری شود؛ واحد مبلغ باید در تنظیمات محصول
نهایی و در UI ثابت شود.

## 10. Staff

### Routes

```text
/staff
/staff/new
/staff/{staffId}/edit
```

### API

```http
GET    /api/businesses/{businessId}/staff
GET    /api/businesses/{businessId}/staff/{staffId}
POST   /api/businesses/{businessId}/staff
PUT    /api/businesses/{businessId}/staff/{staffId}
DELETE /api/businesses/{businessId}/staff/{staffId}
```

در فرم Staff، اتصال User اجباری نیست. Staff می‌تواند فقط ارائه‌دهنده
سرویس باشد و برای انتخاب در Appointment استفاده شود.

## 11. Appointments

### Routes

```text
/appointments
/appointments/new
/appointments/{appointmentId}
```

### API

```http
GET  /api/businesses/{businessId}/appointments?from=<iso>&to=<iso>
GET  /api/businesses/{businessId}/appointments/{appointmentId}
POST /api/businesses/{businessId}/appointments
PUT  /api/businesses/{businessId}/appointments/{appointmentId}
POST /api/businesses/{businessId}/appointments/{appointmentId}/cancel
POST /api/businesses/{businessId}/appointments/{appointmentId}/complete
```

### تقویم

- نمایش روزانه و هفتگی
- فیلتر بر اساس Staff
- نمایش Customer و Service
- رنگ‌بندی Status
- امکان ایجاد نوبت با کلیک روی بازه زمانی

Statusها:

```text
Pending
Confirmed
Completed
Cancelled
NoShow
```

Appointment فقط برنامه است. پس از انجام واقعی کار، کاربر باید از Action
`Complete` استفاده کند تا Visit ساخته شود.

## 12. Visits

### API

```http
GET  /api/businesses/{businessId}/visits?from=<iso>&to=<iso>
GET  /api/businesses/{businessId}/visits/{visitId}
POST /api/businesses/{businessId}/visits
```

ثبت Visit مستقیم برای Walk-in باید در UI قابل دسترسی باشد.

در فرم Visit:

- Customer
- تاریخ مراجعه
- سرویس‌ها
- Staff برای هر سرویس
- مبلغ نهایی اختیاری
- یادداشت اختیاری

قیمت و عنوان Snapshot‌شده تاریخی در Visit نباید با تغییر Service فعلی
تغییر کند.

## 13. Smart Lists و Return Analysis

### Routes

```text
/return-analysis
/return-analysis/customer/{customerId}
```

### API

```http
GET /api/businesses/{businessId}/return-analysis/customers/{customerId}
GET /api/businesses/{businessId}/smart-lists/overdue
GET /api/businesses/{businessId}/smart-lists/due-soon
GET /api/businesses/{businessId}/smart-lists/at-risk
GET /api/businesses/{businessId}/smart-lists/no-recent-visit
```

### Tabs

```text
Due Soon
Overdue
At Risk
No Recent Visit
```

هر Item باید نشان دهد:

- نام مشتری
- موبایل
- سرویس
- آخرین Visit
- تاریخ مورد انتظار بازگشت
- تعداد روز نسبت به تاریخ بازگشت
- وضعیت Appointment آینده

### خروج دستی از لیست

برای حذف دستی:

```http
POST /api/businesses/{businessId}/smart-lists/dismiss
```

نمونه:

```json
{
  "smartListType": "Overdue",
  "customerId": "customer-id",
  "serviceId": "service-id"
}
```

برای `NoRecentVisit` مقدار `serviceId` برابر `null` باشد.

بعد از Dismiss:

- Item از لیست فعلی حذف شود.
- Toast موفقیت نمایش داده شود.
- امکان Undo کوتاه‌مدت یا Restore از بخش «موارد حذف‌شده» فراهم شود.

برای بازگردانی:

```http
POST /api/businesses/{businessId}/smart-lists/restore
```

Dismissal به چرخه فعلی Visit متصل است؛ با ثبت Visit جدید، تحلیل چرخه جدید
را محاسبه می‌کند.

## 14. Reminders

### Routes

```text
/reminders
/reminders/{reminderId}
```

### API

```http
GET  /api/businesses/{businessId}/reminders
GET  /api/businesses/{businessId}/reminders/{reminderId}
POST /api/businesses/{businessId}/reminders
POST /api/businesses/{businessId}/reminders/{reminderId}/complete
POST /api/businesses/{businessId}/reminders/{reminderId}/cancel
```

### ساخت Reminder

ورودی:

- Customer
- Service اختیاری
- عنوان
- تاریخ و ساعت انجام
- یادداشت اختیاری

Reminder به‌صورت خودکار ساخته نمی‌شود. فقط پس از تصمیم کاربر از Smart
List یا Dashboard ساخته شود.

Statusها:

```text
Pending
Completed
Cancelled
```

در لیست Reminderها:

- Pendingها در بالا
- مرتب‌سازی بر اساس DueAt
- دکمه Complete
- دکمه Cancel
- فیلتر Status و بازه تاریخ

## 15. API Client و خطاها

یک API client مرکزی ساخته شود که:

1. Base URL را از Environment بگیرد.
2. Token را به درخواست اضافه کند.
3. در پاسخ `401` کاربر را به Login هدایت کند.
4. در پاسخ `403` پیام عدم دسترسی نمایش دهد.
5. خطاهای `400` را کنار Field یا به‌صورت Summary نمایش دهد.
6. خطاهای `409` را به‌صورت Conflict قابل فهم نمایش دهد.
7. خطاهای ناشناخته را با پیام عمومی و Correlation/Request id نمایش دهد.

از فراخوانی مستقیم `fetch` در Componentها خودداری شود؛ تمام درخواست‌ها
از Hook یا Service layer عبور کنند.

## 16. State و Cache

Query keyها باید Business را شامل شوند:

```text
["dashboard", businessId]
["customers", businessId]
["customer", businessId, customerId]
["appointments", businessId, from, to]
["smart-list", businessId, listType]
["reminders", businessId, filters]
```

پس از Mutation:

- Customer: invalidate customers و customer profile
- Appointment: invalidate appointments و dashboard
- Complete Appointment: invalidate appointments، visits، dashboard و smart lists
- Visit: invalidate visits، customer profile، dashboard و smart lists
- Reminder: invalidate reminders و dashboard
- Dismiss/Restore: invalidate همان Smart List و dashboard

## 17. دسترسی و امنیت UI

- هیچ `businessId` از URL بدون بررسی Membership قابل اعتماد نیست؛ API
  مسئول نهایی Authorization است.
- Business فعلی در Context سراسری UI نگه‌داری شود.
- تغییر Business باید تمام Queryهای وابسته را invalidate کند.
- Token و اطلاعات حساس در Local Storage خام در محیط Production ذخیره نشود.
- قبل از ارسال، داده‌های فرم Trim و Normalize شوند.

## 18. ترتیب پیاده‌سازی UI

```text
1. App shell و Auth
2. Business selector و Onboarding
3. Dashboard
4. Customers و Customer profile
5. Services و Staff
6. Appointments و Calendar
7. Visits
8. Return Analysis و Smart Lists
9. Reminder
10. Loading/Error/Empty states
11. Responsive polish
12. تست نهایی و Accessibility
```

## 19. Definition of Done

UI زمانی آماده است که:

- کاربر بتواند Register و Login کند.
- بتواند Business بسازد و Service Template انتخاب کند.
- Dashboard اطلاعات واقعی Business را نمایش دهد.
- Customer، Service و Staff قابل مدیریت باشند.
- Appointment ایجاد و Complete شود.
- Visit مستقیم و Visit حاصل از Appointment نمایش داده شود.
- Smart Listها قابل مشاهده و Dismiss/Restore باشند.
- Reminder از Smart List ساخته و Complete/Cancel شود.
- هیچ Query داده Business دیگری را نشان ندهد.
- تمام صفحات Loading، Empty و Error state داشته باشند.
- UI در موبایل و دسکتاپ قابل استفاده باشد.
