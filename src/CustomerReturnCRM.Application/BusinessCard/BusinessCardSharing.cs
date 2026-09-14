namespace CustomerReturnCRM.Application.BusinessCard;

public enum BusinessCardShareChannel
{
    Sms = 1,
    Bale = 2
}

public sealed record BusinessCardShareResult(
    string PublicUrl,
    BusinessCardShareChannel Channel,
    bool Sent,
    string? Error);

public interface IBusinessCardSharingService
{
    Task<BusinessCardShareResult?> ShareAsync(
        Guid businessId,
        Guid customerId,
        Guid userId,
        BusinessCardShareChannel channel,
        CancellationToken cancellationToken = default);
}
