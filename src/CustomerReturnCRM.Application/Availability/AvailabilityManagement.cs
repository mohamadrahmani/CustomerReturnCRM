namespace CustomerReturnCRM.Application.Availability;

public sealed class WorkingHourRequest
{
    public DayOfWeek DayOfWeek { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
}

public sealed record WorkingHourResult(Guid Id, DayOfWeek DayOfWeek, TimeSpan StartTime, TimeSpan EndTime);
public sealed record StaffTimeOffResult(Guid Id, Guid StaffId, DateTime StartAt, DateTime EndAt, string? Reason);
public sealed record AvailabilitySlotResult(Guid StaffId, DateTime StartAt, DateTime EndAt);

public sealed class AvailabilityRequest
{
    public Guid ServiceId { get; init; }
    public Guid? StaffId { get; init; }
    public DateOnly Date { get; init; }
    public int SlotIntervalMinutes { get; init; } = 30;
}

public interface IAvailabilityManagementService
{
    Task<IReadOnlyList<WorkingHourResult>> GetBusinessWorkingHoursAsync(Guid businessId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkingHourResult>> ReplaceBusinessWorkingHoursAsync(Guid businessId, Guid userId, IReadOnlyCollection<WorkingHourRequest> hours, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkingHourResult>> GetStaffWorkingHoursAsync(Guid businessId, Guid staffId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkingHourResult>> ReplaceStaffWorkingHoursAsync(Guid businessId, Guid staffId, Guid userId, IReadOnlyCollection<WorkingHourRequest> hours, CancellationToken cancellationToken = default);
    Task<StaffTimeOffResult> AddTimeOffAsync(Guid businessId, Guid staffId, Guid userId, DateTime startAt, DateTime endAt, string? reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaffTimeOffResult>> ListTimeOffAsync(Guid businessId, Guid staffId, Guid userId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<bool> DeleteTimeOffAsync(Guid businessId, Guid staffId, Guid timeOffId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AvailabilitySlotResult>> GetAvailableSlotsAsync(Guid businessId, AvailabilityRequest request, CancellationToken cancellationToken = default);
}
