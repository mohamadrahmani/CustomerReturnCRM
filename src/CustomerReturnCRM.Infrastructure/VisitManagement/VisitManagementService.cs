using CustomerReturnCRM.Application.VisitManagement;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.VisitManagement;

public sealed class VisitManagementService : IVisitManagementService
{
    private readonly ApplicationDbContext _dbContext;

    public VisitManagementService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<VisitResult> CreateAsync(Guid businessId, Guid userId, CreateVisitRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        Validate(request.VisitAt, request.TotalAmount, request.Services);
        var references = await LoadReferencesAsync(businessId, request.CustomerId, request.Services, cancellationToken, requireActive: true);
        var visit = new Visit
        {
            Id = Guid.NewGuid(), BusinessId = businessId, CustomerId = request.CustomerId,
            VisitAt = request.VisitAt, TotalAmount = request.TotalAmount, Note = Normalize(request.Note),
            CreatedAt = DateTime.UtcNow
        };
        foreach (var item in request.Services)
        {
            var service = references.Services[item.ServiceId];
            visit.VisitServices.Add(new VisitService
            {
                Id = Guid.NewGuid(), VisitId = visit.Id, ServiceId = service.Id, StaffId = item.StaffId,
                ServiceTitle = service.Title, Price = service.DefaultPrice, DurationMinutes = service.DefaultDurationMinutes,
                SuggestedReturnDays = service.SuggestedReturnDays
            });
        }
        _dbContext.Visits.Add(visit);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResult(visit);
    }

    public async Task<IReadOnlyList<VisitResult>> ListAsync(Guid businessId, Guid userId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var query = _dbContext.Visits.AsNoTracking().Include(x => x.VisitServices).Where(x => x.BusinessId == businessId);
        if (from.HasValue) query = query.Where(x => x.VisitAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.VisitAt < to.Value);
        var visits = await query.OrderByDescending(x => x.VisitAt).ToListAsync(cancellationToken);
        return visits.Select(ToResult).ToList();
    }

    public async Task<VisitResult?> GetAsync(Guid businessId, Guid visitId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var visit = await _dbContext.Visits.AsNoTracking().Include(x => x.VisitServices).SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == visitId, cancellationToken);
        return visit is null ? null : ToResult(visit);
    }

    public async Task<VisitResult?> CompleteAppointmentAsync(Guid businessId, Guid appointmentId, Guid userId, CompleteAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        if (request.TotalAmount is < 0) throw new ArgumentException("Total amount cannot be negative.");
        var appointment = await _dbContext.Appointments.Include(x => x.AppointmentServices).SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == appointmentId, cancellationToken);
        if (appointment is null) return null;
        if (appointment.Status == AppointmentStatus.Completed) throw new InvalidOperationException("The appointment has already been completed.");
        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow) throw new InvalidOperationException("Cancelled or no-show appointments cannot be completed.");
        if (appointment.AppointmentServices.Count == 0) throw new InvalidOperationException("The appointment has no services to record as a visit.");

        var serviceIds = appointment.AppointmentServices.Select(x => x.ServiceId).Distinct().ToList();
        var suggestedReturns = await _dbContext.Services.Where(x => x.BusinessId == businessId && serviceIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.SuggestedReturnDays, cancellationToken);
        if (suggestedReturns.Count != serviceIds.Count) throw new InvalidOperationException("A service on the appointment no longer belongs to the business.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var visit = new Visit
        {
            Id = Guid.NewGuid(), BusinessId = businessId, CustomerId = appointment.CustomerId, AppointmentId = appointment.Id,
            VisitAt = request.VisitAt ?? DateTime.UtcNow, TotalAmount = request.TotalAmount,
            Note = Normalize(request.Note) ?? appointment.Note, CreatedAt = DateTime.UtcNow
        };
        foreach (var item in appointment.AppointmentServices)
        {
            visit.VisitServices.Add(new VisitService
            {
                Id = Guid.NewGuid(), VisitId = visit.Id, ServiceId = item.ServiceId, StaffId = item.StaffId,
                ServiceTitle = item.ServiceTitle, Price = item.Price, DurationMinutes = item.DurationMinutes,
                SuggestedReturnDays = suggestedReturns[item.ServiceId]
            });
        }
        appointment.Status = AppointmentStatus.Completed;
        appointment.UpdatedAt = DateTime.UtcNow;
        _dbContext.Visits.Add(visit);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResult(visit);
    }

    private async Task<ReferenceData> LoadReferencesAsync(Guid businessId, Guid customerId, IReadOnlyCollection<VisitServiceRequest> requests, CancellationToken cancellationToken, bool requireActive)
    {
        if (!await _dbContext.Customers.AnyAsync(x => x.Id == customerId && x.BusinessId == businessId && (!requireActive || x.IsActive), cancellationToken)) throw new ArgumentException("Customer does not belong to the business or is inactive.");
        var serviceIds = requests.Select(x => x.ServiceId).Distinct().ToList();
        var staffIds = requests.Select(x => x.StaffId).Distinct().ToList();
        var servicesQuery = _dbContext.Services.Where(x => x.BusinessId == businessId && serviceIds.Contains(x.Id));
        var staffQuery = _dbContext.Staff.Where(x => x.BusinessId == businessId && staffIds.Contains(x.Id));
        if (requireActive) { servicesQuery = servicesQuery.Where(x => x.IsActive); staffQuery = staffQuery.Where(x => x.IsActive); }
        var services = await servicesQuery.ToDictionaryAsync(x => x.Id, cancellationToken);
        var staff = await staffQuery.Select(x => x.Id).ToListAsync(cancellationToken);
        if (services.Count != serviceIds.Count) throw new ArgumentException("One or more services do not belong to the business or are inactive.");
        if (staff.Count != staffIds.Count) throw new ArgumentException("One or more staff members do not belong to the business or are inactive.");
        return new ReferenceData(services);
    }

    private async Task EnsureMemberAsync(Guid businessId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.BusinessMembers.AnyAsync(x => x.BusinessId == businessId && x.UserId == userId, cancellationToken)) throw new UnauthorizedAccessException("The user is not a member of this business.");
    }

    private static void Validate(DateTime visitAt, decimal? totalAmount, IReadOnlyCollection<VisitServiceRequest> services)
    {
        if (visitAt == default) throw new ArgumentException("Visit date is required.");
        if (totalAmount is < 0) throw new ArgumentException("Total amount cannot be negative.");
        if (services.Count == 0) throw new ArgumentException("A visit requires at least one service.");
        if (services.Any(x => x.ServiceId == Guid.Empty || x.StaffId == Guid.Empty)) throw new ArgumentException("Service and staff are required for every visit service.");
        if (services.Select(x => x.ServiceId).Distinct().Count() != services.Count) throw new ArgumentException("A visit cannot contain the same service more than once.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static VisitResult ToResult(Visit x) => new(x.Id, x.BusinessId, x.CustomerId, x.AppointmentId, x.VisitAt, x.TotalAmount, x.Note, x.CreatedAt, x.UpdatedAt, x.VisitServices.Select(y => new VisitServiceResult(y.Id, y.ServiceId, y.StaffId, y.ServiceTitle, y.Price, y.DurationMinutes, y.SuggestedReturnDays)).ToList());
    private sealed record ReferenceData(IReadOnlyDictionary<Guid, Service> Services);
}
