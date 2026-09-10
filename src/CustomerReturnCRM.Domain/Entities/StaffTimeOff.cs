namespace CustomerReturnCRM.Domain.Entities;

public sealed class StaffTimeOff
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Reason { get; set; }
    public Staff Staff { get; set; } = null!;
}
