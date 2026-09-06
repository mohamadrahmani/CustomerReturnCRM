using CustomerReturnCRM.Application.Sms;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CustomerReturnCRM.Infrastructure.Sms;

public sealed class SmsSendingBackgroundService : BackgroundService
{
    private const int CampaignBatchSize = 10;
    private const int RecipientBatchSize = 100;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SmsSendingBackgroundService> _logger;

    public SmsSendingBackgroundService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<SmsSendingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SMS sending background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueCampaignsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error while processing SMS campaigns.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("SMS sending background service stopped.");
    }

    private async Task ProcessDueCampaignsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var provider = scope.ServiceProvider.GetRequiredService<ISmsProvider>();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var campaignIds = await dbContext.SmsCampaigns
            .Where(x => x.Status == SmsCampaignStatus.Scheduled && x.ScheduledAt <= now)
            .OrderBy(x => x.ScheduledAt)
            .ThenBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .Take(CampaignBatchSize)
            .ToListAsync(cancellationToken);

        foreach (var campaignId in campaignIds)
        {
            await ProcessCampaignAsync(dbContext, provider, campaignId, cancellationToken);
        }
    }

    private async Task ProcessCampaignAsync(
        ApplicationDbContext dbContext,
        ISmsProvider provider,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var campaign = await dbContext.SmsCampaigns
            .Include(x => x.Recipients)
            .SingleOrDefaultAsync(x => x.Id == campaignId, cancellationToken);

        if (campaign is null || campaign.Status != SmsCampaignStatus.Scheduled)
            return;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        campaign.Status = SmsCampaignStatus.Sending;
        campaign.StartedAt = now;
        campaign.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var pendingRecipients = campaign.Recipients
                .Where(x => x.Status == SmsRecipientStatus.Pending)
                .OrderBy(x => x.CreatedAt)
                .Take(RecipientBatchSize)
                .ToList();

            while (pendingRecipients.Count > 0)
            {
                var messages = pendingRecipients
                    .Select(x => new SmsProviderMessage(
                        x.Id,
                        x.Mobile,
                        x.RenderedMessage ?? campaign.Message))
                    .ToArray();

                var results = await provider.SendAsync(messages, cancellationToken);
                var resultsByRecipient = results.ToDictionary(x => x.RecipientId);

                foreach (var recipient in pendingRecipients)
                {
                    if (!resultsByRecipient.TryGetValue(recipient.Id, out var result))
                    {
                        recipient.Status = SmsRecipientStatus.Failed;
                        recipient.FailureReason = "The SMS provider did not return a result for this recipient.";
                        continue;
                    }

                    if (result.Accepted)
                    {
                        recipient.Status = SmsRecipientStatus.Submitted;
                        recipient.ProviderMessageId = result.ProviderMessageId;
                        recipient.SubmittedAt = now;
                        recipient.FailureReason = null;
                    }
                    else
                    {
                        recipient.Status = SmsRecipientStatus.Failed;
                        recipient.FailureReason = result.FailureReason ?? "The SMS provider rejected the message.";
                    }

                    recipient.UpdatedAt = now;
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                pendingRecipients = campaign.Recipients
                    .Where(x => x.Status == SmsRecipientStatus.Pending)
                    .OrderBy(x => x.CreatedAt)
                    .Take(RecipientBatchSize)
                    .ToList();
            }

            var failedCount = campaign.Recipients.Count(x => x.Status == SmsRecipientStatus.Failed);
            var acceptedCount = campaign.Recipients.Count(x =>
                x.Status == SmsRecipientStatus.Submitted || x.Status == SmsRecipientStatus.Delivered);

            campaign.Status = failedCount == 0
                ? SmsCampaignStatus.Completed
                : acceptedCount == 0
                    ? SmsCampaignStatus.Failed
                    : SmsCampaignStatus.PartiallyFailed;
            campaign.CompletedAt = _timeProvider.GetUtcNow().UtcDateTime;
            campaign.UpdatedAt = campaign.CompletedAt;
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "SMS campaign {CampaignId} completed with status {Status}. Accepted: {AcceptedCount}, Failed: {FailedCount}.",
                campaign.Id,
                campaign.Status,
                acceptedCount,
                failedCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "SMS campaign {CampaignId} failed while sending.", campaign.Id);

            foreach (var recipient in campaign.Recipients.Where(x => x.Status == SmsRecipientStatus.Pending))
            {
                recipient.Status = SmsRecipientStatus.Failed;
                recipient.FailureReason = "SMS sending failed unexpectedly. Check application logs for details.";
                recipient.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            }

            campaign.Status = campaign.Recipients.Any(x =>
                x.Status == SmsRecipientStatus.Submitted || x.Status == SmsRecipientStatus.Delivered)
                ? SmsCampaignStatus.PartiallyFailed
                : SmsCampaignStatus.Failed;
            campaign.CompletedAt = _timeProvider.GetUtcNow().UtcDateTime;
            campaign.UpdatedAt = campaign.CompletedAt;
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }
}
