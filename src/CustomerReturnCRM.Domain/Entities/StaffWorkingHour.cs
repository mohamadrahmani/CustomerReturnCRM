namespace CustomerReturnCRM.Domain.Entities;

public sealed class StaffWorkingHour
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public Staff Staff { get; set; } = null!;
}
