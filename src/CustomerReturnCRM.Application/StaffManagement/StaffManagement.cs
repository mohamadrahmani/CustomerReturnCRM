using CustomerReturnCRM.Application.Common;

namespace CustomerReturnCRM.Application.StaffManagement;

public sealed class CreateStaffRequest
{
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string? Mobile { get; init; }
}

public sealed class UpdateStaffRequest
{
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string? Mobile { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record StaffResult(Guid Id, Guid BusinessId, string FirstName, string LastName, string? Mobile, Guid? UserId, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);

public interface IStaffManagementService
{
    Task<PagedResult<StaffResult>> ListAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, string? search = null, bool? isActive = true, CancellationToken cancellationToken = default);
    Task<StaffResult?> GetAsync(Guid businessId, Guid staffId, Guid userId, CancellationToken cancellationToken = default);
    Task<StaffResult> CreateAsync(Guid businessId, Guid userId, CreateStaffRequest request, CancellationToken cancellationToken = default);
    Task<StaffResult?> UpdateAsync(Guid businessId, Guid staffId, Guid userId, UpdateStaffRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(Guid businessId, Guid staffId, Guid userId, CancellationToken cancellationToken = default);
}
