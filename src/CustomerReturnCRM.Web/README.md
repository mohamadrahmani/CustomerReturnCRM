# CustomerReturnCRM.Web

Frontend application for CustomerReturnCRM.

## Stack

- Next.js 15
- React 19
- TypeScript
- Tailwind CSS
- TanStack React Query
- React Hook Form
- RTL / Persian-first UI

## Run

From this directory:

```bash
npm install
npm run dev
```

Set `NEXT_PUBLIC_API_BASE_URL` in `.env.local` to the API base URL. The
`.env.example` file contains the local default.

## Architecture direction

The frontend is intentionally independent from the ASP.NET Core project,
but lives in the same repository under `src/CustomerReturnCRM.Web`.

The API remains the system of record. Business-scoped query keys should
include `businessId`, and business switching must invalidate or refresh
business-scoped queries.

Authentication is currently wired to the real `/api/auth/login` contract.
The temporary browser `sessionStorage` persistence in the initial shell is
not the final production token strategy; it will be replaced as the
authenticated app shell and API authorization boundary are implemented.

## Implementation order

1. App shell and authentication boundary
2. Business selector / onboarding
3. Dashboard
4. Customers and customer profile
5. Services and staff
6. Appointments and calendar
7. Visits
8. Return analysis and smart lists
9. Reminders
10. Loading, empty, error, responsive and accessibility states
