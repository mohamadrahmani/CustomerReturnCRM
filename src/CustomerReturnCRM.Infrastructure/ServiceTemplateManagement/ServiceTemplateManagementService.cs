using CustomerReturnCRM.Application.ServiceTemplateManagement;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.ServiceTemplateManagement;

public sealed class ServiceTemplateManagementService : IServiceTemplateManagementService
{
    private const string GenericBusinessType = "General";
    private readonly ApplicationDbContext _dbContext;

    public ServiceTemplateManagementService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<ServiceTemplateResult>> ListAsync(
        string? businessType,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = string.IsNullOrWhiteSpace(businessType) ? null : businessType.Trim();
        return await _dbContext.ServiceTemplates.AsNoTracking()
            .Where(x => x.IsActive &&
                        (normalizedType == null || x.BusinessType == normalizedType || x.BusinessType == GenericBusinessType))
            .OrderBy(x => x.BusinessType == GenericBusinessType)
            .ThenBy(x => x.Title)
            .Select(x => new ServiceTemplateResult(
                x.Id, x.BusinessType, x.Title, x.DefaultDurationMinutes, x.SuggestedReturnDays))
            .ToListAsync(cancellationToken);
    }
}
