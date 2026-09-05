using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Application.Common;

namespace CustomerReturnCRM.Application.AppointmentManagement;

public sealed class AppointmentServiceRequest
{
    public Guid ServiceId { get; init; }
    public Guid StaffId { get; init; }
}

public sealed class CreateAppointmentRequest
{
    public Guid CustomerId { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public AppointmentStatus Status { get; init; } = AppointmentStatus.Pending;
    public string? Note { get; init; }
    public List<AppointmentServiceRequest> Services { get; init; } = new();
}

public sealed class UpdateAppointmentRequest
{
    public Guid CustomerId { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public AppointmentStatus Status { get; init; } = AppointmentStatus.Pending;
    public string? Note { get; init; }
    public List<AppointmentServiceRequest> Services { get; init; } = new();
}

public sealed record AppointmentServiceResult(Guid Id, Guid ServiceId, Guid StaffId, string ServiceTitle, decimal Price, int DurationMinutes);
public sealed record AppointmentResult(Guid Id, Guid BusinessId, Guid CustomerId, DateTime StartAt, DateTime EndAt, AppointmentStatus Status, string? Note, DateTime CreatedAt, DateTime? UpdatedAt, IReadOnlyList<AppointmentServiceResult> Services);

public interface IAppointmentManagementService
{
    Task<AppointmentResult> CreateAsync(Guid businessId, Guid userId, CreateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<AppointmentResult>> ListAsync(Guid businessId, Guid userId, DateTime? from, DateTime? to, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<AppointmentResult?> GetAsync(Guid businessId, Guid appointmentId, Guid userId, CancellationToken cancellationToken = default);
    Task<AppointmentResult?> UpdateAsync(Guid businessId, Guid appointmentId, Guid userId, UpdateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<AppointmentResult?> ConfirmAsync(Guid businessId, Guid appointmentId, Guid userId, CancellationToken cancellationToken = default);
    Task<AppointmentResult?> CancelAsync(Guid businessId, Guid appointmentId, Guid userId, CancellationToken cancellationToken = default);
    Task<AppointmentResult?> MarkNoShowAsync(Guid businessId, Guid appointmentId, Guid userId, CancellationToken cancellationToken = default);
}
