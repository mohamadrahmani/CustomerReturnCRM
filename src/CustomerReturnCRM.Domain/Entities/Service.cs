using CustomerReturnCRM.Domain.Common;

namespace CustomerReturnCRM.Domain.Entities;

public sealed class Service : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public decimal DefaultPrice { get; set; }
    public int DefaultDurationMinutes { get; set; }
    public int? SuggestedReturnDays { get; set; }
    public bool IsActive { get; set; } = true;
    public Business Business { get; set; } = null!;
}
