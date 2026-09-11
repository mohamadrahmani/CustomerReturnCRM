using CustomerReturnCRM.Application.Sms;
using Microsoft.Extensions.Logging;

namespace CustomerReturnCRM.Infrastructure.Sms;

public sealed class LoggingSmsProvider : ISmsProvider
{
    private readonly ILogger<LoggingSmsProvider> _logger;
    public SmsProviderType Type => SmsProviderType.Development;

    public LoggingSmsProvider(ILogger<LoggingSmsProvider> logger) => _logger = logger;

    public Task<IReadOnlyCollection<SmsProviderResult>> SendAsync(IReadOnlyCollection<SmsProviderMessage> messages, CancellationToken cancellationToken = default)
    {
        var results = new List<SmsProviderResult>(messages.Count);
        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var providerMessageId = $"dev-{Guid.NewGuid():N}";
            _logger.LogInformation("Development SMS provider accepted message {ProviderMessageId} for recipient {RecipientId} to {Mobile}.", providerMessageId, message.RecipientId, MaskMobile(message.Mobile));
            results.Add(new SmsProviderResult(message.RecipientId, true, providerMessageId, null));
        }
        return Task.FromResult<IReadOnlyCollection<SmsProviderResult>>(results);
    }

    public Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken ct = default)
        => Task.FromResult(new SmsSendResult(request.Mobiles.Select(m => new SmsSendItemResult(m, true, $"dev-{Guid.NewGuid():N}", null, null, null, null)).ToArray()));
    public Task<SmsSendResult> SendLikeToLikeAsync(SmsLikeToLikeRequest request, CancellationToken ct = default)
        => Task.FromResult(new SmsSendResult(request.Mobiles.Select(m => new SmsSendItemResult(m, true, $"dev-{Guid.NewGuid():N}", null, null, null, null)).ToArray()));
    public Task<SmsSendResult> SendPatternAsync(SmsPatternRequest request, CancellationToken ct = default)
        => Task.FromResult(new SmsSendResult(new[] { new SmsSendItemResult(request.Mobile, true, $"dev-{Guid.NewGuid():N}", null, null, null, null) }));
    public Task<SmsDeliveryResult> GetStatusAsync(string providerMessageId, CancellationToken ct = default)
        => Task.FromResult(new SmsDeliveryResult(providerMessageId, SmsDeliveryStatus.Delivered, 1, null, null, DateTime.UtcNow));
    public Task<SmsCreditResult> GetCreditAsync(CancellationToken ct = default) => Task.FromResult(new SmsCreditResult(0));
    public Task<IReadOnlyList<SmsLine>> GetLinesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SmsLine>>(Array.Empty<SmsLine>());

    private static string MaskMobile(string mobile) => mobile.Length <= 4 ? "****" : new string('*', Math.Max(0, mobile.Length - 4)) + mobile[^4..];
}
