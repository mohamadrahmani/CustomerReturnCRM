using CustomerReturnCRM.Application.Common;

namespace CustomerReturnCRM.Application.CustomerManagement;

public sealed class CreateCustomerRequest
{
    public string FirstName { get; init; } = null!;
    public string? LastName { get; init; }
    public string Mobile { get; init; } = null!;
    public DateTime? BirthDate { get; init; }
    public string? Note { get; init; }
}

public sealed class UpdateCustomerRequest
{
    public string FirstName { get; init; } = null!;
    public string? LastName { get; init; }
    public string Mobile { get; init; } = null!;
    public DateTime? BirthDate { get; init; }
    public string? Note { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record CustomerResult(
    Guid Id,
    Guid BusinessId,
    string FirstName,
    string? LastName,
    string Mobile,
    DateTime? BirthDate,
    string? Note,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? LastVisitDate,
    int TotalVisits);

public interface ICustomerManagementService
{
    Task<PagedResult<CustomerResult>> ListAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, string? search = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<CustomerResult?> GetAsync(Guid businessId, Guid customerId, Guid userId, CancellationToken cancellationToken = default);
    Task<CustomerResult> CreateAsync(Guid businessId, Guid userId, CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerResult?> UpdateAsync(Guid businessId, Guid customerId, Guid userId, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(Guid businessId, Guid customerId, Guid userId, CancellationToken cancellationToken = default);
}
