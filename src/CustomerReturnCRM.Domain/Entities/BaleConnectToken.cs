using CustomerReturnCRM.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Domain.Entities;

[Index(nameof(TokenHash), IsUnique = true)]
[Index(nameof(BusinessId), nameof(CustomerId), nameof(ExpiresAtUtc))]
public sealed class BaleConnectToken : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public bool IsRevoked { get; set; }
    public Business Business { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}
