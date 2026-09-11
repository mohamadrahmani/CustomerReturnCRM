using CustomerReturnCRM.Application.Sms;

namespace CustomerReturnCRM.Infrastructure.Sms;

public sealed class SmsProviderResolver : ISmsProviderResolver
{
    private readonly IReadOnlyDictionary<SmsProviderType, ISmsProvider> _providers;

    public SmsProviderResolver(IEnumerable<ISmsProvider> providers)
    {
        _providers = providers.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.Single());
    }

    public ISmsProvider Resolve(SmsProviderType providerType)
        => _providers.TryGetValue(providerType, out var provider)
            ? provider
            : throw new InvalidOperationException($"SMS provider '{providerType}' is not registered.");
}
