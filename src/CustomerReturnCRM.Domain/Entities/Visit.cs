using CustomerReturnCRM.Domain.Common;

namespace CustomerReturnCRM.Domain.Entities;

public sealed class Visit : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? AppointmentId { get; set; }
    public DateTime VisitAt { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Note { get; set; }
    public Business Business { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Appointment? Appointment { get; set; }
    public ICollection<VisitService> VisitServices { get; set; } = new List<VisitService>();
}
