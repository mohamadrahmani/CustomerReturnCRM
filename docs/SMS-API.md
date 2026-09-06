# SMS API Contract

## Purpose

This document describes the SMS backend API currently implemented for the CustomerReturnCRM frontend.

The API is business-scoped and requires authentication. The frontend should treat SMS as an action/capability available from Customers, Smart Lists, Appointments, and Settings rather than as an isolated business domain in the navigation.

## Base URL

Development:

```text
http://localhost:5108
```

API base path:

```text
/api/businesses/{businessId}/sms
```

All endpoints require a valid authenticated user (`[Authorize]`). The backend identifies the current user from the JWT `NameIdentifier` claim or `sub` claim.

---

## Common conventions

### Authentication

Send the JWT as:

```http
Authorization: Bearer <token>
```

### Business scope

`businessId` is a required route parameter on every endpoint. The authenticated user must be a member of that business.

If the user is not authenticated: `401 Unauthorized`.

If the user is authenticated but is not a member of the business: `403 Forbidden`.

### Date/time

`DateTime` values should be sent/handled as UTC. `ScheduledAt` must be in the future when supplied.

### SMS variables

Campaign message rendering currently supports these exact variables:

```text
[نام]
[نام خانوادگی]
[نام کامل]
[نام کسب‌وکار]
```

Variables are resolved when the campaign is created. The rendered message is stored on each recipient, so later template/customer changes do not alter an existing campaign.

Maximum campaign message length: **2000 characters**.

Maximum recipients per campaign: **10,000**.

---

# 1. Templates

Templates are reusable SMS message definitions belonging to a business.

## 1.1 List templates

```http
GET /api/businesses/{businessId}/sms/templates?activeOnly=true
```

### Query

| Parameter | Type | Default | Description |
|---|---|---:|---|
| `activeOnly` | boolean | `true` | When true, only active templates are returned. |

### Response `200 OK`

```json
[
  {
    "id": "guid",
    "businessId": "guid",
    "name": "یادآوری مراجعه",
    "content": "سلام [نام]، زمان مراجعه شما فرا رسیده است.",
    "isActive": true,
    "createdAt": "2026-09-06T10:00:00Z",
    "updatedAt": null
  }
]
```

---

## 1.2 Get template

```http
GET /api/businesses/{businessId}/sms/templates/{templateId}
```

### Response

`200 OK` with one `SmsTemplateResult`, or `404 Not Found` when the template does not exist in the business.

---

## 1.3 Create template

```http
POST /api/businesses/{businessId}/sms/templates
Content-Type: application/json
```

### Request

```json
{
  "name": "یادآوری مراجعه",
  "content": "سلام [نام]، زمان مراجعه بعدی شما فرا رسیده است."
}
```

### Rules

- `name` is required.
- `content` is required.
- Template name must be unique within the business.
- New templates are active by default.

### Responses

- `201 Created`
- `400 Bad Request` for validation/business-rule errors.
- `403 Forbidden` when the user is not a business member.

---

## 1.4 Update template

```http
PUT /api/businesses/{businessId}/sms/templates/{templateId}
Content-Type: application/json
```

### Request

```json
{
  "name": "یادآوری مراجعه",
  "content": "سلام [نام]، زمان مراجعه بعدی شما فرا رسیده است.",
  "isActive": true
}
```

### Responses

- `200 OK`
- `404 Not Found`
- `400 Bad Request`
- `403 Forbidden`

---

# 2. Campaigns

A campaign is the actual SMS sending operation. The campaign stores a snapshot of recipients' mobile numbers and rendered messages.

## 2.1 List campaigns

```http
GET /api/businesses/{businessId}/sms/campaigns
```

### Query parameters

| Parameter | Type | Default | Description |
|---|---|---:|---|
| `status` | integer/enum | null | Optional campaign status filter. |
| `page` | integer | `1` | Page number. |
| `pageSize` | integer | `20` | Page size; normalized by the backend pagination helper. |

### Campaign statuses

```text
1 = Scheduled
2 = Sending
3 = Completed
4 = PartiallyFailed
5 = Failed
6 = Cancelled
```

### Response

The endpoint returns the project's standard `PagedResult<SmsCampaignResult>` shape. The frontend should use the actual `items`, `page`, `pageSize`, `totalCount` fields returned by the common pagination contract already used by the API.

Each campaign item contains summary counts:

- `recipientCount`
- `acceptedCount`
- `deliveredCount`
- `failedCount`

Recipients are not included in the list response.

---

## 2.2 Get campaign

```http
GET /api/businesses/{businessId}/sms/campaigns/{campaignId}?includeRecipients=true
```

### Query

| Parameter | Type | Default | Description |
|---|---|---:|---|
| `includeRecipients` | boolean | `true` | Include recipient details in the response. |

### Response

```json
{
  "id": "guid",
  "businessId": "guid",
  "templateId": "guid-or-null",
  "createdByUserId": "guid",
  "name": "یادآوری مشتریان",
  "message": "سلام [نام]، زمان مراجعه شما فرا رسیده است.",
  "scheduledAt": "2026-09-06T12:00:00Z",
  "status": 1,
  "startedAt": null,
  "completedAt": null,
  "cancelledAt": null,
  "createdAt": "2026-09-06T11:00:00Z",
  "updatedAt": null,
  "recipientCount": 2,
  "acceptedCount": 0,
  "deliveredCount": 0,
  "failedCount": 0,
  "recipients": [
    {
      "id": "guid",
      "customerId": "guid",
      "customerName": "علی رضایی",
      "mobile": "09xxxxxxxxx",
      "renderedMessage": "سلام علی، زمان مراجعه شما فرا رسیده است.",
      "status": 1,
      "providerMessageId": null,
      "submittedAt": null,
      "deliveredAt": null,
      "failureReason": null
    }
  ]
}
```

### Recipient statuses

```text
1 = Pending
2 = Submitted
3 = Delivered
4 = Failed
```

Important: `Submitted` means the provider accepted/submitted the message. It does **not** mean the SMS has reached the handset. `Delivered` is the later delivery state and will be supported by the real provider/webhook integration.

---

## 2.3 Create campaign

```http
POST /api/businesses/{businessId}/sms/campaigns
Content-Type: application/json
```

### Request

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

### Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `templateId` | GUID/null | No | Active template belonging to the same business. |
| `name` | string/null | No | Optional campaign name. |
| `message` | string | Yes | SMS content; max 2000 characters. |
| `scheduledAt` | DateTime/null | No | Future UTC time. If omitted, campaign is eligible for immediate background processing. |
| `customerIds` | GUID[] | Yes | One or more customers; max 10,000. |

### Backend validation

The selected customers must:

- belong to the specified business;
- be active;
- exist;
- have a mobile number.

The template, when supplied, must belong to the same business and be active.

### Important behavior

At creation time the backend snapshots:

- customer ID;
- mobile number;
- rendered message;
- campaign message/template context.

Therefore the frontend should not expect a later customer/template edit to modify an already-created campaign.

### Response

`201 Created` with the complete `SmsCampaignResult`, including recipients.

### Errors

`400 Bad Request` can be returned for validation/business rules, for example:

```json
{
  "error": "At least one customer is required."
}
```

or:

```json
{
  "error": "All selected customers must have a mobile number."
}
```

---

## 2.4 Cancel scheduled campaign

```http
POST /api/businesses/{businessId}/sms/campaigns/{campaignId}/cancel
```

No request body is required.

### Behavior

Only campaigns in `Scheduled` status can be cancelled.

### Responses

- `200 OK` with updated campaign.
- `404 Not Found` if the campaign does not exist in the business.
- `409 Conflict` if the campaign is no longer scheduled.
- `403 Forbidden` if the user is not a member of the business.

Example conflict:

```json
{
  "error": "Only scheduled campaigns can be cancelled."
}
```

---

# 3. Frontend flows

The backend is intentionally designed so the UI can reuse one SMS Composer.

## Customer Details

```text
Customer Details
   -> Send SMS
   -> SMS Composer
   -> POST campaigns
```

## Customer List

```text
Customer List
   -> select customers
   -> Send SMS
   -> SMS Composer
   -> POST campaigns
```

## Smart List

```text
Smart List
   -> Send SMS to this list
   -> SMS Composer
   -> POST campaigns
```

The selected Smart List audience should be resolved to customer IDs by the frontend before calling `POST campaigns`. The campaign becomes a snapshot and subsequent Smart List changes do not affect it.

## Appointment

The frontend can provide a contextual `Send SMS` action from an appointment and pass the relevant customer ID into the same Composer.

## Settings / Templates

Template management should use the four template endpoints under `/templates`.

---

# 4. Composer behavior expected by the API

The Composer should support:

1. Recipient selection/list preview.
2. Template selection.
3. Message editing after selecting a template.
4. Supported variable insertion:
   - `[نام]`
   - `[نام خانوادگی]`
   - `[نام کامل]`
   - `[نام کسب‌وکار]`
5. Preview with resolved customer data where applicable.
6. Character/segment counter can be implemented client-side; the backend currently enforces a 2000-character maximum but does not expose SMS segment calculation.
7. Send now by omitting `scheduledAt`.
8. Schedule by sending a future UTC `scheduledAt`.
9. After creation, navigate/show the resulting campaign status.

---

# 5. Background sending behavior

The backend contains a hosted SMS worker.

It periodically searches for:

```text
Status == Scheduled
AND ScheduledAt <= current UTC time
```

It changes the campaign to `Sending`, sends pending recipients in batches, and then determines the final campaign status.

Current batch sizes:

- maximum 10 due campaigns per polling cycle;
- maximum 100 recipients per provider batch.

The polling interval is configured in Development configuration:

```json
{
  "Sms": {
    "PollingIntervalSeconds": 10
  }
}
```

The current development provider is a logging/simulation provider. It does **not** send real SMS. It returns accepted/submitted results with generated development provider message IDs.

---

# 6. Current limitations / not yet implemented

The frontend agent must not invent endpoints for these capabilities yet:

- Edit an existing scheduled campaign: **not implemented**.
- Real SMS provider integration: **not implemented; development logging provider is active**.
- Delivery-status webhook: **not implemented yet**.
- Retry failed recipients: **not implemented**.
- SMS segment calculation from the backend: **not implemented**.
- Draft campaign state: **not implemented**.

The currently available campaign write operations are therefore only:

```text
Create
Cancel (Scheduled only)
```

---

# 7. Recommended frontend status presentation

Use human-readable Persian labels in the UI, while sending/reading the numeric enum values from the API.

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

Do not label `Submitted` as `Delivered`.

---

# 8. API summary

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

This is the complete SMS API surface currently implemented by the backend.
