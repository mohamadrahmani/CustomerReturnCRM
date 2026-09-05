using CustomerReturnCRM.Application.ServiceManagement;
using CustomerReturnCRM.Application.Common;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.ServiceManagement;

public sealed class ServiceManagementService : IServiceManagementService
{
    private readonly ApplicationDbContext _dbContext;

    public ServiceManagementService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<ServiceResult>> ListAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, string? search = null, bool? isActive = true, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = _dbContext.Services.AsNoTracking().Where(x => x.BusinessId == businessId);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        var normalizedSearch = search?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
            query = query.Where(x => x.Title.Contains(normalizedSearch) || (x.Description != null && x.Description.Contains(normalizedSearch)));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Title).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => ToResult(x)).ToListAsync(cancellationToken);
        return Pagination.Create(items, page, pageSize, total);
    }

    public async Task<ServiceResult?> GetAsync(Guid businessId, Guid serviceId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        return await _dbContext.Services.AsNoTracking().Where(x => x.BusinessId == businessId && x.Id == serviceId).Select(x => ToResult(x)).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult> CreateAsync(Guid businessId, Guid userId, CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        Validate(request.Title, request.DefaultPrice, request.DefaultDurationMinutes, request.SuggestedReturnDays);
        var title = request.Title.Trim();
        if (await _dbContext.Services.AnyAsync(x => x.BusinessId == businessId && x.Title == title, cancellationToken)) throw new InvalidOperationException("A service with this title already exists in the business.");
        var service = new Service { Id = Guid.NewGuid(), BusinessId = businessId, Title = title, Description = Normalize(request.Description), DefaultPrice = request.DefaultPrice, DefaultDurationMinutes = request.DefaultDurationMinutes, SuggestedReturnDays = request.SuggestedReturnDays, IsActive = true, CreatedAt = DateTime.UtcNow };
        _dbContext.Services.Add(service);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResult(service);
    }

    public async Task<ServiceResult?> UpdateAsync(Guid businessId, Guid serviceId, Guid userId, UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        Validate(request.Title, request.DefaultPrice, request.DefaultDurationMinutes, request.SuggestedReturnDays);
        var service = await _dbContext.Services.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == serviceId, cancellationToken);
        if (service is null) return null;
        var title = request.Title.Trim();
        if (await _dbContext.Services.AnyAsync(x => x.BusinessId == businessId && x.Title == title && x.Id != serviceId, cancellationToken)) throw new InvalidOperationException("A service with this title already exists in the business.");
        service.Title = title; service.Description = Normalize(request.Description); service.DefaultPrice = request.DefaultPrice; service.DefaultDurationMinutes = request.DefaultDurationMinutes; service.SuggestedReturnDays = request.SuggestedReturnDays; service.IsActive = request.IsActive; service.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResult(service);
    }

    public async Task<bool> DeactivateAsync(Guid businessId, Guid serviceId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var service = await _dbContext.Services.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == serviceId, cancellationToken);
        if (service is null) return false;
        service.IsActive = false; service.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureMemberAsync(Guid businessId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.BusinessMembers.AnyAsync(x => x.BusinessId == businessId && x.UserId == userId, cancellationToken)) throw new UnauthorizedAccessException("The user is not a member of this business.");
    }

    private static void Validate(string title, decimal price, int duration, int? returnDays) { if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required."); if (price < 0) throw new ArgumentException("Default price cannot be negative."); if (duration <= 0) throw new ArgumentException("Default duration must be greater than zero."); if (returnDays is < 0) throw new ArgumentException("Suggested return days cannot be negative."); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ServiceResult ToResult(Service x) => new(x.Id, x.BusinessId, x.Title, x.Description, x.DefaultPrice, x.DefaultDurationMinutes, x.SuggestedReturnDays, x.IsActive, x.CreatedAt, x.UpdatedAt);
}
