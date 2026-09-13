namespace CustomerReturnCRM.Application.Bale;

public sealed record BaleConnectInviteResult(Guid CustomerId, string ConnectUrl, DateTime ExpiresAtUtc);

public sealed record BaleSendMessageRequest(long ChatId, string Text);

public interface IBaleService
{
    Task<BaleConnectInviteResult?> CreateConnectInviteAsync(Guid businessId, Guid customerId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HandleUpdateAsync(string updateJson, CancellationToken cancellationToken = default);
    Task<bool> SendMessageAsync(BaleSendMessageRequest request, CancellationToken cancellationToken = default);
}
