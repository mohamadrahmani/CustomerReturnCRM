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

An `Appointment` represents planned work. A `Visit` represents work
that actually happened and can also be created directly for walk-in
customers.

## Technology and structure

- .NET 9 / ASP.NET Core Web API
- Entity Framework Core 9
- SQL Server
- ASP.NET Core Identity
- JWT authentication
- Lightweight Clean Architecture

```text
src/
  CustomerReturnCRM.Domain
  CustomerReturnCRM.Application
  CustomerReturnCRM.Infrastructure
  CustomerReturnCRM.API
```

`Business` is the tenant. Operational records are scoped to a business,
and a user can belong to more than one business.

## Prerequisites

- .NET SDK 9
- SQL Server LocalDB (the default configuration uses
  `(localdb)\mssqllocaldb`)
- Entity Framework Core CLI:

```powershell
dotnet tool install --global dotnet-ef
```

## Configuration

The default connection string and JWT settings are in
`src/CustomerReturnCRM.API/appsettings.json`.

The development JWT key is intentionally a placeholder and must be
replaced before using the application outside local development.

Return-analysis thresholds can be configured in the same file:

- `ReturnAnalysis:DueSoonDays` (default: 7)
- `ReturnAnalysis:AtRiskDays` (default: 30)
- `ReturnAnalysis:NoRecentVisitDays` (default: 90)

## Run locally

From the repository root:

```powershell
dotnet restore CustomerReturnCRM.sln
dotnet ef database update `
  --project src/CustomerReturnCRM.Infrastructure/CustomerReturnCRM.Infrastructure.csproj `
  --startup-project src/CustomerReturnCRM.API/CustomerReturnCRM.API.csproj
dotnet run --project src/CustomerReturnCRM.API/CustomerReturnCRM.API.csproj
```

The API uses the launch settings configured under
`src/CustomerReturnCRM.API/Properties/launchSettings.json`.

After starting the API, interactive API documentation is available at:

```text
http://localhost:5108/swagger
```

Use the `Authorize` button in Swagger UI and enter the JWT returned by
`/api/auth/login` as a Bearer token.

## Authentication and onboarding

1. `POST /api/auth/register`
2. `POST /api/auth/login`
3. Create a business with `POST /api/businesses`
4. Use the returned business ID for business-scoped endpoints.

Send the JWT returned by registration or login as:

```text
Authorization: Bearer <token>
```

## Main API areas

```text
POST /api/auth/register
POST /api/auth/login

POST /api/businesses
GET  /api/service-templates?businessType=BeautySalon

/api/businesses/{businessId}/customers
/api/businesses/{businessId}/services
/api/businesses/{businessId}/staff
/api/businesses/{businessId}/appointments
/api/businesses/{businessId}/visits

POST /api/businesses/{businessId}/appointments/{appointmentId}/complete

GET /api/businesses/{businessId}/return-analysis/customers/{customerId}

GET /api/businesses/{businessId}/smart-lists/overdue
GET /api/businesses/{businessId}/smart-lists/due-soon
GET /api/businesses/{businessId}/smart-lists/at-risk
GET /api/businesses/{businessId}/smart-lists/no-recent-visit

POST /api/businesses/{businessId}/smart-lists/dismiss
POST /api/businesses/{businessId}/smart-lists/restore

GET  /api/businesses/{businessId}/reminders
GET  /api/businesses/{businessId}/reminders/{reminderId}
POST /api/businesses/{businessId}/reminders
POST /api/businesses/{businessId}/reminders/{reminderId}/complete
POST /api/businesses/{businessId}/reminders/{reminderId}/cancel

GET  /api/businesses/{businessId}/dashboard
```

`POST /api/businesses` accepts an optional `serviceTemplateId`. When it
is provided, the selected template is copied into the new business as
the first service. The copied service starts with price `0` and can be
edited afterwards.

`GET /api/service-templates?businessType=...` returns active templates
for the requested business type plus active `General` templates. The
endpoint is read-only and templates are used only during onboarding;
the selected template is copied into the business as a normal service.

## Customer list summary

Customer list and detail responses include the following visit summary
fields in addition to the customer's stored profile data:

```text
LastVisitDate
TotalVisits
```

`LastVisitDate` is the most recent actual `Visit.VisitAt` for the customer
within the current business, and `TotalVisits` counts actual visits for
that customer within the current business. Customers with no visits have
`LastVisitDate = null` and `TotalVisits = 0`.

These values are query-derived and do not introduce duplicated summary
columns or state into the `Customer` entity.

## Return analysis rules

Expected return dates are calculated dynamically from the latest
`VisitService` for each customer and service:

```text
Last visit date + SuggestedReturnDays = Expected return date
```

Expected returns and smart lists are queries, not stored entities.
Future appointments suppress the corresponding smart-list item.

Users can manually remove a current opportunity from a smart list with
`POST /smart-lists/dismiss`. The request identifies the list type,
customer and, for service-based lists, service:

```json
{
  "smartListType": "Overdue",
  "customerId": "00000000-0000-0000-0000-000000000000",
  "serviceId": "00000000-0000-0000-0000-000000000000"
}
```

Use `POST /smart-lists/restore` with the same payload to show the item
again. A dismissal is tied to the current visit/return cycle; a newer
visit creates a new analysis cycle and is not hidden by the old
dismissal.

## Database migrations

Migrations live in
`src/CustomerReturnCRM.Infrastructure/Persistence/Migrations`.

After model changes, create a migration with:

```powershell
dotnet ef migrations add <MigrationName> `
  --project src/CustomerReturnCRM.Infrastructure/CustomerReturnCRM.Infrastructure.csproj `
  --startup-project src/CustomerReturnCRM.API/CustomerReturnCRM.API.csproj `
  --output-dir Persistence/Migrations
```

The `AddVisits` migration creates the `Visits` and `VisitServices`
tables required by the visit and return-analysis features.

The `AddSmartListDismissals` migration creates the persistence required
for manual smart-list dismissal and restoration.

The `AddReminders` migration creates the `Reminders` table required for
manual follow-up tasks.

The `AddServiceTemplates` migration creates the service-template catalog
and seeds the initial `General` and `BeautySalon` templates used by
onboarding.

## Current MVP scope

Implemented:

- Registration, login and JWT authentication
- Business setup with owner membership and staff
- Service templates for onboarding, including initial seeded templates
- Customer and service management
- Customer list visit summary (`LastVisitDate`, `TotalVisits`)
- Appointment management
- Appointment completion into a visit
- Direct walk-in visit creation
- Return analysis and four smart lists
- Manual smart-list dismissal and restoration
- Reminder creation, listing, completion and cancellation
- Action-oriented dashboard query

Not implemented yet:

- SMS and automatic messaging
- Payments, accounting and inventory
- Branches, online booking and mobile application
- Advanced permissions

## Development notes

Keep business data scoped by `BusinessId`, preserve historical snapshots
in appointment and visit services, and avoid adding features outside the
MVP without an explicit product decision.
