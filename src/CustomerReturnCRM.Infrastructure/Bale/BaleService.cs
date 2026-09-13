using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CustomerReturnCRM.Application.Bale;
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

    public BaleService(ApplicationDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<BaleConnectInviteResult?> CreateConnectInviteAsync(
        Guid businessId,
        Guid customerId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var isMember = await _db.BusinessMembers.AnyAsync(
            x => x.BusinessId == businessId && x.UserId == userId,
            cancellationToken);
        if (!isMember) return null;

        var customerExists = await _db.Customers.AnyAsync(
            x => x.Id == customerId && x.BusinessId == businessId && x.IsActive,
            cancellationToken);
        if (!customerExists) return null;

        var existing = await _db.BaleCustomerIdentities
            .FirstOrDefaultAsync(x => x.BusinessId == businessId && x.CustomerId == customerId && x.IsActive, cancellationToken);
        if (existing is not null)
        {
            var botUsername = _configuration["Bale:BotUsername"];
            if (string.IsNullOrWhiteSpace(botUsername))
                throw new InvalidOperationException("Bale:BotUsername is not configured.");

            var token = CreateToken();
            var now = DateTime.UtcNow;
            _db.BaleConnectTokens.Add(new BaleConnectToken
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                CustomerId = customerId,
                TokenHash = HashToken(token),
                ExpiresAtUtc = now.AddMinutes(15),
                CreatedAt = now
            });
            await _db.SaveChangesAsync(cancellationToken);
            return new BaleConnectInviteResult(customerId, BuildConnectUrl(botUsername, token), now.AddMinutes(15));
        }

        var rawToken = CreateToken();
        var createdAt = DateTime.UtcNow;
        _db.BaleConnectTokens.Add(new BaleConnectToken
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CustomerId = customerId,
            TokenHash = HashToken(rawToken),
            ExpiresAtUtc = createdAt.AddMinutes(15),
            CreatedAt = createdAt
        });
        await _db.SaveChangesAsync(cancellationToken);

        var username = _configuration["Bale:BotUsername"];
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("Bale:BotUsername is not configured.");

        return new BaleConnectInviteResult(customerId, BuildConnectUrl(username, rawToken), createdAt.AddMinutes(15));
    }

    public async Task<bool> HandleUpdateAsync(string updateJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updateJson)) return false;

        using var document = JsonDocument.Parse(updateJson);
        var root = document.RootElement;
        if (!root.TryGetProperty("message", out var message)) return false;
        if (!message.TryGetProperty("chat", out var chat) || !chat.TryGetProperty("id", out var chatIdElement)) return false;
        if (!chatIdElement.TryGetInt64(out var chatId)) return false;
        if (!message.TryGetProperty("from", out var from) || !from.TryGetProperty("id", out var userIdElement)) return false;
        if (!userIdElement.TryGetInt64(out var baleUserId)) return false;
        if (!message.TryGetProperty("text", out var textElement)) return false;

        var text = textElement.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return false;

        const string startPrefix = "/start";
        if (!text.StartsWith(startPrefix, StringComparison.OrdinalIgnoreCase)) return false;
        var token = text[startPrefix.Length..].Trim();
        if (token.Length == 0) return false;

        var tokenHash = HashToken(token);
        var connectToken = await _db.BaleConnectTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && !x.IsRevoked && x.UsedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow, cancellationToken);
        if (connectToken is null) return false;

        var username = from.TryGetProperty("username", out var usernameElement) ? usernameElement.GetString() : null;
        var existing = await _db.BaleCustomerIdentities
            .FirstOrDefaultAsync(x => x.BusinessId == connectToken.BusinessId && x.CustomerId == connectToken.CustomerId, cancellationToken);

        if (existing is null)
        {
            _db.BaleCustomerIdentities.Add(new BaleCustomerIdentity
            {
                Id = Guid.NewGuid(),
                BusinessId = connectToken.BusinessId,
                CustomerId = connectToken.CustomerId,
                BaleUserId = baleUserId,
                BaleChatId = chatId,
                Username = username,
                ConnectedAtUtc = DateTime.UtcNow,
                LastSeenAtUtc = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.BaleUserId = baleUserId;
            existing.BaleChatId = chatId;
            existing.Username = username;
            existing.LastSeenAtUtc = DateTime.UtcNow;
            existing.IsActive = true;
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
        using var response = await client.PostAsJsonAsync(
            $"bot{token}/sendMessage",
            new { chat_id = request.ChatId, text = request.Text },
            cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private static string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .Replace("+", "-").Replace("/", "_").TrimEnd('=');

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }

    private static string BuildConnectUrl(string botUsername, string token) =>
        $"https://ble.ir/{Uri.EscapeDataString(botUsername)}?start={Uri.EscapeDataString(token)}";
}
