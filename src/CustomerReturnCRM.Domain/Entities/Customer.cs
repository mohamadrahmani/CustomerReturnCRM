using CustomerReturnCRM.Domain.Common;

namespace CustomerReturnCRM.Domain.Entities;

public sealed class Customer : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public string FirstName { get; set; } = null!;
    public string? LastName { get; set; }
    public string Mobile { get; set; } = null!;
    public DateTime? BirthDate { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
    public Business Business { get; set; } = null!;
}
