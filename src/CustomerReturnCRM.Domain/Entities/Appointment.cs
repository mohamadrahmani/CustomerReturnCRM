using CustomerReturnCRM.Domain.Common;

namespace CustomerReturnCRM.Domain.Entities;

public sealed class Appointment : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Note { get; set; }
    public Business Business { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
}
