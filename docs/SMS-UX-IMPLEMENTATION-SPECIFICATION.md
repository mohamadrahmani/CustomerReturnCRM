# SMS Frontend UX Implementation Specification

## 1. Product position

SMS is a contextual capability of CustomerReturnCRM, not a standalone top-level domain. Do not add SMS to the main navigation.

SMS entry points:

- Customer Profile → ارسال SMS
- Customer List → انتخاب مشتری‌ها → ارسال SMS
- Smart List → ارسال SMS به این لیست
- Appointment → ارسال SMS
- Settings → مدیریت قالب‌ها و تاریخچه ارسال

All entry points reuse the same SMS Composer.

## 2. Global UI rules

The SMS UI must follow the existing CustomerReturnCRM frontend conventions:

- Persian-first UI.
- Full RTL.
- Responsive Desktop/Tablet/Mobile behavior.
- Existing card, modal, drawer, badge, toast, loading, empty and error patterns.
- Persian date/time picker for scheduled sending.
- API values remain ISO 8601/UTC.
- Numeric API enums are translated to Persian labels in UI.
- No technical terms such as tenant/snapshot should be exposed to the business user.

## 3. Composer modes

### Contextual mode

Used from Customer Profile or Appointment.

The customer is preselected and shown as a recipient summary. The user can review the recipient before sending.

### Bulk mode

Used from Customer List or Smart List.

The Composer receives a customer ID collection. It must display audience size and allow review/removal before submit.

Customer search must use the existing server-side customer search capability. Do not load only the first 100 customers and treat that as the complete searchable set.

## 4. Recipient UX

Display:

```text
گیرندگان: 42 مشتری
```

For each recipient show at minimum:

- Customer name
- Mobile

Recipients without a mobile number must not reach the create-campaign request. If such data is encountered, show a clear validation message and allow the user to remove the affected customer(s).

Maximum campaign audience is 10,000 customers.

If the audience is above the limit:

```text
امکان ارسال به بیش از ۱۰٬۰۰۰ مشتری در یک نوبت وجود ندارد.
```

For Smart Lists, show the audience size before opening the Composer.

## 5. Template UX

Template selector contains active templates only by default.

Selecting a template copies its content into the message editor. The copied message remains editable.

Template management belongs under Settings and supports:

- List active/inactive templates
- Create
- Edit
- Activate/deactivate

There is no Delete action in the current API.

## 6. Message editor

Required UI:

- Multiline editor.
- RTL text direction.
- Character counter.
- Maximum 2000 characters.
- Variable insertion controls.

Variables:

```text
[نام]
[نام خانوادگی]
[نام کامل]
[نام کسب‌وکار]
```

Use clickable controls instead of requiring the user to type variable syntax manually.

## 7. Preview

Preview is client-side. There is no backend preview endpoint.

Single-recipient preview should resolve the variables using the selected customer's data.

Bulk preview should show a small representative sample, preferably up to three recipients.

The UI must make it clear that the final campaign uses a per-recipient rendered snapshot.

## 8. Send mode

Provide an explicit choice:

```text
○ ارسال فوری
● زمان‌بندی ارسال
```

### Send now

Omit `scheduledAt` from the POST request.

The UI must not claim immediate delivery. The API creates a campaign and the background worker processes it.

Recommended success message:

```text
درخواست ارسال ثبت شد.
```

### Schedule

Show Persian date and time pickers.

The selected local date/time must be converted to UTC ISO 8601 before calling the API.

Reject past or current scheduling values in the UI before submission; the backend remains the final authority.

## 9. Submit states

The primary action must have clear disabled/loading behavior while the request is being submitted.

After successful creation:

1. Close the Composer or transition to the result state.
2. Show the created campaign status.
3. Offer navigation to campaign details/history.

Do not label a newly created campaign as delivered.

## 10. Campaign history

Campaign history is a secondary SMS management view under Settings.

List columns/cards should include:

- Campaign name (when supplied)
- Creation date/time
- Scheduled date/time
- Status
- Recipient count
- Accepted count
- Delivered count
- Failed count

Use pagination from the standard API contract.

Campaign list does not load recipient details. Open campaign detail to see recipients.

## 11. Campaign detail

Show:

- Campaign status
- Message
- Creation time
- Scheduled time
- Recipient summary counts
- Recipient list when available
- Per-recipient status
- Failure reason where available

For scheduled campaigns, show the Cancel action.

Do not show Edit because the backend does not implement campaign editing.

## 12. Status labels

### Campaign

| API value | Persian UI |
|---:|---|
| 1 | زمان‌بندی شده |
| 2 | در حال ارسال |
| 3 | ارسال شده |
| 4 | بخشی ناموفق |
| 5 | ناموفق |
| 6 | لغو شده |

### Recipient

| API value | Persian UI |
|---:|---|
| 1 | در انتظار |
| 2 | تحویل به سرویس‌دهنده |
| 3 | تحویل شده |
| 4 | ناموفق |

`Submitted` must never be presented as `Delivered`.

## 13. Responsive behavior

### Desktop

Use a centered Composer modal or appropriate wide panel with:

- Recipient section
- Template/message section
- Preview section
- Send mode and action footer

### Mobile

Use a full-height Drawer/Modal style appropriate to the existing application.

The primary submit action should remain reachable and preferably sticky at the bottom.

Avoid horizontal scrolling.

Recipient chips/cards must wrap cleanly.

Date/time controls must fit the viewport.

## 14. Required UI states

The SMS Composer and campaign/history views must provide:

- Loading state
- Empty state
- Error state
- Validation state
- Submitting state
- Success state

Examples:

```text
هنوز قالب پیامکی ثبت نشده است.
```

```text
مشتری دارای شماره موبایل معتبر برای ارسال انتخاب نشده است.
```

```text
درخواست ارسال ثبت شد.
```

## 15. API restrictions

The frontend must not invent or call endpoints for:

- Campaign editing
- Retry failed recipients
- Delivery webhook management
- Real provider configuration
- Draft campaigns
- Backend segment calculation

The current campaign write operations are only:

```text
Create
Cancel (Scheduled only)
```

## 16. Provider semantics

Development uses a logging/simulation SMS provider. It does not send real SMS.

The background worker uses the configured polling interval. The frontend must not hard-code the current 10-second value and must not treat it as a delivery SLA.

## 17. Accessibility

The Composer must support:

- Keyboard navigation.
- Visible focus states.
- Labels for all inputs.
- Accessible status text.
- Buttons with clear action labels.
- Error messages associated with the relevant fields.
- Sufficient touch target size on mobile.

## 18. Implementation order

Implement the SMS frontend in this order:

1. Shared SMS API methods/types.
2. Shared SMS Composer.
3. Contextual Send SMS action on Customer Profile.
4. Bulk selection and Send SMS on Customer List.
5. Smart List Send SMS action.
6. Appointment contextual Send SMS action.
7. Settings → SMS Templates.
8. Settings → Campaign History and Campaign Detail.
9. Responsive/mobile refinement.
10. Loading/empty/error/success/accessibility QA.
