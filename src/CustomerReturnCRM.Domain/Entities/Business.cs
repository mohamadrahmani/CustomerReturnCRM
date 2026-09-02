using CustomerReturnCRM.Domain.Common;

namespace CustomerReturnCRM.Domain.Entities;

public sealed class Business : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string BusinessType { get; set; } = null!;
    public string Mobile { get; set; } = null!;
    public string? Address { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<BusinessMember> Members { get; set; } = new List<BusinessMember>();
    public ICollection<Staff> Staff { get; set; } = new List<Staff>();
}

