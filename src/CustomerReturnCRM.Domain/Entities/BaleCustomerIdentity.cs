using CustomerReturnCRM.Domain.Common;

namespace CustomerReturnCRM.Domain.Entities;

public sealed class BaleCustomerIdentity : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public long BaleUserId { get; set; }
    public long BaleChatId { get; set; }
    public string? Username { get; set; }
    public DateTime ConnectedAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public Business Business { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}
