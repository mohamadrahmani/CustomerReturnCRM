# Central admin and license management (v1)

## Enable a central administrator

The shared login page is used for both business users and the product owner. Central-admin access is never granted by public registration. After applying the migration, grant it manually to a trusted existing account in SQL Server:

```sql
UPDATE dbo.AspNetUsers
SET IsCentralAdmin = 1
WHERE PhoneNumber = N'<ADMIN_MOBILE>';
```

Replace `<ADMIN_MOBILE>` with the exact phone number stored in `AspNetUsers.PhoneNumber`. Verify that exactly one intended account was updated. The user must sign out and sign in again so a new JWT includes the central-admin claim. To revoke access, set `IsCentralAdmin = 0` and ask the user to sign in again.

## Apply the migration

From the repository root, using the configured database connection:

```powershell
dotnet ef database update --project src/CustomerReturnCRM.Infrastructure --startup-project src/CustomerReturnCRM.API
```

Back up the database before applying migrations in production. The migration defaults existing businesses to an active license and leaves dates unset to avoid unexpectedly blocking existing customers. Configure start/end dates from the central admin panel after review.

## Behavior

- Admin route: `/admin/businesses`
- Admin APIs: `/api/central-admin/businesses` and `PUT /api/central-admin/businesses/{businessId}/license`
- A license is valid only when active and today's UTC date is within any configured start/end dates.
- Inactive/expired licenses block authenticated APIs under `/api/businesses/{businessId}`; the business data is retained.
- Authentication, the central-admin API, and public booking routes are not blocked by the license middleware.
- Bulk SMS administration is intentionally out of scope for v1.
