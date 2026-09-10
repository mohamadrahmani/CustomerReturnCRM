using CustomerReturnCRM.Domain.Common;

namespace CustomerReturnCRM.Domain.Entities;

public sealed class SmsRecipient : AuditableEntity
{
    public Guid SmsCampaignId { get; set; }
    public Guid CustomerId { get; set; }
    public string Mobile { get; set; } = null!;
    public string? RenderedMessage { get; set; }
    public SmsRecipientStatus Status { get; set; }
    public string? ProviderMessageId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? FailureReason { get; set; }

    public SmsCampaign SmsCampaign { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}
