using CustomerReturnCRM.Domain.Common;

namespace CustomerReturnCRM.Domain.Entities;

public sealed class SmsCampaign : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public Guid? TemplateId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? Name { get; set; }
    public string Message { get; set; } = null!;
    public DateTime? ScheduledAt { get; set; }
    public SmsCampaignStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public Business Business { get; set; } = null!;
    public SmsTemplate? Template { get; set; }
    public ICollection<SmsRecipient> Recipients { get; set; } = new List<SmsRecipient>();
}
