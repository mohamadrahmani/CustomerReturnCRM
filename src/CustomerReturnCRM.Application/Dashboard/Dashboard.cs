using CustomerReturnCRM.Application.ReminderManagement;
using CustomerReturnCRM.Application.ReturnAnalysis;
using CustomerReturnCRM.Domain.Entities;

namespace CustomerReturnCRM.Application.Dashboard;

public sealed record DashboardAppointmentResult(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    DateTime StartAt,
    DateTime EndAt,
    AppointmentStatus Status,
    IReadOnlyList<string> Services);

public sealed record DashboardReminderResult(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid? ServiceId,
    string Title,
    DateTime DueAt,
    ReminderStatus Status);

public sealed record DashboardVisitResult(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    DateTime VisitAt,
    decimal? TotalAmount);

public sealed record DashboardResult(
    DateTime Date,
    int ActiveCustomerCount,
    IReadOnlyList<DashboardAppointmentResult> TodayAppointments,
    IReadOnlyList<DashboardReminderResult> PendingReminders,
    IReadOnlyList<SmartListItemResult> DueSoon,
    IReadOnlyList<SmartListItemResult> Overdue,
    IReadOnlyList<SmartListItemResult> AtRisk,
    IReadOnlyList<SmartListItemResult> NoRecentVisit,
    IReadOnlyList<DashboardVisitResult> RecentVisits);

public interface IDashboardService
{
    Task<DashboardResult> GetAsync(
        Guid businessId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
