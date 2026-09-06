using CustomerReturnCRM.Application.Common;

namespace CustomerReturnCRM.Application.ReturnAnalysis;

public sealed record ExpectedReturnResult(Guid ServiceId, string ServiceTitle, DateTime LastVisitAt, int SuggestedReturnDays, DateTime ExpectedReturnDate, int DaysFromExpectedReturn, bool HasFutureAppointment);
public sealed record CustomerReturnAnalysisResult(Guid CustomerId, string CustomerName, string Mobile, IReadOnlyList<ExpectedReturnResult> Services);
public sealed record SmartListItemResult(Guid CustomerId, string CustomerName, string Mobile, Guid? ServiceId, string? ServiceTitle, DateTime LastVisitAt, DateTime? ExpectedReturnDate, int? DaysFromExpectedReturn, string SmartListType);

public static class SmartListTypes
{
    public const string Overdue = "Overdue";
    public const string DueSoon = "DueSoon";
    public const string AtRisk = "AtRisk";
    public const string NoRecentVisit = "NoRecentVisit";
    public static bool IsValid(string value) => value is Overdue or DueSoon or AtRisk or NoRecentVisit;
}

public sealed record DismissSmartListItemRequest(string SmartListType, Guid CustomerId, Guid? ServiceId);
public sealed record RestoreSmartListItemRequest(string SmartListType, Guid CustomerId, Guid? ServiceId);

public interface IReturnAnalysisService
{
    Task<CustomerReturnAnalysisResult?> GetCustomerAnalysisAsync(Guid businessId, Guid customerId, Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResult<SmartListItemResult>> GetOverdueAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<PagedResult<SmartListItemResult>> GetDueSoonAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<PagedResult<SmartListItemResult>> GetAtRiskAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<PagedResult<SmartListItemResult>> GetNoRecentVisitAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<PagedResult<SmartListItemResult>> GetDismissedAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<bool> DismissAsync(Guid businessId, Guid userId, DismissSmartListItemRequest request, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(Guid businessId, Guid userId, RestoreSmartListItemRequest request, CancellationToken cancellationToken = default);
}
