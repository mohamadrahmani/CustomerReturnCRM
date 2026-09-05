using CustomerReturnCRM.Domain.Common;

namespace CustomerReturnCRM.Domain.Entities;

public sealed class SmartListDismissal : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? ServiceId { get; set; }
    public string SmartListType { get; set; } = null!;
    public DateTime LastVisitAt { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public Guid DismissedByUserId { get; set; }
    public Business Business { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Service? Service { get; set; }
}
