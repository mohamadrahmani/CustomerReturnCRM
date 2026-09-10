# SMS API Contract

## Purpose

This document describes the SMS backend API currently implemented for the CustomerReturnCRM frontend.

SMS is an **action/capability**, not an independent navigation domain. It is exposed contextually from Customers, Smart Lists, Appointments, and Settings/Template management. The frontend should reuse one shared **SMS Composer**.

## Base URL

Development:

```text
http://localhost:5108
```

API base path:

```text
/api/businesses/{businessId}/sms
```

All endpoints require authentication. The authenticated user must be a member of the requested business.

## Common conventions

### Authentication

```http
Authorization: Bearer <token>
```

### Business scope

`businessId` is required on every endpoint. Non-authenticated requests return `401`. Authenticated users who are not members of the business receive `403`.

### Date/time

API `DateTime` values are UTC/ISO 8601. The frontend must display and select dates/times using the project's Persian date/time components and send ISO 8601 values to the API. `scheduledAt` must be in the future when supplied.

### SMS variables

Supported variables are exactly:

```text
[نام]
[نام خانوادگی]
[نام کامل]
[نام کسب‌وکار]
```

Variables are resolved when a campaign is created. The rendered message is stored on each recipient, so later customer or template changes do not modify an existing campaign.

Maximum campaign message length: **2000 characters**.
Maximum recipients per campaign: **10,000**.

---

# 1. Templates

Templates are reusable SMS message definitions belonging to a business. They should be managed under Settings rather than added to the main application navigation.

## 1.1 List templates

```http
GET /api/businesses/{businessId}/sms/templates?activeOnly=true
```

`activeOnly` defaults to `true`.

## 1.2 Get template

```http
GET /api/businesses/{businessId}/sms/templates/{templateId}
```

Returns `200` or `404` when the template does not exist in the business.

## 1.3 Create template

```http
POST /api/businesses/{businessId}/sms/templates
Content-Type: application/json
```

```json
{
  "name": "یادآوری مراجعه",
  "content": "سلام [نام]، زمان مراجعه بعدی شما فرا رسیده است."
}
```

Rules:

- `name` is required.
- `content` is required.
- Template name is unique within the business.
- New templates are active by default.

## 1.4 Update template

```http
PUT /api/businesses/{businessId}/sms/templates/{templateId}
Content-Type: application/json
```

```json
{
  "name": "یادآوری مراجعه",
  "content": "سلام [نام]، زمان مراجعه بعدی شما فرا رسیده است.",
  "isActive": true
}
```

There is no delete operation. Use `isActive` to disable a template.

---

# 2. Campaigns

A campaign represents an SMS sending operation. A campaign stores a snapshot of recipient mobile numbers and rendered messages.

## 2.1 List campaigns

```http
GET /api/businesses/{businessId}/sms/campaigns
```

Query parameters:

| Parameter | Type | Default |
|---|---|---:|
| `status` | integer/enum | null |
| `page` | integer | 1 |
| `pageSize` | integer | 20 |

Response is the standard `PagedResult<SmsCampaignResult>` with `items`, `page`, `pageSize`, and `totalCount`.

The list response contains summary counts only and **does not include recipients**.

Campaign statuses:

```text
1 = Scheduled
2 = Sending
3 = Completed
4 = PartiallyFailed
5 = Failed
6 = Cancelled
```

Summary fields:

- `recipientCount`
- `acceptedCount`
- `deliveredCount`
- `failedCount`

## 2.2 Get campaign

```http
GET /api/businesses/{businessId}/sms/campaigns/{campaignId}?includeRecipients=true
```

`includeRecipients` defaults to `true`.

Recipient fields include customer, mobile, rendered message, provider message ID, submission/delivery timestamps, and failure reason.

Recipient statuses:

```text
1 = Pending
2 = Submitted
3 = Delivered
4 = Failed
```

**Important:** `Submitted` means the provider accepted/submitted the SMS. It does not mean delivery to the handset. Only `Delivered` represents delivery.

## 2.3 Create campaign

```http
POST /api/businesses/{businessId}/sms/campaigns
Content-Type: application/json
```

```json
{
  "templateId": "guid-or-null",
  "name": "یادآوری مشتریان",
  "message": "سلام [نام]، زمان مراجعه بعدی شما فرا رسیده است.",
  "scheduledAt": "2026-09-06T12:00:00Z",
  "customerIds": [
    "customer-guid-1",
    "customer-guid-2"
  ]
}
```

Fields:

| Field | Type | Required | Description |
|---|---|---|---|
| `templateId` | GUID/null | No | Active template in the same business. |
| `name` | string/null | No | Optional campaign name. |
| `message` | string | Yes | SMS content, max 2000 chars. |
| `scheduledAt` | DateTime/null | No | Future UTC time. Omit for send-now. |
| `customerIds` | GUID[] | Yes | Active customers with mobile numbers; max 10,000. |

The backend validates that all selected customers belong to the business, are active, exist, and have a mobile number. A supplied template must belong to the same business and be active.

At creation time the backend snapshots customer ID, mobile number, and rendered message. Later customer/template changes do not affect the campaign.

### Send now semantics

Omitting `scheduledAt` does **not** mean the HTTP request synchronously sends all SMS messages. The campaign is created with the current UTC time and becomes eligible for the background worker.

Therefore the UI should say **«درخواست ارسال ثبت شد»** or equivalent rather than claiming immediate handset delivery.

Response: `201 Created` with the created campaign and recipients.

## 2.4 Cancel scheduled campaign

```http
POST /api/businesses/{businessId}/sms/campaigns/{campaignId}/cancel
```

Only `Scheduled` campaigns can be cancelled. A non-scheduled campaign returns `409 Conflict`.

---

# 3. Approved frontend flows

SMS must remain contextual and must not become a top-level navigation item.

## Customer Details

```text
Customer Profile
   -> ارسال SMS
   -> SMS Composer
   -> POST /campaigns
```

The Composer opens with the current customer preselected.

## Customer List

```text
Customer List
   -> select customers
   -> ارسال SMS
   -> SMS Composer
   -> POST /campaigns
```

Customer selection must support search by customer name/mobile. The existing server-side customer search API should be used; the frontend must not assume that the first 100 customers represent the complete customer set.

## Smart List

```text
Smart List
   -> ارسال SMS به این لیست
   -> audience preview
   -> SMS Composer
   -> POST /campaigns
```

The frontend resolves the Smart List to customer IDs before creating the campaign. The campaign stores a snapshot and must not depend on later changes to the Smart List.

Before opening the Composer, show the audience size, for example:

```text
موعد گذشته
42 مشتری
[ارسال SMS به 42 مشتری]
```

The Composer must allow reviewing and removing recipients before submission.

## Appointment

An appointment may expose a contextual `ارسال SMS` action for its customer. It opens the same Composer with that customer preselected.

## Settings / Templates

Template management uses the four template endpoints. Campaign history should be presented as a secondary SMS management view under Settings; SMS is not added to the main navigation.

---

# 4. SMS Composer UX contract

The project is Persian-first, RTL, responsive, and uses Persian date/time controls. The Composer must follow the same visual and interaction conventions as the existing CRM pages.

## 4.1 Recipient section

Two modes are supported:

### Contextual recipient

```text
مشتری
علی رضایی   0912...
```

The recipient is already selected.

### Bulk recipient

```text
گیرندگان: 42 مشتری
```

The user can search, add, review, and remove recipients before creating the campaign.

Show a clear validation message if there are no recipients or if the audience exceeds 10,000.

## 4.2 Template section

Provide an active-template selector. Selecting a template fills the message editor, but the message remains editable.

## 4.3 Message editor

Provide:

- multiline message input;
- variable insertion controls;
- live character count;
- maximum 2000-character validation;
- RTL/Persian layout.

Suggested variable controls:

```text
[+ نام] [+ نام خانوادگی] [+ نام کامل] [+ نام کسب‌وکار]
```

## 4.4 Preview

Preview is client-side because the backend does not expose a preview endpoint.

For a single recipient, show resolved data:

```text
متن:
سلام [نام] عزیز، وقت مراجعه بعدی شما فرا رسیده است.

پیش‌نمایش:
سلام علی عزیز، وقت مراجعه بعدی شما فرا رسیده است.
```

For bulk recipients, show a small representative preview (for example up to three recipients) and make clear that the campaign will snapshot the rendered message for each recipient.

## 4.5 Send now / Schedule

Use an explicit choice:

```text
○ ارسال فوری
● زمان‌بندی ارسال
```

When scheduling, use the project's Persian date/time picker. The selected value is converted to UTC ISO 8601 before sending to the API.

For send-now, omit `scheduledAt`.

## 4.6 Submit result

After `POST /campaigns`, show the resulting campaign status. Do not claim that SMS is already delivered.

Example:

```text
درخواست ارسال ثبت شد
وضعیت: زمان‌بندی شده
```

The user should be able to navigate to campaign details/history.

---

# 5. Status presentation

Use Persian labels in UI while reading/sending numeric enum values.

### Campaign

| API value | UI label |
|---:|---|
| 1 | زمان‌بندی شده |
| 2 | در حال ارسال |
| 3 | ارسال شده |
| 4 | بخشی ناموفق |
| 5 | ناموفق |
| 6 | لغو شده |

### Recipient

| API value | UI label |
|---:|---|
| 1 | در انتظار |
| 2 | تحویل به سرویس‌دهنده |
| 3 | تحویل شده |
| 4 | ناموفق |

Never display `Submitted` as `Delivered`.

---

# 6. Background sending

The backend contains a hosted SMS worker. It looks for campaigns where:

```text
Status == Scheduled
AND ScheduledAt <= current UTC time
```

Current development settings:

```json
{
  "Sms": {
    "PollingIntervalSeconds": 10
  }
}
```

Current batch limits:

- maximum 10 due campaigns per polling cycle;
- maximum 100 recipients per provider batch.

The current provider is a logging/simulation provider. It does not send real SMS and returns accepted/submitted development results with generated provider message IDs.

The frontend must not hard-code the 10-second polling value or imply that it is a delivery SLA.

---

# 7. Performance and data-loading rules

Campaign List is a summary endpoint. It must not load all recipient entities for the requested page merely to calculate counts. Recipient details are loaded only by the campaign detail endpoint when requested.

This keeps large campaigns (up to 10,000 recipients) from unnecessarily inflating list queries.

---

# 8. Current limitations / not implemented

The frontend must not invent endpoints for capabilities not implemented by the backend:

- Edit an existing scheduled campaign: **not implemented**.
- Real SMS provider: **not implemented; development logging provider is active**.
- Delivery webhook: **not implemented**.
- Retry failed recipients: **not implemented**.
- Backend SMS segment calculation: **not implemented**.
- Draft campaign state: **not implemented**.

Current campaign write operations are:

```text
Create
Cancel (Scheduled only)
```

---

# 9. API summary

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/api/businesses/{businessId}/sms/templates` | List templates |
| GET | `/api/businesses/{businessId}/sms/templates/{templateId}` | Get template |
| POST | `/api/businesses/{businessId}/sms/templates` | Create template |
| PUT | `/api/businesses/{businessId}/sms/templates/{templateId}` | Update template |
| GET | `/api/businesses/{businessId}/sms/campaigns` | List campaigns |
| GET | `/api/businesses/{businessId}/sms/campaigns/{campaignId}` | Get campaign |
| POST | `/api/businesses/{businessId}/sms/campaigns` | Create/send/schedule campaign |
| POST | `/api/businesses/{businessId}/sms/campaigns/{campaignId}/cancel` | Cancel scheduled campaign |
