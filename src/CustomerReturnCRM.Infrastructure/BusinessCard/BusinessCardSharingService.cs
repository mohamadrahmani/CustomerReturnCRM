using CustomerReturnCRM.Application.Bale;
using CustomerReturnCRM.Application.BusinessCard;
using CustomerReturnCRM.Application.Sms;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CustomerReturnCRM.Infrastructure.BusinessCard;

public sealed class BusinessCardSharingService : IBusinessCardSharingService
{
    private readonly ApplicationDbContext _db;
    private readonly ISmsService _smsService;
    private readonly IBaleService _baleService;
    private readonly IConfiguration _configuration;

    public BusinessCardSharingService(
        ApplicationDbContext db,
        ISmsService smsService,
        IBaleService baleService,
        IConfiguration configuration)
    {
        _db = db;
        _smsService = smsService;
        _baleService = baleService;
        _configuration = configuration;
    }

    public async Task<BusinessCardShareResult?> ShareAsync(
        Guid businessId,
        Guid customerId,
        Guid userId,
        BusinessCardShareChannel channel,
        CancellationToken cancellationToken = default)
    {
        var isMember = await _db.BusinessMembers.AnyAsync(
            x => x.BusinessId == businessId && x.UserId == userId,
            cancellationToken);
        if (!isMember) return null;

        var business = await _db.Businesses.AsNoTracking()
            .Where(x => x.Id == businessId && x.IsActive && x.PublicBookingEnabled && x.PublicSlug != null)
            .Select(x => new { x.Id, x.Name, x.PublicSlug })
            .SingleOrDefaultAsync(cancellationToken);
        if (business is null) return null;

        var customer = await _db.Customers.AsNoTracking()
            .Where(x => x.Id == customerId && x.BusinessId == businessId && x.IsActive)
            .Select(x => new { x.Id, x.FirstName, x.Mobile })
            .SingleOrDefaultAsync(cancellationToken);
        if (customer is null) return null;

        var baseUrl = _configuration["PublicApp:BaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Configuration 'PublicApp:BaseUrl' is required.");

        var publicUrl = $"{baseUrl}/b/{Uri.EscapeDataString(business.PublicSlug!)}";
        var firstName = string.IsNullOrWhiteSpace(customer.FirstName) ? "دوست عزیز" : $"{customer.FirstName} عزیز";
        var message = $"{firstName}\nکارت ویزیت و صفحه رزرو {business.Name}:\n{publicUrl}";

        if (channel == BusinessCardShareChannel.Sms)
        {
            if (string.IsNullOrWhiteSpace(customer.Mobile))
                return new BusinessCardShareResult(publicUrl, channel, false, "شماره موبایل مشتری ثبت نشده است.");

            try
            {
                var result = await _smsService.SendAsync(
                    new SmsSendRequest(new[] { customer.Mobile }, message, SmsMessageType.Marketing),
                    cancellationToken);
                var item = result.Items.FirstOrDefault();
                return new BusinessCardShareResult(publicUrl, channel, item?.Accepted == true, item?.ErrorMessage);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return new BusinessCardShareResult(publicUrl, channel, false, exception.Message);
            }
        }

        var identity = await _db.Set<Domain.Entities.BaleCustomerIdentity>().AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.CustomerId == customerId && x.IsActive)
            .Select(x => new { x.BaleChatId })
            .SingleOrDefaultAsync(cancellationToken);
        if (identity is null)
            return new BusinessCardShareResult(publicUrl, channel, false, "این مشتری هنوز به بله متصل نشده است.");

        var sent = await _baleService.SendMessageAsync(
            new BaleSendMessageRequest(identity.BaleChatId, message),
            cancellationToken);
        return new BusinessCardShareResult(
            publicUrl,
            channel,
            sent,
            sent ? null : "ارسال پیام در بله ناموفق بود.");
    }
}
