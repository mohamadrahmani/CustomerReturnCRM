using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize(Policy = "CentralAdmin")]
[Route("api/central-admin")]
public sealed class CentralAdminController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("businesses")]
    public async Task<ActionResult<IReadOnlyList<CentralAdminBusinessDto>>> ListBusinesses(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var rows = await db.Businesses.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id, x.Name, x.BusinessType, x.Mobile, x.City, x.IsActive,
                x.LicenseIsActive, x.LicenseStartDate, x.LicenseEndDate,
                x.LicenseAdminNote, x.CreatedAt
            }).ToListAsync(cancellationToken);

        var owners = await (from member in db.BusinessMembers.AsNoTracking()
                            join user in db.Users.AsNoTracking() on member.UserId equals user.Id
                            orderby member.CreatedAt
                            select new { member.BusinessId, user.PhoneNumber })
            .ToListAsync(cancellationToken);
        var ownerMap = owners.GroupBy(x => x.BusinessId)
            .ToDictionary(x => x.Key, x => x.First().PhoneNumber);

        return Ok(rows.Select(x => new CentralAdminBusinessDto(
            x.Id, x.Name, x.BusinessType, x.Mobile, x.City,
            ownerMap.GetValueOrDefault(x.Id), x.IsActive, x.LicenseIsActive,
            x.LicenseStartDate, x.LicenseEndDate, x.LicenseAdminNote, x.CreatedAt,
            IsLicenseValid(x.LicenseIsActive, x.LicenseStartDate, x.LicenseEndDate, today)))
            .ToList());
    }

    [HttpPut("businesses/{businessId:guid}/license")]
    public async Task<ActionResult<CentralAdminBusinessDto>> UpdateLicense(
        Guid businessId,
        UpdateBusinessLicenseRequest request,
        CancellationToken cancellationToken)
    {
        if (request.LicenseStartDate.HasValue && request.LicenseEndDate.HasValue &&
            request.LicenseStartDate.Value.Date > request.LicenseEndDate.Value.Date)
            return BadRequest(new { error = "تاریخ پایان اعتبار نمی‌تواند قبل از تاریخ شروع باشد." });

        var business = await db.Businesses.FirstOrDefaultAsync(x => x.Id == businessId, cancellationToken);
        if (business is null) return NotFound(new { error = "کسب‌وکار پیدا نشد." });

        business.LicenseIsActive = request.LicenseIsActive;
        business.LicenseStartDate = request.LicenseStartDate?.Date;
        business.LicenseEndDate = request.LicenseEndDate?.Date;
        business.LicenseAdminNote = string.IsNullOrWhiteSpace(request.LicenseAdminNote)
            ? null : request.LicenseAdminNote.Trim();
        business.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var ownerMobile = await (from member in db.BusinessMembers.AsNoTracking()
                                 join user in db.Users.AsNoTracking() on member.UserId equals user.Id
                                 where member.BusinessId == businessId
                                 orderby member.CreatedAt
                                 select user.PhoneNumber).FirstOrDefaultAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        return Ok(new CentralAdminBusinessDto(
            business.Id, business.Name, business.BusinessType, business.Mobile, business.City,
            ownerMobile, business.IsActive, business.LicenseIsActive, business.LicenseStartDate,
            business.LicenseEndDate, business.LicenseAdminNote, business.CreatedAt,
            IsLicenseValid(business.LicenseIsActive, business.LicenseStartDate, business.LicenseEndDate, today)));
    }

    private static bool IsLicenseValid(bool active, DateTime? start, DateTime? end, DateTime today) =>
        active && (!start.HasValue || start.Value.Date <= today) && (!end.HasValue || end.Value.Date >= today);
}

public sealed record UpdateBusinessLicenseRequest(
    bool LicenseIsActive,
    DateTime? LicenseStartDate,
    DateTime? LicenseEndDate,
    string? LicenseAdminNote);

public sealed record CentralAdminBusinessDto(
    Guid Id, string Name, string BusinessType, string Mobile, string? City, string? OwnerMobile,
    bool IsActive, bool LicenseIsActive, DateTime? LicenseStartDate, DateTime? LicenseEndDate,
    string? LicenseAdminNote, DateTime CreatedAt, bool LicenseValid);
