namespace CustomerReturnCRM.Application.ServiceTemplateManagement;

public sealed record ServiceTemplateResult(
    Guid Id,
    string BusinessType,
    string Title,
    int DefaultDurationMinutes,
    int? SuggestedReturnDays);

public interface IServiceTemplateManagementService
{
    Task<IReadOnlyList<ServiceTemplateResult>> ListAsync(
        string? businessType,
        CancellationToken cancellationToken = default);
}
