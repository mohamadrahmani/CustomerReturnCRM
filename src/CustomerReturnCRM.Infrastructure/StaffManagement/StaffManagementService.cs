using CustomerReturnCRM.Application.StaffManagement;
using CustomerReturnCRM.Application.Common;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.StaffManagement;

public sealed class StaffManagementService : IStaffManagementService
{
    private readonly ApplicationDbContext _dbContext;
    public StaffManagementService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<StaffResult>> ListAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, string? search = null, bool? isActive = true, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = _dbContext.Staff.AsNoTracking().Where(x => x.BusinessId == businessId);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        var normalizedSearch = search?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch)) query = query.Where(x => x.FirstName.Contains(normalizedSearch) || x.LastName.Contains(normalizedSearch) || (x.Mobile != null && x.Mobile.Contains(normalizedSearch)));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => ToResult(x)).ToListAsync(cancellationToken);
        return Pagination.Create(items, page, pageSize, total);
    }

    public async Task<StaffResult?> GetAsync(Guid businessId, Guid staffId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        return await _dbContext.Staff.AsNoTracking().Where(x => x.BusinessId == businessId && x.Id == staffId).Select(x => ToResult(x)).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<StaffResult> CreateAsync(Guid businessId, Guid userId, CreateStaffRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        Validate(request.FirstName, request.LastName);
        var mobile = Normalize(request.Mobile);
        await EnsureMobileIsUniqueAsync(businessId, mobile, null, cancellationToken);
        var staff = new Staff { Id = Guid.NewGuid(), BusinessId = businessId, FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Mobile = mobile, IsActive = true, CreatedAt = DateTime.UtcNow };
        _dbContext.Staff.Add(staff);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResult(staff);
    }

    public async Task<StaffResult?> UpdateAsync(Guid businessId, Guid staffId, Guid userId, UpdateStaffRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        Validate(request.FirstName, request.LastName);
        var staff = await _dbContext.Staff.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == staffId, cancellationToken);
        if (staff is null) return null;
        var mobile = Normalize(request.Mobile);
        await EnsureMobileIsUniqueAsync(businessId, mobile, staffId, cancellationToken);
        staff.FirstName = request.FirstName.Trim(); staff.LastName = request.LastName.Trim(); staff.Mobile = mobile; staff.IsActive = request.IsActive; staff.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResult(staff);
    }

    public async Task<bool> DeactivateAsync(Guid businessId, Guid staffId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var staff = await _dbContext.Staff.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == staffId, cancellationToken);
        if (staff is null) return false;
        staff.IsActive = false; staff.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureMobileIsUniqueAsync(Guid businessId, string? mobile, Guid? exceptStaffId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mobile)) return;
        var exists = await _dbContext.Staff.AnyAsync(x => x.BusinessId == businessId && x.Mobile == mobile && (!exceptStaffId.HasValue || x.Id != exceptStaffId.Value), cancellationToken);
        if (exists) throw new ArgumentException("این شماره موبایل قبلاً برای یک کارمند در این کسب‌وکار ثبت شده است.");
    }

    private async Task EnsureMemberAsync(Guid businessId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.BusinessMembers.AnyAsync(x => x.BusinessId == businessId && x.UserId == userId, cancellationToken)) throw new UnauthorizedAccessException("The user is not a member of this business.");
    }

    private static void Validate(string firstName, string lastName) { if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required."); if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name is required."); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static StaffResult ToResult(Staff x) => new(x.Id, x.BusinessId, x.FirstName, x.LastName, x.Mobile, x.UserId, x.IsActive, x.CreatedAt, x.UpdatedAt);
}
