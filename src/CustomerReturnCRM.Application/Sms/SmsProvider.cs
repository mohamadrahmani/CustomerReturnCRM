namespace CustomerReturnCRM.Application.Sms;

public sealed record SmsProviderMessage(Guid RecipientId, string Mobile, string Message);
public sealed record SmsProviderResult(Guid RecipientId, bool Accepted, string? ProviderMessageId, string? FailureReason);

public enum SmsProviderType
{
    SmsIr = 1
}

public enum SmsMessageType
{
    Text = 1,
    Pattern = 2,
    Otp = 3,
    Reminder = 4,
    Confirmation = 5,
    Cancellation = 6,
    Reschedule = 7,
    Birthday = 8,
    Marketing = 9
}

public enum SmsDeliveryStatus
{
    Queued = 1,
    Processing = 2,
    SentToOperator = 3,
    Delivered = 4,
    Failed = 5,
    Blacklisted = 6
}

public sealed record SmsSendRequest(IReadOnlyCollection<string> Mobiles, string Message, SmsMessageType MessageType = SmsMessageType.Text);
public sealed record SmsLikeToLikeRequest(IReadOnlyCollection<string> Mobiles, IReadOnlyCollection<string> Messages, SmsMessageType MessageType = SmsMessageType.Text);
public sealed record SmsPatternRequest(string Mobile, string PatternId, IReadOnlyDictionary<string, string> Parameters, SmsMessageType MessageType = SmsMessageType.Pattern);
public sealed record SmsSendItemResult(string Mobile, bool Accepted, string? ProviderMessageId, string? ProviderPackId, decimal? Cost, string? ErrorCode, string? ErrorMessage);
public sealed record SmsSendResult(IReadOnlyCollection<SmsSendItemResult> Items);
public sealed record SmsDeliveryResult(string ProviderMessageId, SmsDeliveryStatus Status, int? ProviderStatusCode, string? ErrorCode, string? ErrorMessage, DateTime? DeliveredAt);
public sealed record SmsCreditResult(decimal Credit);
public sealed record SmsLine(string Number, string? Type, bool? IsActive);

public interface ISmsProvider
{
    SmsProviderType Type { get; }
    Task<IReadOnlyCollection<SmsProviderResult>> SendAsync(IReadOnlyCollection<SmsProviderMessage> messages, CancellationToken cancellationToken = default);
    Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken ct = default);
    Task<SmsSendResult> SendLikeToLikeAsync(SmsLikeToLikeRequest request, CancellationToken ct = default);
    Task<SmsSendResult> SendPatternAsync(SmsPatternRequest request, CancellationToken ct = default);
    Task<SmsDeliveryResult> GetStatusAsync(string providerMessageId, CancellationToken ct = default);
    Task<SmsCreditResult> GetCreditAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SmsLine>> GetLinesAsync(CancellationToken ct = default);
}

public interface ISmsProviderResolver
{
    ISmsProvider Resolve(SmsProviderType providerType);
}
