namespace CustomerReturnCRM.Application.Sms;

public sealed record SmsProviderMessage(
    Guid RecipientId,
    string Mobile,
    string Message);

public sealed record SmsProviderResult(
    Guid RecipientId,
    bool Accepted,
    string? ProviderMessageId,
    string? FailureReason);

public interface ISmsProvider
{
    Task<IReadOnlyCollection<SmsProviderResult>> SendAsync(
        IReadOnlyCollection<SmsProviderMessage> messages,
        CancellationToken cancellationToken = default);
}
