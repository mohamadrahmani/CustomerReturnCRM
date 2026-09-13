using CustomerReturnCRM.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Domain.Entities;

[Index(nameof(BusinessId), nameof(CustomerId), IsUnique = true)]
[Index(nameof(BusinessId), nameof(BaleUserId), IsUnique = true)]
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
