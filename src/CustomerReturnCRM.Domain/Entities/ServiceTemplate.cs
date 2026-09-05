using CustomerReturnCRM.Domain.Common;

namespace CustomerReturnCRM.Domain.Entities;

public sealed class ServiceTemplate : AuditableEntity
{
    public string BusinessType { get; set; } = null!;
    public string Title { get; set; } = null!;
    public int DefaultDurationMinutes { get; set; }
    public int? SuggestedReturnDays { get; set; }
    public bool IsActive { get; set; } = true;
}
