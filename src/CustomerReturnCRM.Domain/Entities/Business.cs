using CustomerReturnCRM.Domain.Common;

namespace CustomerReturnCRM.Domain.Entities;

public sealed class Business : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string BusinessType { get; set; } = null!;
    public string Mobile { get; set; } = null!;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Description { get; set; }
    public string? PublicSlug { get; set; }
    public bool PublicBookingEnabled { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Product license is separate from operational status and public booking settings.
    public bool LicenseIsActive { get; set; } = true;
    public DateTime? LicenseStartDate { get; set; }
    public DateTime? LicenseEndDate { get; set; }
    public string? LicenseAdminNote { get; set; }

    public ICollection<BusinessMember> Members { get; set; } = new List<BusinessMember>();
    public ICollection<Staff> Staff { get; set; } = new List<Staff>();
    public ICollection<BusinessWorkingHour> WorkingHours { get; set; } = new List<BusinessWorkingHour>();
}
