using CustomerReturnCRM.Application.Common;

namespace CustomerReturnCRM.Application.ServiceManagement;

public sealed class CreateServiceRequest
{
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public decimal DefaultPrice { get; init; }
    public int DefaultDurationMinutes { get; init; }
    public int? SuggestedReturnDays { get; init; }
}

public sealed class UpdateServiceRequest
{
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public decimal DefaultPrice { get; init; }
    public int DefaultDurationMinutes { get; init; }
    public int? SuggestedReturnDays { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record ServiceResult(Guid Id, Guid BusinessId, string Title, string? Description, decimal DefaultPrice, int DefaultDurationMinutes, int? SuggestedReturnDays, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);

public interface IServiceManagementService
{
    Task<PagedResult<ServiceResult>> ListAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, string? search = null, bool? isActive = true, CancellationToken cancellationToken = default);
    Task<ServiceResult?> GetAsync(Guid businessId, Guid serviceId, Guid userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateAsync(Guid businessId, Guid userId, CreateServiceRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult?> UpdateAsync(Guid businessId, Guid serviceId, Guid userId, UpdateServiceRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(Guid businessId, Guid serviceId, Guid userId, CancellationToken cancellationToken = default);
}
