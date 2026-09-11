namespace CustomerReturnCRM.Application.Sms;

public interface ISmsService
{
    Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken ct = default);
    Task<SmsSendResult> SendLikeToLikeAsync(SmsLikeToLikeRequest request, CancellationToken ct = default);
    Task<SmsSendResult> SendPatternAsync(SmsPatternRequest request, CancellationToken ct = default);
    Task<SmsDeliveryResult> GetStatusAsync(string providerMessageId, CancellationToken ct = default);
    Task<SmsCreditResult> GetCreditAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SmsLine>> GetLinesAsync(CancellationToken ct = default);
}
