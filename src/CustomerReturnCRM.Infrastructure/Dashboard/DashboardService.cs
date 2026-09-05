using CustomerReturnCRM.Application.Dashboard;
using CustomerReturnCRM.Application.ReturnAnalysis;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using CustomerReturnCRM.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IReturnAnalysisService _returnAnalysisService;
    private readonly TimeProvider _timeProvider;

    public DashboardService(
        ApplicationDbContext dbContext,
        IReturnAnalysisService returnAnalysisService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _returnAnalysisService = returnAnalysisService;
        _timeProvider = timeProvider;
    }

    public async Task<DashboardResult> GetAsync(
        Guid businessId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);

        var today = IranTime.Now(_timeProvider).Date;
        var todayUtc = IranTime.StartOfTodayUtc(_timeProvider);
        var tomorrowUtc = todayUtc.AddDays(1);

        var activeCustomerCount = await _dbContext.Customers.CountAsync(
            x => x.BusinessId == businessId && x.IsActive,
            cancellationToken);

        var appointments = await _dbContext.Appointments.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.StartAt >= todayUtc && x.StartAt < tomorrowUtc)
            .OrderBy(x => x.StartAt)
            .Select(x => new DashboardAppointmentResult(
                x.Id,
                x.CustomerId,
                string.IsNullOrWhiteSpace(x.Customer.LastName)
                    ? x.Customer.FirstName
                    : x.Customer.FirstName + " " + x.Customer.LastName,
                x.StartAt,
                x.EndAt,
                x.Status,
                x.AppointmentServices.OrderBy(service => service.ServiceTitle)
                    .Select(service => service.ServiceTitle)
                    .ToList()))
            .ToListAsync(cancellationToken);

        var pendingReminders = await _dbContext.Reminders.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.Status == ReminderStatus.Pending)
            .OrderBy(x => x.DueAt)
            .Select(x => new DashboardReminderResult(
                x.Id,
                x.CustomerId,
                string.IsNullOrWhiteSpace(x.Customer.LastName)
                    ? x.Customer.FirstName
                    : x.Customer.FirstName + " " + x.Customer.LastName,
                x.ServiceId,
                x.Title,
                x.DueAt,
                x.Status))
            .ToListAsync(cancellationToken);

        var recentVisits = await _dbContext.Visits.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.Customer.IsActive)
            .OrderByDescending(x => x.VisitAt)
            .Take(10)
            .Select(x => new DashboardVisitResult(
                x.Id,
                x.CustomerId,
                string.IsNullOrWhiteSpace(x.Customer.LastName)
                    ? x.Customer.FirstName
                    : x.Customer.FirstName + " " + x.Customer.LastName,
                x.VisitAt,
                x.TotalAmount))
            .ToListAsync(cancellationToken);

        return new DashboardResult(
            today,
            activeCustomerCount,
            appointments,
            pendingReminders,
            (await _returnAnalysisService.GetDueSoonAsync(businessId, userId, 1, 100, cancellationToken)).Items,
            (await _returnAnalysisService.GetOverdueAsync(businessId, userId, 1, 100, cancellationToken)).Items,
            (await _returnAnalysisService.GetAtRiskAsync(businessId, userId, 1, 100, cancellationToken)).Items,
            (await _returnAnalysisService.GetNoRecentVisitAsync(businessId, userId, 1, 100, cancellationToken)).Items,
            recentVisits);
    }

    private async Task EnsureMemberAsync(Guid businessId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.BusinessMembers.AnyAsync(
                x => x.BusinessId == businessId && x.UserId == userId,
                cancellationToken))
            throw new UnauthorizedAccessException("The user is not a member of this business.");
    }
}
