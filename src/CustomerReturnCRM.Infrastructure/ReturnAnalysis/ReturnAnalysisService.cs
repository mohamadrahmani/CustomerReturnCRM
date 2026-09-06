using CustomerReturnCRM.Application.ReturnAnalysis;
using CustomerReturnCRM.Application.Common;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using CustomerReturnCRM.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CustomerReturnCRM.Infrastructure.ReturnAnalysis;

public sealed class ReturnAnalysisService : IReturnAnalysisService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ReturnAnalysisOptions _options;
    private readonly TimeProvider _timeProvider;

    public ReturnAnalysisService(ApplicationDbContext dbContext, IOptions<ReturnAnalysisOptions> options, TimeProvider timeProvider) { _dbContext = dbContext; _options = options.Value; _timeProvider = timeProvider; }

    public async Task<CustomerReturnAnalysisResult?> GetCustomerAnalysisAsync(Guid businessId, Guid customerId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var customer = await _dbContext.Customers.AsNoTracking().Where(x => x.BusinessId == businessId && x.Id == customerId && x.IsActive).Select(x => new CustomerRow(x.Id, x.FirstName, x.LastName, x.Mobile)).SingleOrDefaultAsync(cancellationToken);
        if (customer is null) return null;
        var expectedReturns = await LoadExpectedReturnsAsync(businessId, customerId, cancellationToken);
        return new CustomerReturnAnalysisResult(customer.Id, BuildCustomerName(customer.FirstName, customer.LastName), customer.Mobile, expectedReturns.OrderBy(x => x.ExpectedReturnDate).Select(ToExpectedReturnResult).ToList());
    }

    public Task<PagedResult<SmartListItemResult>> GetOverdueAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) => GetSmartListAsync(businessId, userId, SmartListTypes.Overdue, page, pageSize, cancellationToken);
    public Task<PagedResult<SmartListItemResult>> GetDueSoonAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) => GetSmartListAsync(businessId, userId, SmartListTypes.DueSoon, page, pageSize, cancellationToken);
    public Task<PagedResult<SmartListItemResult>> GetAtRiskAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) => GetSmartListAsync(businessId, userId, SmartListTypes.AtRisk, page, pageSize, cancellationToken);
    public Task<PagedResult<SmartListItemResult>> GetNoRecentVisitAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) => GetNoRecentVisitItemsAsync(businessId, userId, page, pageSize, cancellationToken);

    public async Task<PagedResult<SmartListItemResult>> GetDismissedAsync(Guid businessId, Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = _dbContext.SmartListDismissals.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.Customer.IsActive)
            .Include(x => x.Customer)
            .Include(x => x.Service);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).ThenBy(x => x.Customer.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new SmartListItemResult(
                x.CustomerId,
                BuildCustomerName(x.Customer.FirstName, x.Customer.LastName),
                x.Customer.Mobile,
                x.ServiceId,
                x.Service != null ? x.Service.Title : null,
                x.LastVisitAt,
                x.ExpectedReturnDate,
                x.ExpectedReturnDate.HasValue ? (IranTime.Now(_timeProvider).Date - x.ExpectedReturnDate.Value.Date).Days : null,
                x.SmartListType))
            .ToListAsync(cancellationToken);
        return Pagination.Create(items, page, pageSize, total);
    }

    public async Task<bool> DismissAsync(Guid businessId, Guid userId, DismissSmartListItemRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        ValidateDismissalRequest(request.SmartListType, request.CustomerId, request.ServiceId);
        var now = IranTime.UtcNow(_timeProvider); var localToday = IranTime.Now(_timeProvider).Date; DateTime lastVisitAt; DateTime? expectedReturnDate;
        if (request.SmartListType == SmartListTypes.NoRecentVisit)
        {
            var lastVisit = await _dbContext.Visits.AsNoTracking().Where(x => x.BusinessId == businessId && x.CustomerId == request.CustomerId && x.Customer.IsActive).MaxAsync(x => (DateTime?)x.VisitAt, cancellationToken);
            if (!lastVisit.HasValue || IranTime.ToIran(lastVisit.Value).Date >= localToday.AddDays(-_options.NoRecentVisitDays)) return false;
            lastVisitAt = lastVisit.Value; expectedReturnDate = null;
        }
        else
        {
            var expectedReturn = (await LoadExpectedReturnsAsync(businessId, request.CustomerId, cancellationToken)).SingleOrDefault(x => x.ServiceId == request.ServiceId);
            if (expectedReturn is null || expectedReturn.HasFutureAppointment || !IsInSmartList(expectedReturn, request.SmartListType)) return false;
            lastVisitAt = expectedReturn.LastVisitAt; expectedReturnDate = expectedReturn.ExpectedReturnDate;
        }
        var dismissal = await _dbContext.SmartListDismissals.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.CustomerId == request.CustomerId && x.ServiceId == request.ServiceId && x.SmartListType == request.SmartListType, cancellationToken);
        if (dismissal is null) _dbContext.SmartListDismissals.Add(new SmartListDismissal { Id = Guid.NewGuid(), BusinessId = businessId, CustomerId = request.CustomerId, ServiceId = request.ServiceId, SmartListType = request.SmartListType, LastVisitAt = lastVisitAt, ExpectedReturnDate = expectedReturnDate, DismissedByUserId = userId, CreatedAt = now });
        else { dismissal.LastVisitAt = lastVisitAt; dismissal.ExpectedReturnDate = expectedReturnDate; dismissal.DismissedByUserId = userId; dismissal.UpdatedAt = now; }
        await _dbContext.SaveChangesAsync(cancellationToken); return true;
    }

    public async Task<bool> RestoreAsync(Guid businessId, Guid userId, RestoreSmartListItemRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken); ValidateDismissalRequest(request.SmartListType, request.CustomerId, request.ServiceId);
        var dismissal = await _dbContext.SmartListDismissals.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.CustomerId == request.CustomerId && x.ServiceId == request.ServiceId && x.SmartListType == request.SmartListType, cancellationToken);
        if (dismissal is null) return false; _dbContext.SmartListDismissals.Remove(dismissal); await _dbContext.SaveChangesAsync(cancellationToken); return true;
    }

    private async Task<PagedResult<SmartListItemResult>> GetSmartListAsync(Guid businessId, Guid userId, string listType, int page, int pageSize, CancellationToken cancellationToken)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken); var expectedReturns = await LoadExpectedReturnsAsync(businessId, null, cancellationToken); var dismissals = await LoadDismissalsAsync(businessId, listType, cancellationToken);
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var allItems = expectedReturns.Where(x => !x.HasFutureAppointment && IsInSmartList(x, listType) && !IsDismissed(x, dismissals)).OrderBy(x => listType == SmartListTypes.DueSoon ? x.ExpectedReturnDate : DateTime.MaxValue).ThenByDescending(x => x.DaysFromExpectedReturn).ThenBy(x => x.CustomerName).Select(x => ToSmartListItem(x, listType)).ToList();
        return Pagination.Create(allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, allItems.Count);
    }

    private async Task<PagedResult<SmartListItemResult>> GetNoRecentVisitItemsAsync(Guid businessId, Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken); var now = IranTime.UtcNow(_timeProvider); var inactiveBefore = IranTime.Now(_timeProvider).Date.AddDays(-_options.NoRecentVisitDays); var futureCustomerIds = await LoadFutureAppointmentCustomerIdsAsync(businessId, now, cancellationToken); var dismissals = await LoadDismissalsAsync(businessId, SmartListTypes.NoRecentVisit, cancellationToken);
        var lastVisits = await _dbContext.Visits.AsNoTracking().Where(x => x.BusinessId == businessId && x.Customer.IsActive).GroupBy(x => new { x.CustomerId, x.Customer.FirstName, x.Customer.LastName, x.Customer.Mobile }).Select(g => new LastVisitRow(g.Key.CustomerId, g.Key.FirstName, g.Key.LastName, g.Key.Mobile, g.Max(x => x.VisitAt))).ToListAsync(cancellationToken);
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var allItems = lastVisits.Where(x => x.LastVisitAt.Date < inactiveBefore && !futureCustomerIds.Contains(x.CustomerId) && !IsDismissed(x, dismissals)).OrderBy(x => x.LastVisitAt).ThenBy(x => x.FirstName).Select(x => new SmartListItemResult(x.CustomerId, BuildCustomerName(x.FirstName, x.LastName), x.Mobile, null, null, x.LastVisitAt, null, null, SmartListTypes.NoRecentVisit)).ToList();
        return Pagination.Create(allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, allItems.Count);
    }

    private async Task<IReadOnlyList<ExpectedReturnRow>> LoadExpectedReturnsAsync(Guid businessId, Guid? customerId, CancellationToken cancellationToken)
    {
        var query = _dbContext.VisitServices.AsNoTracking().Where(x => x.Visit.BusinessId == businessId && x.Visit.Customer.IsActive && x.Service.IsActive && x.SuggestedReturnDays.HasValue);
        if (customerId.HasValue) query = query.Where(x => x.Visit.CustomerId == customerId.Value);
        var rows = await query.Select(x => new VisitServiceRow(x.Id, x.Visit.CustomerId, x.Visit.Customer.FirstName, x.Visit.Customer.LastName, x.Visit.Customer.Mobile, x.ServiceId, x.ServiceTitle, x.Visit.VisitAt, x.SuggestedReturnDays!.Value)).ToListAsync(cancellationToken);
        var now = IranTime.UtcNow(_timeProvider); var localToday = IranTime.Now(_timeProvider).Date; var futureAppointments = await LoadFutureAppointmentServiceKeysAsync(businessId, now, cancellationToken);
        return rows.GroupBy(x => new { x.CustomerId, x.ServiceId }).Select(g => g.OrderByDescending(x => x.VisitAt).ThenByDescending(x => x.VisitServiceId).First()).Select(x => { var expected = IranTime.ToIran(x.VisitAt).Date.AddDays(x.SuggestedReturnDays); return new ExpectedReturnRow(x.CustomerId, BuildCustomerName(x.FirstName, x.LastName), x.Mobile, x.ServiceId, x.ServiceTitle, x.VisitAt, x.SuggestedReturnDays, expected, (localToday - expected).Days, futureAppointments.Contains((x.CustomerId, x.ServiceId))); }).ToList();
    }

    private async Task<HashSet<(Guid CustomerId, Guid ServiceId)>> LoadFutureAppointmentServiceKeysAsync(Guid businessId, DateTime now, CancellationToken cancellationToken)
    {
        var keys = await _dbContext.AppointmentServices.AsNoTracking().Where(x => x.Appointment.BusinessId == businessId && x.Appointment.StartAt >= now && (x.Appointment.Status == AppointmentStatus.Pending || x.Appointment.Status == AppointmentStatus.Confirmed)).Select(x => new { x.Appointment.CustomerId, x.ServiceId }).Distinct().ToListAsync(cancellationToken); return keys.Select(x => (x.CustomerId, x.ServiceId)).ToHashSet();
    }
    private async Task<HashSet<Guid>> LoadFutureAppointmentCustomerIdsAsync(Guid businessId, DateTime now, CancellationToken cancellationToken)
    {
        var ids = await _dbContext.Appointments.AsNoTracking().Where(x => x.BusinessId == businessId && x.StartAt >= now && (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Confirmed)).Select(x => x.CustomerId).Distinct().ToListAsync(cancellationToken); return ids.ToHashSet();
    }
    private async Task<List<SmartListDismissal>> LoadDismissalsAsync(Guid businessId, string listType, CancellationToken cancellationToken) => await _dbContext.SmartListDismissals.AsNoTracking().Where(x => x.BusinessId == businessId && x.SmartListType == listType).ToListAsync(cancellationToken);
    private bool IsInSmartList(ExpectedReturnRow item, string listType) => listType switch { SmartListTypes.Overdue => item.DaysFromExpectedReturn > 0 && item.DaysFromExpectedReturn <= _options.AtRiskDays, SmartListTypes.DueSoon => item.DaysFromExpectedReturn <= 0 && item.DaysFromExpectedReturn >= -_options.DueSoonDays, SmartListTypes.AtRisk => item.DaysFromExpectedReturn > _options.AtRiskDays, _ => false };
    private static bool IsDismissed(ExpectedReturnRow item, IEnumerable<SmartListDismissal> dismissals) => dismissals.Any(x => x.CustomerId == item.CustomerId && x.ServiceId == item.ServiceId && x.LastVisitAt == item.LastVisitAt && x.ExpectedReturnDate == item.ExpectedReturnDate);
    private static bool IsDismissed(LastVisitRow item, IEnumerable<SmartListDismissal> dismissals) => dismissals.Any(x => x.CustomerId == item.CustomerId && x.ServiceId == null && x.LastVisitAt == item.LastVisitAt);
    private static void ValidateDismissalRequest(string listType, Guid customerId, Guid? serviceId) { if (!SmartListTypes.IsValid(listType)) throw new ArgumentException("Unknown smart list type."); if (customerId == Guid.Empty) throw new ArgumentException("Customer is required."); if (listType == SmartListTypes.NoRecentVisit && serviceId.HasValue) throw new ArgumentException("NoRecentVisit does not accept a service."); if (listType != SmartListTypes.NoRecentVisit && (!serviceId.HasValue || serviceId == Guid.Empty)) throw new ArgumentException("Service is required for this smart list."); }
    private async Task EnsureMemberAsync(Guid businessId, Guid userId, CancellationToken cancellationToken) { if (!await _dbContext.BusinessMembers.AnyAsync(x => x.BusinessId == businessId && x.UserId == userId, cancellationToken)) throw new UnauthorizedAccessException("The user is not a member of this business."); }
    private static ExpectedReturnResult ToExpectedReturnResult(ExpectedReturnRow x) => new(x.ServiceId, x.ServiceTitle, x.LastVisitAt, x.SuggestedReturnDays, x.ExpectedReturnDate, x.DaysFromExpectedReturn, x.HasFutureAppointment);
    private static SmartListItemResult ToSmartListItem(ExpectedReturnRow x, string listType) => new(x.CustomerId, x.CustomerName, x.Mobile, x.ServiceId, x.ServiceTitle, x.LastVisitAt, x.ExpectedReturnDate, x.DaysFromExpectedReturn, listType);
    private static string BuildCustomerName(string firstName, string? lastName) => string.IsNullOrWhiteSpace(lastName) ? firstName : $"{firstName} {lastName}";
    private sealed record CustomerRow(Guid Id, string FirstName, string? LastName, string Mobile);
    private sealed record VisitServiceRow(Guid VisitServiceId, Guid CustomerId, string FirstName, string? LastName, string Mobile, Guid ServiceId, string ServiceTitle, DateTime VisitAt, int SuggestedReturnDays);
    private sealed record ExpectedReturnRow(Guid CustomerId, string CustomerName, string Mobile, Guid ServiceId, string ServiceTitle, DateTime LastVisitAt, int SuggestedReturnDays, DateTime ExpectedReturnDate, int DaysFromExpectedReturn, bool HasFutureAppointment);
    private sealed record LastVisitRow(Guid CustomerId, string FirstName, string? LastName, string Mobile, DateTime LastVisitAt);
}
