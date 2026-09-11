using CustomerReturnCRM.Application.Sms;
using Microsoft.Extensions.Configuration;

namespace CustomerReturnCRM.Infrastructure.Sms;

public sealed class ConfiguredSmsProvider : ISmsProvider
{
    private readonly ISmsProviderResolver _resolver;
    private readonly SmsProviderType _providerType;

    public SmsProviderType Type => _providerType;

    public ConfiguredSmsProvider(ISmsProviderResolver resolver, IConfiguration configuration)
    {
        _resolver = resolver;
        var configured = configuration["Sms:Provider"];
        _providerType = Enum.TryParse<SmsProviderType>(configured, true, out var value) ? value : SmsProviderType.SmsIr;
    }

    private ISmsProvider Provider => _resolver.Resolve(_providerType);

    public Task<IReadOnlyCollection<SmsProviderResult>> SendAsync(IReadOnlyCollection<SmsProviderMessage> messages, CancellationToken cancellationToken = default)
        => Provider.SendAsync(messages, cancellationToken);
    public Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken ct = default) => Provider.SendAsync(request, ct);
    public Task<SmsSendResult> SendLikeToLikeAsync(SmsLikeToLikeRequest request, CancellationToken ct = default) => Provider.SendLikeToLikeAsync(request, ct);
    public Task<SmsSendResult> SendPatternAsync(SmsPatternRequest request, CancellationToken ct = default) => Provider.SendPatternAsync(request, ct);
    public Task<SmsDeliveryResult> GetStatusAsync(string providerMessageId, CancellationToken ct = default) => Provider.GetStatusAsync(providerMessageId, ct);
    public Task<SmsCreditResult> GetCreditAsync(CancellationToken ct = default) => Provider.GetCreditAsync(ct);
    public Task<IReadOnlyList<SmsLine>> GetLinesAsync(CancellationToken ct = default) => Provider.GetLinesAsync(ct);
}
