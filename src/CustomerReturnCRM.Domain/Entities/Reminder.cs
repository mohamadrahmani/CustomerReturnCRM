using CustomerReturnCRM.Domain.Common;

namespace CustomerReturnCRM.Domain.Entities;

public sealed class Reminder : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? ServiceId { get; set; }
    public string Title { get; set; } = null!;
    public DateTime DueAt { get; set; }
    public ReminderStatus Status { get; set; } = ReminderStatus.Pending;
    public string? Note { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Business Business { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Service? Service { get; set; }
}
