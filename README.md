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

Backend:
- .NET 9 / ASP.NET Core Web API
- Entity Framework Core 9
- SQL Server
- ASP.NET Core Identity
- JWT authentication
- Lightweight Clean Architecture

Repository structure:

```text
src/
  CustomerReturnCRM.Domain
  CustomerReturnCRM.Application
  CustomerReturnCRM.Infrastructure
  CustomerReturnCRM.API
  CustomerReturnCRM.Web        # Next.js frontend
```

`CustomerReturnCRM.Web` is an independent Next.js application in the
same repository. It is not a .NET project and therefore is intentionally
not added to `CustomerReturnCRM.sln`.

Frontend stack:
- Next.js 15 / React 19
- TypeScript
- Tailwind CSS
- TanStack React Query
- React Hook Form
- RTL / Persian-first UI

`Business` is the tenant. Operational records are scoped to a business,
and a user can belong to more than one business.

## Prerequisites

Backend:
- .NET SDK 9
- SQL Server LocalDB (the default configuration uses
  `(localdb)\\mssqllocaldb`)
- Entity Framework Core CLI

Frontend:
- Node.js 20+
- npm

## Configuration

The default connection string and JWT settings are in
`src/CustomerReturnCRM.API/appsettings.json`.

The frontend API base URL is configured with
`src/CustomerReturnCRM.Web/.env.local` using the variable:

```text
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
```

See `src/CustomerReturnCRM.Web/README.md` for frontend setup and the
implementation sequence.

## Run locally

Backend, from the repository root:

```powershell
dotnet restore CustomerReturnCRM.sln
dotnet ef database update `
  --project src/CustomerReturnCRM.Infrastructure/CustomerReturnCRM.Infrastructure.csproj `
  --startup-project src/CustomerReturnCRM.API/CustomerReturnCRM.API.csproj
dotnet run --project src/CustomerReturnCRM.API/CustomerReturnCRM.API.csproj
```

Frontend, from `src/CustomerReturnCRM.Web`:

```bash
npm install
npm run dev
```

The API uses the launch settings configured under
`src/CustomerReturnCRM.API/Properties/launchSettings.json`.

After starting the API, interactive API documentation is available at:

```text
http://localhost:5108/swagger
```

## Authentication and onboarding

1. `POST /api/auth/register`
2. `POST /api/auth/login`
3. Create a business with `POST /api/businesses`
4. Use the returned business ID for business-scoped endpoints.

The login response contains the JWT, expiry and the user's businesses.
The frontend authentication client is based on this actual API contract.

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
the first service.

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
- Initial Next.js frontend foundation under `src/CustomerReturnCRM.Web`

Not implemented yet:

- SMS and automatic messaging
- Payments, accounting and inventory
- Branches, online booking and mobile application
- Advanced permissions
- Complete frontend module implementation

## Development notes

Keep business data scoped by `BusinessId`, preserve historical snapshots
in appointment and visit services, and avoid adding features outside the
MVP without an explicit product decision.
