using CustomerReturnCRM.Application.Common;
using CustomerReturnCRM.Domain.Entities;

namespace CustomerReturnCRM.Application.Sms;

public sealed record SmsTemplateResult(Guid Id, Guid BusinessId, string Name, string Content, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed class CreateSmsTemplateRequest
{
    public string Name { get; init; } = null!;
    public string Content { get; init; } = null!;
}

public sealed class UpdateSmsTemplateRequest
{
    public string Name { get; init; } = null!;
    public string Content { get; init; } = null!;
    public bool IsActive { get; init; } = true;
}

public sealed record SmsRecipientResult(Guid Id, Guid CustomerId, string CustomerName, string Mobile, string? RenderedMessage, SmsRecipientStatus Status, string? ProviderMessageId, DateTime? SubmittedAt, DateTime? DeliveredAt, string? FailureReason);

public sealed record SmsCampaignResult(Guid Id, Guid BusinessId, Guid? TemplateId, Guid CreatedByUserId, string? Name, string Message, DateTime? ScheduledAt, SmsCampaignStatus Status, DateTime? StartedAt, DateTime? CompletedAt, DateTime? CancelledAt, DateTime CreatedAt, DateTime? UpdatedAt, int RecipientCount, int AcceptedCount, int DeliveredCount, int FailedCount, IReadOnlyList<SmsRecipientResult>? Recipients);

public sealed class CreateSmsCampaignRequest
{
    public Guid? TemplateId { get; init; }
    public string? Name { get; init; }
    public string Message { get; init; } = null!;
    public DateTime? ScheduledAt { get; init; }
    public IReadOnlyCollection<Guid> CustomerIds { get; init; } = Array.Empty<Guid>();
}

public interface ISmsManagementService
{
    Task<IReadOnlyList<SmsTemplateResult>> ListTemplatesAsync(Guid businessId, Guid userId, bool activeOnly, CancellationToken cancellationToken = default);
    Task<SmsTemplateResult?> GetTemplateAsync(Guid businessId, Guid templateId, Guid userId, CancellationToken cancellationToken = default);
    Task<SmsTemplateResult> CreateTemplateAsync(Guid businessId, Guid userId, CreateSmsTemplateRequest request, CancellationToken cancellationToken = default);
    Task<SmsTemplateResult?> UpdateTemplateAsync(Guid businessId, Guid templateId, Guid userId, UpdateSmsTemplateRequest request, CancellationToken cancellationToken = default);
    Task<SmsCampaignResult> CreateCampaignAsync(Guid businessId, Guid userId, CreateSmsCampaignRequest request, CancellationToken cancellationToken = default);
    Task<SmsCampaignResult?> GetCampaignAsync(Guid businessId, Guid campaignId, Guid userId, bool includeRecipients, CancellationToken cancellationToken = default);
    Task<PagedResult<SmsCampaignResult>> ListCampaignsAsync(Guid businessId, Guid userId, SmsCampaignStatus? status, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<SmsCampaignResult?> CancelCampaignAsync(Guid businessId, Guid campaignId, Guid userId, CancellationToken cancellationToken = default);
}
