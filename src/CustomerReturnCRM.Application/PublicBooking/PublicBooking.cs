namespace CustomerReturnCRM.Application.PublicBooking;

public sealed record PublicBusinessProfileResult(
    Guid BusinessId,
    string Name,
    string BusinessType,
    string Mobile,
    string? Address,
    string? City,
    string? Description,
    string PublicSlug,
    IReadOnlyList<PublicServiceResult> Services,
    IReadOnlyList<PublicStaffResult> Staff);

public sealed record PublicServiceResult(
    Guid Id,
    string Title,
    string? Description,
    decimal Price,
    int DurationMinutes);

public sealed record PublicStaffResult(
    Guid Id,
    string FirstName,
    string LastName);

public sealed class PublicBookingRequest
{
    public Guid ServiceId { get; init; }
    public Guid? StaffId { get; init; }
    public DateTime StartAt { get; init; }
    public string FirstName { get; init; } = null!;
    public string? LastName { get; init; }
    public string Mobile { get; init; } = null!;
    public string? Note { get; init; }
}

public sealed record PublicBookingResult(
    Guid AppointmentId,
    string PublicCode,
    DateTime StartAt,
    DateTime EndAt,
    string Status,
    string BusinessName,
    string ServiceTitle,
    string StaffName,
    string PublicDetailsUrl);

public sealed record PublicAppointmentResult(
    string PublicCode,
    string BusinessName,
    string? Address,
    string? City,
    string CustomerName,
    string ServiceTitle,
    string StaffName,
    DateTime StartAt,
    DateTime EndAt,
    string Status);

public interface IPublicBookingService
{
    Task<PublicBusinessProfileResult?> GetBusinessAsync(string slug, CancellationToken cancellationToken = default);
    Task<PublicBookingResult> BookAsync(string slug, PublicBookingRequest request, CancellationToken cancellationToken = default);
    Task<PublicAppointmentResult?> GetAppointmentAsync(string publicCode, CancellationToken cancellationToken = default);
}
