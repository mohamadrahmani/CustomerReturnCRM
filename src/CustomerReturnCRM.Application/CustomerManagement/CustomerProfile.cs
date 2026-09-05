using CustomerReturnCRM.Application.ReturnAnalysis;

namespace CustomerReturnCRM.Application.CustomerManagement;

public sealed record CustomerProfileVisit(Guid Id, DateTime VisitAt, decimal? TotalAmount, string? Note, IReadOnlyList<string> Services);

public sealed record CustomerProfileAppointment(Guid Id, DateTime StartAt, DateTime EndAt, string Status, string? Note, IReadOnlyList<string> Services);

public sealed record CustomerProfileReminder(Guid Id, Guid? ServiceId, string Title, DateTime DueAt, string Status, string? Note);

public sealed record CustomerProfileResult(CustomerResult Customer, IReadOnlyList<CustomerProfileVisit> Visits, IReadOnlyList<CustomerProfileAppointment> FutureAppointments, IReadOnlyList<CustomerProfileReminder> Reminders, CustomerReturnAnalysisResult ReturnAnalysis);

public interface ICustomerProfileService
{
    Task<CustomerProfileResult?> GetAsync(Guid businessId, Guid customerId, Guid userId, CancellationToken cancellationToken = default);
}
