using CustomerReturnCRM.Application.Common;
using CustomerReturnCRM.Application.Sms;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.Sms;

public sealed class SmsManagementService : ISmsManagementService
{
    private readonly ApplicationDbContext _dbContext;

    public SmsManagementService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<SmsTemplateResult>> ListTemplatesAsync(Guid businessId, Guid userId, bool activeOnly, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var query = _dbContext.SmsTemplates.AsNoTracking().Where(x => x.BusinessId == businessId);
        if (activeOnly) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Name)
            .Select(x => new SmsTemplateResult(x.Id, x.BusinessId, x.Name, x.Content, x.IsActive, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<SmsTemplateResult?> GetTemplateAsync(Guid businessId, Guid templateId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        return await _dbContext.SmsTemplates.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.Id == templateId)
            .Select(x => new SmsTemplateResult(x.Id, x.BusinessId, x.Name, x.Content, x.IsActive, x.CreatedAt, x.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SmsTemplateResult> CreateTemplateAsync(Guid businessId, Guid userId, CreateSmsTemplateRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var name = Required(request.Name, "Template name is required.");
        var content = Required(request.Content, "Template content is required.");
        if (await _dbContext.SmsTemplates.AnyAsync(x => x.BusinessId == businessId && x.Name == name, cancellationToken))
            throw new ArgumentException("A template with this name already exists.");

        var template = new SmsTemplate { Id = Guid.NewGuid(), BusinessId = businessId, Name = name, Content = content, IsActive = true, CreatedAt = DateTime.UtcNow };
        _dbContext.SmsTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetTemplateAsync(businessId, template.Id, userId, cancellationToken))!;
    }

    public async Task<SmsTemplateResult?> UpdateTemplateAsync(Guid businessId, Guid templateId, Guid userId, UpdateSmsTemplateRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var template = await _dbContext.SmsTemplates.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == templateId, cancellationToken);
        if (template is null) return null;
        var name = Required(request.Name, "Template name is required.");
        var content = Required(request.Content, "Template content is required.");
        if (await _dbContext.SmsTemplates.AnyAsync(x => x.BusinessId == businessId && x.Id != templateId && x.Name == name, cancellationToken))
            throw new ArgumentException("A template with this name already exists.");
        template.Name = name;
        template.Content = content;
        template.IsActive = request.IsActive;
        template.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetTemplateAsync(businessId, templateId, userId, cancellationToken);
    }

    public async Task<SmsCampaignResult> CreateCampaignAsync(Guid businessId, Guid userId, CreateSmsCampaignRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var message = Required(request.Message, "Message is required.");
        if (message.Length > 2000) throw new ArgumentException("Message cannot exceed 2000 characters.");
        if (request.CustomerIds.Count == 0) throw new ArgumentException("At least one customer is required.");
        if (request.CustomerIds.Count > 10000) throw new ArgumentException("A campaign cannot contain more than 10000 recipients.");
        if (request.ScheduledAt.HasValue && request.ScheduledAt.Value <= DateTime.UtcNow) throw new ArgumentException("Scheduled time must be in the future.");

        SmsTemplate? template = null;
        if (request.TemplateId.HasValue)
        {
            template = await _dbContext.SmsTemplates.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == request.TemplateId.Value && x.IsActive, cancellationToken);
            if (template is null) throw new ArgumentException("Template does not belong to the business or is inactive.");
        }

        var ids = request.CustomerIds.Distinct().ToArray();
        var customers = await _dbContext.Customers.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.IsActive && ids.Contains(x.Id))
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.Mobile })
            .ToListAsync(cancellationToken);
        if (customers.Count != ids.Length) throw new ArgumentException("One or more customers do not belong to the business, are inactive, or do not exist.");
        if (customers.Any(x => string.IsNullOrWhiteSpace(x.Mobile))) throw new ArgumentException("All selected customers must have a mobile number.");

        var businessName = await _dbContext.Businesses.Where(x => x.Id == businessId).Select(x => x.Name).SingleAsync(cancellationToken);
        var campaign = new SmsCampaign
        {
            Id = Guid.NewGuid(), BusinessId = businessId, TemplateId = template?.Id, CreatedByUserId = userId,
            Name = Normalize(request.Name), Message = message, ScheduledAt = request.ScheduledAt ?? DateTime.UtcNow,
            Status = SmsCampaignStatus.Scheduled, CreatedAt = DateTime.UtcNow
        };

        foreach (var customer in customers)
        {
            var fullName = ($"{customer.FirstName} {customer.LastName}").Trim();
            campaign.Recipients.Add(new SmsRecipient
            {
                Id = Guid.NewGuid(), SmsCampaignId = campaign.Id, CustomerId = customer.Id, Mobile = customer.Mobile.Trim(),
                RenderedMessage = Render(message, customer.FirstName, customer.LastName, fullName, businessName),
                Status = SmsRecipientStatus.Pending, CreatedAt = DateTime.UtcNow
            });
        }

        _dbContext.SmsCampaigns.Add(campaign);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetCampaignAsync(businessId, campaign.Id, userId, true, cancellationToken))!;
    }

    public async Task<SmsCampaignResult?> GetCampaignAsync(Guid businessId, Guid campaignId, Guid userId, bool includeRecipients, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var campaign = await _dbContext.SmsCampaigns.AsNoTracking()
            .Include(x => x.Recipients).ThenInclude(x => x.Customer)
            .SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == campaignId, cancellationToken);
        return campaign is null ? null : Map(campaign, includeRecipients);
    }

    public async Task<PagedResult<SmsCampaignResult>> ListCampaignsAsync(Guid businessId, Guid userId, SmsCampaignStatus? status, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var query = _dbContext.SmsCampaigns.AsNoTracking().Where(x => x.BusinessId == businessId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var total = await query.CountAsync(cancellationToken);
        var campaigns = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Include(x => x.Recipients).ThenInclude(x => x.Customer).ToListAsync(cancellationToken);
        return Pagination.Create(campaigns.Select(x => Map(x, false)).ToList(), page, pageSize, total);
    }

    public async Task<SmsCampaignResult?> CancelCampaignAsync(Guid businessId, Guid campaignId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var campaign = await _dbContext.SmsCampaigns.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == campaignId, cancellationToken);
        if (campaign is null) return null;
        if (campaign.Status != SmsCampaignStatus.Scheduled) throw new InvalidOperationException("Only scheduled campaigns can be cancelled.");
        campaign.Status = SmsCampaignStatus.Cancelled;
        campaign.CancelledAt = DateTime.UtcNow;
        campaign.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetCampaignAsync(businessId, campaignId, userId, true, cancellationToken);
    }

    private static SmsCampaignResult Map(SmsCampaign campaign, bool includeRecipients)
    {
        var recipients = includeRecipients ? campaign.Recipients.Select(x => new SmsRecipientResult(x.Id, x.CustomerId, ($"{x.Customer.FirstName} {x.Customer.LastName}").Trim(), x.Mobile, x.RenderedMessage, x.Status, x.ProviderMessageId, x.SubmittedAt, x.DeliveredAt, x.FailureReason)).ToList() : null;
        return new SmsCampaignResult(campaign.Id, campaign.BusinessId, campaign.TemplateId, campaign.CreatedByUserId, campaign.Name, campaign.Message, campaign.ScheduledAt, campaign.Status, campaign.StartedAt, campaign.CompletedAt, campaign.CancelledAt, campaign.CreatedAt, campaign.UpdatedAt, campaign.Recipients.Count, campaign.Recipients.Count(x => x.Status == SmsRecipientStatus.Submitted || x.Status == SmsRecipientStatus.Delivered), campaign.Recipients.Count(x => x.Status == SmsRecipientStatus.Delivered), campaign.Recipients.Count(x => x.Status == SmsRecipientStatus.Failed), recipients);
    }

    private async Task EnsureMemberAsync(Guid businessId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.BusinessMembers.AnyAsync(x => x.BusinessId == businessId && x.UserId == userId, cancellationToken))
            throw new UnauthorizedAccessException("The user is not a member of this business.");
    }

    private static string Required(string? value, string message) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException(message) : value.Trim();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Render(string message, string firstName, string? lastName, string fullName, string businessName) =>
        message.Replace("[نام]", firstName, StringComparison.Ordinal)
            .Replace("[نام خانوادگی]", lastName ?? string.Empty, StringComparison.Ordinal)
            .Replace("[نام کامل]", fullName, StringComparison.Ordinal)
            .Replace("[نام کسب‌وکار]", businessName, StringComparison.Ordinal);
}
