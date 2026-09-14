using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CustomerReturnCRM.Application.Bale;
using CustomerReturnCRM.Application.Sms;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CustomerReturnCRM.Infrastructure.Bale;

public sealed class BaleService : IBaleService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ISmsService _smsService;

    public BaleService(ApplicationDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration, ISmsService smsService)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _smsService = smsService;
    }

    public async Task<BaleConnectInviteResult?> CreateConnectInviteAsync(Guid businessId, Guid customerId, Guid userId, CancellationToken cancellationToken = default)
    {
        var isMember = await _db.BusinessMembers.AnyAsync(x => x.BusinessId == businessId && x.UserId == userId, cancellationToken);
        if (!isMember) return null;

        var customerExists = await _db.Customers.AnyAsync(x => x.Id == customerId && x.BusinessId == businessId && x.IsActive, cancellationToken);
        if (!customerExists) return null;

        var botUsername = _configuration["Bale:BotUsername"];
        if (string.IsNullOrWhiteSpace(botUsername)) throw new InvalidOperationException("Bale:BotUsername is not configured.");

        var rawToken = CreateToken();
        var createdAt = DateTime.UtcNow;
        _db.Set<BaleConnectToken>().Add(new BaleConnectToken
        {
            Id = Guid.NewGuid(), BusinessId = businessId, CustomerId = customerId,
            TokenHash = HashToken(rawToken), ExpiresAtUtc = createdAt.AddMinutes(15), CreatedAt = createdAt
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new BaleConnectInviteResult(customerId, BuildConnectUrl(botUsername, rawToken), createdAt.AddMinutes(15));
    }

    public async Task<BaleConnectSmsResult?> CreateConnectInviteAndSendSmsAsync(Guid businessId, Guid customerId, Guid userId, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .Where(x => x.Id == customerId && x.BusinessId == businessId && x.IsActive)
            .Select(x => new { x.Id, x.FirstName, x.Mobile })
            .FirstOrDefaultAsync(cancellationToken);

        if (customer is null || string.IsNullOrWhiteSpace(customer.Mobile)) return null;

        var invite = await CreateConnectInviteAsync(businessId, customerId, userId, cancellationToken);
        if (invite is null) return null;

        var message = $"{customer.FirstName} عزیز\nبرای دریافت یادآوری‌های نوبت و زمان مناسب مراجعه در بله، روی لینک زیر بزنید و Start را انتخاب کنید:\n{invite.ConnectUrl}";
        try
        {
            var result = await _smsService.SendAsync(new SmsSendRequest(
                new[] { customer.Mobile },
                message,
                SmsMessageType.Reminder), cancellationToken);
            var item = result.Items.FirstOrDefault();
            return new BaleConnectSmsResult(invite, item?.Accepted == true, item?.ErrorMessage);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new BaleConnectSmsResult(invite, false, exception.Message);
        }
    }

    public async Task<bool> HandleUpdateAsync(string updateJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updateJson)) return false;

        using var document = JsonDocument.Parse(updateJson);
        var root = document.RootElement;
        if (!root.TryGetProperty("message", out var message)) return false;
        if (!message.TryGetProperty("chat", out var chat) || !chat.TryGetProperty("id", out var chatIdElement) || !chatIdElement.TryGetInt64(out var chatId)) return false;
        if (!message.TryGetProperty("from", out var from) || !from.TryGetProperty("id", out var userIdElement) || !userIdElement.TryGetInt64(out var baleUserId)) return false;
        if (!message.TryGetProperty("text", out var textElement)) return false;

        var text = textElement.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith("/start", StringComparison.OrdinalIgnoreCase)) return false;
        var token = text[6..].Trim();
        if (token.Length == 0) return false;

        var tokenHash = HashToken(token);
        var connectToken = await _db.Set<BaleConnectToken>().FirstOrDefaultAsync(
            x => x.TokenHash == tokenHash && !x.IsRevoked && x.UsedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow,
            cancellationToken);
        if (connectToken is null) return false;

        var username = from.TryGetProperty("username", out var usernameElement) ? usernameElement.GetString() : null;
        var existing = await _db.Set<BaleCustomerIdentity>().FirstOrDefaultAsync(
            x => x.BusinessId == connectToken.BusinessId && x.CustomerId == connectToken.CustomerId, cancellationToken);

        if (existing is null)
        {
            _db.Set<BaleCustomerIdentity>().Add(new BaleCustomerIdentity
            {
                Id = Guid.NewGuid(), BusinessId = connectToken.BusinessId, CustomerId = connectToken.CustomerId,
                BaleUserId = baleUserId, BaleChatId = chatId, Username = username,
                ConnectedAtUtc = DateTime.UtcNow, LastSeenAtUtc = DateTime.UtcNow, IsActive = true, CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.BaleUserId = baleUserId; existing.BaleChatId = chatId; existing.Username = username;
            existing.LastSeenAtUtc = DateTime.UtcNow; existing.IsActive = true;
        }

        connectToken.UsedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await SendMessageAsync(new BaleSendMessageRequest(chatId, "اتصال شما با موفقیت انجام شد. از این پس پیام‌های مربوط به یادآوری‌ها را در بله دریافت می‌کنید."), cancellationToken);
        return true;
    }

    public async Task<bool> SendMessageAsync(BaleSendMessageRequest request, CancellationToken cancellationToken = default)
    {
        var token = _configuration["Bale:BotToken"];
        if (string.IsNullOrWhiteSpace(token)) return false;
        var client = _httpClientFactory.CreateClient("Bale");
        using var response = await client.PostAsJsonAsync($"bot{token}/sendMessage", new { chat_id = request.ChatId, text = request.Text }, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private static string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string BuildConnectUrl(string botUsername, string token) => $"https://ble.ir/{Uri.EscapeDataString(botUsername)}?start={Uri.EscapeDataString(token)}";
}
