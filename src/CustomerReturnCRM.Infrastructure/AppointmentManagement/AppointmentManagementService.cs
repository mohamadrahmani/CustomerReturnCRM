using CustomerReturnCRM.Application.AppointmentManagement;
using CustomerReturnCRM.Application.Common;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.AppointmentManagement;

public sealed class AppointmentManagementService : IAppointmentManagementService
{
    private readonly ApplicationDbContext _dbContext;

    public AppointmentManagementService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<AppointmentResult> CreateAsync(Guid businessId, Guid userId, CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        Validate(request.StartAt, request.EndAt, request.Status, request.Services);
        var snapshots = await LoadAndValidateReferencesAsync(businessId, request.CustomerId, request.Services, cancellationToken);
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(), BusinessId = businessId, CustomerId = request.CustomerId,
            StartAt = request.StartAt, EndAt = request.EndAt, Status = request.Status,
            Note = Normalize(request.Note), CreatedAt = DateTime.UtcNow
        };
        AddServices(appointment, request.Services, snapshots);
        _dbContext.Appointments.Add(appointment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResult(appointment);
    }

    public async Task<PagedResult<AppointmentResult>> ListAsync(Guid businessId, Guid userId, DateTime? from, DateTime? to, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var query = _dbContext.Appointments.AsNoTracking().Include(x => x.AppointmentServices).Where(x => x.BusinessId == businessId);
        if (from.HasValue) query = query.Where(x => x.StartAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.StartAt < to.Value);
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var total = await query.CountAsync(cancellationToken);
        var appointments = await query.OrderBy(x => x.StartAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Pagination.Create(appointments.Select(ToResult).ToList(), page, pageSize, total);
    }

    public async Task<AppointmentResult?> GetAsync(Guid businessId, Guid appointmentId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var appointment = await _dbContext.Appointments.AsNoTracking().Include(x => x.AppointmentServices).SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == appointmentId, cancellationToken);
        return appointment is null ? null : ToResult(appointment);
    }

    public async Task<AppointmentResult?> UpdateAsync(Guid businessId, Guid appointmentId, Guid userId, UpdateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        Validate(request.StartAt, request.EndAt, request.Status, request.Services);
        var appointment = await _dbContext.Appointments.Include(x => x.AppointmentServices).SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == appointmentId, cancellationToken);
        if (appointment is null) return null;
        if (appointment.Status == AppointmentStatus.Completed) throw new InvalidOperationException("Completed appointments cannot be updated.");
        var snapshots = await LoadAndValidateReferencesAsync(businessId, request.CustomerId, request.Services, cancellationToken);
        appointment.CustomerId = request.CustomerId; appointment.StartAt = request.StartAt; appointment.EndAt = request.EndAt; appointment.Status = request.Status; appointment.Note = Normalize(request.Note); appointment.UpdatedAt = DateTime.UtcNow;
        _dbContext.AppointmentServices.RemoveRange(appointment.AppointmentServices);
        appointment.AppointmentServices.Clear();
        AddServices(appointment, request.Services, snapshots);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResult(appointment);
    }

    public async Task<AppointmentResult?> CancelAsync(Guid businessId, Guid appointmentId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var appointment = await _dbContext.Appointments.Include(x => x.AppointmentServices).SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == appointmentId, cancellationToken);
        if (appointment is null) return null;
        if (appointment.Status == AppointmentStatus.Completed) throw new InvalidOperationException("Completed appointments cannot be cancelled.");
        appointment.Status = AppointmentStatus.Cancelled; appointment.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResult(appointment);
    }

    private async Task<ReferenceSnapshots> LoadAndValidateReferencesAsync(Guid businessId, Guid customerId, IReadOnlyCollection<AppointmentServiceRequest> requests, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Customers.AnyAsync(x => x.Id == customerId && x.BusinessId == businessId && x.IsActive, cancellationToken)) throw new ArgumentException("Customer does not belong to the business or is inactive.");
        var serviceIds = requests.Select(x => x.ServiceId).Distinct().ToList();
        var staffIds = requests.Select(x => x.StaffId).Distinct().ToList();
        var services = await _dbContext.Services.Where(x => x.BusinessId == businessId && x.IsActive && serviceIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var staff = await _dbContext.Staff.Where(x => x.BusinessId == businessId && x.IsActive && staffIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(cancellationToken);
        if (services.Count != serviceIds.Count) throw new ArgumentException("One or more services do not belong to the business or are inactive.");
        if (staff.Count != staffIds.Count) throw new ArgumentException("One or more staff members do not belong to the business or are inactive.");
        return new ReferenceSnapshots(services);
    }

    private static void AddServices(Appointment appointment, IReadOnlyCollection<AppointmentServiceRequest> requests, ReferenceSnapshots snapshots)
    {
        foreach (var request in requests)
        {
            var service = snapshots.Services[request.ServiceId];
            appointment.AppointmentServices.Add(new AppointmentService
            {
                Id = Guid.NewGuid(), AppointmentId = appointment.Id, ServiceId = service.Id, StaffId = request.StaffId,
                ServiceTitle = service.Title, Price = service.DefaultPrice, DurationMinutes = service.DefaultDurationMinutes
            });
        }
    }

    private async Task EnsureMemberAsync(Guid businessId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.BusinessMembers.AnyAsync(x => x.BusinessId == businessId && x.UserId == userId, cancellationToken)) throw new UnauthorizedAccessException("The user is not a member of this business.");
    }

    private static void Validate(DateTime startAt, DateTime endAt, AppointmentStatus status, IReadOnlyCollection<AppointmentServiceRequest> services)
    {
        if (endAt <= startAt) throw new ArgumentException("End time must be after start time.");
        if (status == AppointmentStatus.Completed) throw new ArgumentException("Appointment completion is not available in this phase.");
        if (services.Count == 0) throw new ArgumentException("An appointment requires at least one service.");
        if (services.Any(x => x.ServiceId == Guid.Empty || x.StaffId == Guid.Empty)) throw new ArgumentException("Service and staff are required for every appointment service.");
        if (services.Select(x => x.ServiceId).Distinct().Count() != services.Count) throw new ArgumentException("An appointment cannot contain the same service more than once.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static AppointmentResult ToResult(Appointment x) => new(x.Id, x.BusinessId, x.CustomerId, x.StartAt, x.EndAt, x.Status, x.Note, x.CreatedAt, x.UpdatedAt, x.AppointmentServices.Select(y => new AppointmentServiceResult(y.Id, y.ServiceId, y.StaffId, y.ServiceTitle, y.Price, y.DurationMinutes)).ToList());
    private sealed record ReferenceSnapshots(IReadOnlyDictionary<Guid, Service> Services);
}
