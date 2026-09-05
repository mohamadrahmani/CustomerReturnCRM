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
NEXT_PUBLIC_API_BASE_URL=http://localhost:5108
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

## CI

GitHub Actions validates both applications on pushes and pull requests
to `main`:

- Backend: restore and Release build with .NET 9.
- Frontend: install dependencies and run the Next.js production build
  with Node.js 20.

The workflow is defined in `.github/workflows/ci.yml`.

## Authentication and onboarding

1. `POST /api/auth/register`
2. `POST /api/auth/login`
3. The login response contains the JWT and the user's businesses.
4. The frontend uses the selected business as the active tenant context.
5. If the user has no business, onboarding creates the first business.
