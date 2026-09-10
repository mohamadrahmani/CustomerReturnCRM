using CustomerReturnCRM.Application.Sms;
using Microsoft.Extensions.Logging;

namespace CustomerReturnCRM.Infrastructure.Sms;

public sealed class LoggingSmsProvider : ISmsProvider
{
    private readonly ILogger<LoggingSmsProvider> _logger;

    public LoggingSmsProvider(ILogger<LoggingSmsProvider> logger) => _logger = logger;

    public Task<IReadOnlyCollection<SmsProviderResult>> SendAsync(
        IReadOnlyCollection<SmsProviderMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SmsProviderResult>(messages.Count);

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var providerMessageId = $"dev-{Guid.NewGuid():N}";
            _logger.LogInformation(
                "SMS provider accepted message {ProviderMessageId} for recipient {RecipientId} to {Mobile}: {Message}",
                providerMessageId,
                message.RecipientId,
                message.Mobile,
                message.Message);
            results.Add(new SmsProviderResult(message.RecipientId, true, providerMessageId, null));
        }

        return Task.FromResult<IReadOnlyCollection<SmsProviderResult>>(results);
    }
}
