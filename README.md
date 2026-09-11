# CustomerReturnCRM

CustomerReturnCRM is the backend MVP for SalonCRM: a multi-tenant CRM
for service businesses that helps them understand customer return
behavior and follow up at the right time.

The initial target market is women's beauty salons, while the domain
model remains generic enough for other appointment-based businesses.

## Core flow

```text
Customer
  -> Appointment
  -> Completed appointment
  -> Visit
  -> Visit service history
  -> Return analysis
  -> Smart list
  -> Human follow-up action
```

CI trigger verification after the SMS.ir provider dependency fix.
