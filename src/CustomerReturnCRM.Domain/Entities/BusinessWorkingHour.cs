namespace CustomerReturnCRM.Domain.Entities;

public sealed class BusinessWorkingHour
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public Business Business { get; set; } = null!;
}
