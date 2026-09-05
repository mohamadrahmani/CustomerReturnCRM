namespace CustomerReturnCRM.Application.BusinessSetup;

public sealed class BusinessSetupRequest
{
    public string Name { get; init; } = null!;
    public string BusinessType { get; init; } = null!;
    public string Mobile { get; init; } = null!;
    public string? Address { get; init; }
    public string? City { get; init; }
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string? StaffMobile { get; init; }
    public Guid? ServiceTemplateId { get; init; }
}

public sealed record BusinessSetupResult(Guid BusinessId, Guid MembershipId, Guid StaffId, Guid? ServiceId);

public interface IBusinessSetupService
{
    Task<BusinessSetupResult> CreateAsync(BusinessSetupRequest request, Guid userId, CancellationToken cancellationToken = default);
}
