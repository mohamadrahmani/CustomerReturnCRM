using CustomerReturnCRM.Domain.Common;

namespace CustomerReturnCRM.Domain.Entities;

public sealed class BusinessMember : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = null!;
    public Business Business { get; set; } = null!;
}
