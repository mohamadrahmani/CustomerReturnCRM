namespace CustomerReturnCRM.Domain.Entities;

public sealed class AppointmentService
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid StaffId { get; set; }
    public string ServiceTitle { get; set; } = null!;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public Appointment Appointment { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public Staff Staff { get; set; } = null!;
}
