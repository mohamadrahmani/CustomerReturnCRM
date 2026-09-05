using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Application.Common;

namespace CustomerReturnCRM.Application.VisitManagement;

public sealed class VisitServiceRequest
{
    public Guid ServiceId { get; init; }
    public Guid StaffId { get; init; }
}

public sealed class CreateVisitRequest
{
    public Guid CustomerId { get; init; }
    public DateTime VisitAt { get; init; }
    public decimal? TotalAmount { get; init; }
    public string? Note { get; init; }
    public List<VisitServiceRequest> Services { get; init; } = new();
}

public sealed class CompleteAppointmentRequest
{
    public DateTime? VisitAt { get; init; }
    public decimal? TotalAmount { get; init; }
    public string? Note { get; init; }
}

public sealed record VisitServiceResult(Guid Id, Guid ServiceId, Guid StaffId, string ServiceTitle, decimal Price, int DurationMinutes, int? SuggestedReturnDays);

public sealed record VisitResult(Guid Id, Guid BusinessId, Guid CustomerId, Guid? AppointmentId, DateTime VisitAt, decimal? TotalAmount, string? Note, DateTime CreatedAt, DateTime? UpdatedAt, IReadOnlyList<VisitServiceResult> Services);

public interface IVisitManagementService
{
    Task<VisitResult> CreateAsync(Guid businessId, Guid userId, CreateVisitRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<VisitResult>> ListAsync(Guid businessId, Guid userId, DateTime? from, DateTime? to, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<VisitResult?> GetAsync(Guid businessId, Guid visitId, Guid userId, CancellationToken cancellationToken = default);
    Task<VisitResult?> CompleteAppointmentAsync(Guid businessId, Guid appointmentId, Guid userId, CompleteAppointmentRequest request, CancellationToken cancellationToken = default);
}
