using CustomerReturnCRM.Domain.Common;

namespace CustomerReturnCRM.Domain.Entities;

public sealed class SmsTemplate : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    public Business Business { get; set; } = null!;
}
