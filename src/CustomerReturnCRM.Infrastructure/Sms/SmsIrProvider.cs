using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CustomerReturnCRM.Application.Sms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CustomerReturnCRM.Infrastructure.Sms;

public sealed class SmsIrProvider : ISmsProvider
{
    private const string BaseUrl = "https://api.sms.ir/v1/";
    private readonly HttpClient _httpClient;
    private readonly ILogger<SmsIrProvider> _logger;
    private readonly string _apiKey;
    private readonly string _lineNumber;

    public SmsProviderType Type => SmsProviderType.SmsIr;

    public SmsIrProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<SmsIrProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(SmsIrProvider));
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _logger = logger;
        _apiKey = configuration["SMSApiKey"] ?? configuration["Sms:SmsIr:ApiKey"] ?? string.Empty;
        _lineNumber = configuration["SMSLineNumber"] ?? configuration["Sms:SmsIr:LineNumber"] ?? string.Empty;
    }

    public async Task<IReadOnlyCollection<SmsProviderResult>> SendAsync(IReadOnlyCollection<SmsProviderMessage> messages, CancellationToken cancellationToken = default)
    {
        var results = new List<SmsProviderResult>(messages.Count);
        foreach (var batch in messages.Chunk(100))
        {
            var request = new { lineNumber = ParseLineNumber(), messageText = batch.First().Message, mobiles = batch.Select(x => x.Mobile).ToArray(), sendDateTime = (DateTime?)null };
            var response = await SendWithRetryAsync(HttpMethod.Post, "send/bulk", request, cancellationToken);
            var accepted = IsSuccess(response.StatusCode);
            var messageId = ExtractString(response.Json, "messageId", "id");
            var packId = ExtractString(response.Json, "packId");
            foreach (var item in batch)
                results.Add(new SmsProviderResult(item.RecipientId, accepted, messageId ?? packId, accepted ? null : BuildError(response)));
        }
        return results;
    }

    public async Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken ct = default)
    {
        if (request.Mobiles.Count == 0) return new SmsSendResult(Array.Empty<SmsSendItemResult>());
        var all = new List<SmsSendItemResult>();
        foreach (var batch in request.Mobiles.Chunk(100))
        {
            var response = await SendWithRetryAsync(HttpMethod.Post, "send/bulk", new { lineNumber = ParseLineNumber(), messageText = request.Message, mobiles = batch.ToArray(), sendDateTime = (DateTime?)null }, ct);
            all.AddRange(ToSendResult(batch.ToArray(), response).Items);
        }
        return new SmsSendResult(all);
    }

    public async Task<SmsSendResult> SendLikeToLikeAsync(SmsLikeToLikeRequest request, CancellationToken ct = default)
    {
        if (request.Mobiles.Count != request.Messages.Count) throw new ArgumentException("The number of mobiles and messages must be equal.");
        if (request.Mobiles.Count == 0) return new SmsSendResult(Array.Empty<SmsSendItemResult>());
        var mobiles = request.Mobiles.ToArray();
        var messages = request.Messages.ToArray();
        var all = new List<SmsSendItemResult>();
        foreach (var indexes in Enumerable.Range(0, mobiles.Length).Chunk(100))
        {
            var batchMobiles = indexes.Select(i => mobiles[i]).ToArray();
            var batchMessages = indexes.Select(i => messages[i]).ToArray();
            var response = await SendWithRetryAsync(HttpMethod.Post, "send/likeToLike", new { lineNumber = ParseLineNumber(), messageTexts = batchMessages, mobiles = batchMobiles }, ct);
            all.AddRange(ToSendResult(batchMobiles, response).Items);
        }
        return new SmsSendResult(all);
    }

    public async Task<SmsSendResult> SendPatternAsync(SmsPatternRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.PatternId)) throw new ArgumentException("PatternId is required.");
        var parameters = request.Parameters.Select(x => new { name = x.Key, value = x.Value }).ToArray();
        var response = await SendWithRetryAsync(HttpMethod.Post, "send/verify", new { mobile = request.Mobile, templateId = ParsePatternId(request.PatternId), parameters }, ct);
        return ToSendResult(new[] { request.Mobile }, response);
    }

    public async Task<SmsDeliveryResult> GetStatusAsync(string providerMessageId, CancellationToken ct = default)
    {
        var response = await SendWithRetryAsync(HttpMethod.Get, $"send/{Uri.EscapeDataString(providerMessageId)}", null, ct);
        var code = ExtractInt(response.Json, "status", "statusCode");
        return new SmsDeliveryResult(providerMessageId, MapStatus(code), code, IsSuccess(response.StatusCode) ? null : ExtractString(response.Json, "errorCode", "code"), IsSuccess(response.StatusCode) ? null : BuildError(response), MapStatus(code) == SmsDeliveryStatus.Delivered ? DateTime.UtcNow : null);
    }

    public async Task<SmsCreditResult> GetCreditAsync(CancellationToken ct = default)
    {
        var response = await SendWithRetryAsync(HttpMethod.Get, "credit", null, ct);
        if (!IsSuccess(response.StatusCode)) throw new InvalidOperationException(BuildError(response));
        return new SmsCreditResult(ExtractDecimal(response.Json, "credit", "data") ?? 0m);
    }

    public async Task<IReadOnlyList<SmsLine>> GetLinesAsync(CancellationToken ct = default)
    {
        var response = await SendWithRetryAsync(HttpMethod.Get, "line", null, ct);
        if (!IsSuccess(response.StatusCode)) throw new InvalidOperationException(BuildError(response));
        var lines = new List<SmsLine>();
        foreach (var item in FindArray(response.Json))
        {
            var number = ExtractString(item, "lineNumber", "number", "line") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(number)) lines.Add(new SmsLine(number, ExtractString(item, "type"), ExtractBool(item, "isActive")));
        }
        return lines;
    }

    private async Task<ProviderResponse> SendWithRetryAsync(HttpMethod method, string endpoint, object? body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) throw new InvalidOperationException("SMS.ir API Key is not configured. Set SMSApiKey or Sms:SmsIr:ApiKey.");
        if (string.IsNullOrWhiteSpace(_lineNumber) && (endpoint == "send/bulk" || endpoint == "send/likeToLike")) throw new InvalidOperationException("SMS.ir line number is not configured. Set SMSLineNumber or Sms:SmsIr:LineNumber.");

        Exception? last = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(method, endpoint);
                request.Headers.TryAddWithoutValidation("X-API-KEY", _apiKey);
                if (body is not null) request.Content = JsonContent.Create(body);
                using var response = await _httpClient.SendAsync(request, ct);
                var raw = await response.Content.ReadAsStringAsync(ct);
                var json = ParseJson(raw);
                _logger.LogDebug("SMS.ir response {StatusCode} for {Endpoint}: {ResponseBody}", (int)response.StatusCode, endpoint, Truncate(raw, 1000));
                if (!ShouldRetry(response.StatusCode)) return new ProviderResponse(response.StatusCode, json, raw);
                last = new HttpRequestException($"SMS.ir returned HTTP {(int)response.StatusCode}: {Truncate(raw, 500)}");
            }
            catch (Exception ex) when ((ex is HttpRequestException || ex is TaskCanceledException) && !ct.IsCancellationRequested)
            {
                last = ex;
                _logger.LogWarning(ex, "SMS.ir request attempt {Attempt} failed for {Endpoint}.", attempt + 1, endpoint);
            }
            if (attempt < 2) await Task.Delay(TimeSpan.FromSeconds(attempt == 0 ? 2 : 5), ct);
        }
        throw new InvalidOperationException("SMS.ir request failed after retries.", last);
    }

    private SmsSendResult ToSendResult(IReadOnlyCollection<string> mobiles, ProviderResponse response)
    {
        var accepted = IsSuccess(response.StatusCode);
        var messageId = ExtractString(response.Json, "messageId", "id");
        var packId = ExtractString(response.Json, "packId");
        var errorCode = accepted ? null : ExtractString(response.Json, "errorCode", "code");
        var errorMessage = accepted ? null : BuildError(response);
        return new SmsSendResult(mobiles.Select(m => new SmsSendItemResult(m, accepted, messageId, packId, null, errorCode, errorMessage)).ToArray());
    }

    private int ParseLineNumber()
    {
        var normalized = NormalizeDigits(_lineNumber).Trim();
        if (int.TryParse(normalized, out var value)) return value;
        throw new InvalidOperationException("SMS.ir LineNumber must be numeric. Use the numeric sender line from your SMS.ir account (for example 3000xxxxxx), without +, spaces, or hyphens.");
    }

    private static string NormalizeDigits(string value)
    {
        return value
            .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
            .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
            .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
            .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');
    }

    private static int ParsePatternId(string value) => int.TryParse(NormalizeDigits(value).Trim(), out var id) ? id : throw new ArgumentException("SMS.ir PatternId must be numeric.");
    private static bool IsSuccess(HttpStatusCode code) => (int)code is >= 200 and < 300;
    private static bool ShouldRetry(HttpStatusCode code) => code == HttpStatusCode.TooManyRequests || (int)code >= 500;
    private static string BuildError(ProviderResponse response) => ExtractString(response.Json, "message", "errorMessage", "error") ?? (!string.IsNullOrWhiteSpace(response.RawBody) ? Truncate(response.RawBody, 500) : $"SMS.ir request failed with HTTP {(int)response.StatusCode}.");
    private static SmsDeliveryStatus MapStatus(int? code) => code switch { 1 => SmsDeliveryStatus.Delivered, 2 or 4 or 6 => SmsDeliveryStatus.Failed, 3 => SmsDeliveryStatus.Processing, 5 => SmsDeliveryStatus.SentToOperator, 7 => SmsDeliveryStatus.Blacklisted, _ => SmsDeliveryStatus.Queued };
    private static JsonElement ParseJson(string raw) { if (string.IsNullOrWhiteSpace(raw)) return default; try { using var document = JsonDocument.Parse(raw); return document.RootElement.Clone(); } catch { return default; } }
    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
    private static string? ExtractString(JsonElement element, params string[] names) { foreach (var name in names) if (TryFind(element, name, out var value) && value.ValueKind == JsonValueKind.String) return value.GetString(); return null; }
    private static int? ExtractInt(JsonElement element, params string[] names) { foreach (var name in names) if (TryFind(element, name, out var value) && value.TryGetInt32(out var result)) return result; return null; }
    private static decimal? ExtractDecimal(JsonElement element, params string[] names) { foreach (var name in names) if (TryFind(element, name, out var value) && value.TryGetDecimal(out var result)) return result; return null; }
    private static bool? ExtractBool(JsonElement element, params string[] names) { foreach (var name in names) if (TryFind(element, name, out var value) && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)) return value.GetBoolean(); return null; }
    private static bool TryFind(JsonElement element, string name, out JsonElement value) { if (element.ValueKind == JsonValueKind.Object) { foreach (var p in element.EnumerateObject()) if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) { value = p.Value; return true; } foreach (var p in element.EnumerateObject()) if (TryFind(p.Value, name, out value)) return true; } else if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) if (TryFind(item, name, out value)) return true; value = default; return false; }
    private static IEnumerable<JsonElement> FindArray(JsonElement element) { if (element.ValueKind == JsonValueKind.Array) { foreach (var x in element.EnumerateArray()) yield return x; yield break; } if (element.ValueKind == JsonValueKind.Object) foreach (var p in element.EnumerateObject()) if (p.Value.ValueKind == JsonValueKind.Array) foreach (var x in p.Value.EnumerateArray()) yield return x; }
    private sealed record ProviderResponse(HttpStatusCode StatusCode, JsonElement Json, string RawBody);
}
