using System.Security.Cryptography;
using CustomerReturnCRM.Application.Availability;
using CustomerReturnCRM.Application.PublicBooking;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CustomerReturnCRM.Infrastructure.PublicBooking;

public sealed class PublicBookingService : IPublicBookingService
{
    private const string CodeAlphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    private readonly ApplicationDbContext _db;
    private readonly IAvailabilityManagementService _availability;
    private readonly IConfiguration _configuration;

    public PublicBookingService(ApplicationDbContext db, IAvailabilityManagementService availability, IConfiguration configuration)
    {
        _db = db;
        _availability = availability;
        _configuration = configuration;
    }

    public async Task<PublicBusinessProfileResult?> GetBusinessAsync(string slug, CancellationToken cancellationToken = default)
    {
        var business = await _db.Businesses.AsNoTracking()
            .Include(x => x.Staff)
            .SingleOrDefaultAsync(x => x.PublicSlug == slug.Trim() && x.IsActive && x.PublicBookingEnabled, cancellationToken);

        if (business is null) return null;

        var services = await _db.Services.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.IsActive)
            .OrderBy(x => x.Title)
            .Select(x => new PublicServiceResult(x.Id, x.Title, x.Description, x.DefaultPrice, x.DefaultDurationMinutes))
            .ToListAsync(cancellationToken);

        var staff = business.Staff.Where(x => x.IsActive)
            .OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
            .Select(x => new PublicStaffResult(x.Id, x.FirstName, x.LastName))
            .ToList();

        return new PublicBusinessProfileResult(business.Id, business.Name, business.BusinessType, business.Mobile,
            business.Address, business.City, business.Description, business.PublicSlug, services, staff);
    }

    public async Task<PublicBookingResult> BookAsync(string slug, PublicBookingRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var business = await _db.Businesses.SingleOrDefaultAsync(
            x => x.PublicSlug == slug.Trim() && x.IsActive && x.PublicBookingEnabled, cancellationToken);
        if (business is null) throw new KeyNotFoundException("Public booking is not available for this business.");

        var service = await _db.Services.SingleOrDefaultAsync(
            x => x.Id == request.ServiceId && x.BusinessId == business.Id && x.IsActive, cancellationToken);
        if (service is null) throw new ArgumentException("The selected service is not available.");

        var availability = await _availability.GetAvailableSlotsAsync(business.Id, new AvailabilityRequest
        {
            ServiceId = service.Id,
            StaffId = request.StaffId,
            Date = DateOnly.FromDateTime(request.StartAt),
            SlotIntervalMinutes = 5
        }, cancellationToken);

        var candidate = availability.FirstOrDefault(x => x.StartAt == request.StartAt);
        if (candidate is null) throw new InvalidOperationException("The selected time is no longer available.");

        var staff = await _db.Staff.SingleOrDefaultAsync(
            x => x.Id == candidate.StaffId && x.BusinessId == business.Id && x.IsActive, cancellationToken);
        if (staff is null) throw new InvalidOperationException("The selected staff member is no longer available.");

        var endAt = request.StartAt.AddMinutes(service.DefaultDurationMinutes);
        if (endAt <= request.StartAt) throw new ArgumentException("Service duration is invalid.");

        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        var overlap = await _db.Appointments.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.StartAt < endAt && x.EndAt > request.StartAt &&
                        x.Status != AppointmentStatus.Cancelled && x.Status != AppointmentStatus.NoShow &&
                        x.AppointmentServices.Any(s => s.StaffId == staff.Id))
            .AnyAsync(cancellationToken);
        if (overlap) throw new InvalidOperationException("The selected time was just booked by another customer.");

        var mobile = request.Mobile.Trim();
        var customer = await _db.Customers.SingleOrDefaultAsync(
            x => x.BusinessId == business.Id && x.Mobile == mobile, cancellationToken);

        if (customer is null)
        {
            customer = new Customer
            {
                Id = Guid.NewGuid(), BusinessId = business.Id,
                FirstName = request.FirstName.Trim(), LastName = Normalize(request.LastName),
                Mobile = mobile, IsActive = true, CreatedAt = DateTime.UtcNow
            };
            _db.Customers.Add(customer);
        }
        else if (!customer.IsActive)
        {
            throw new InvalidOperationException("This customer record is inactive. Please contact the business.");
        }
        // An existing CRM customer is intentionally not overwritten by public booking input.
        // The mobile number identifies the CRM record; the customer remains the source of truth for identity data.

        var publicCode = await CreateUniqueCodeAsync(cancellationToken);
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(), BusinessId = business.Id, CustomerId = customer.Id,
            StartAt = request.StartAt, EndAt = endAt, Status = AppointmentStatus.Confirmed,
            Note = Normalize(request.Note), PublicCode = publicCode, CreatedAt = DateTime.UtcNow
        };
        appointment.AppointmentServices.Add(new AppointmentService
        {
            Id = Guid.NewGuid(), AppointmentId = appointment.Id, ServiceId = service.Id,
            StaffId = staff.Id, ServiceTitle = service.Title, Price = service.DefaultPrice,
            DurationMinutes = service.DefaultDurationMinutes
        });
        _db.Appointments.Add(appointment);

        var publicUrl = BuildPublicAppointmentUrl(publicCode);
        var message = $"نوبت شما در {business.Name} ثبت شد. {service.Title} - {FormatDateTime(request.StartAt)}. مشاهده نوبت: {publicUrl}";
        var campaign = new SmsCampaign
        {
            Id = Guid.NewGuid(), BusinessId = business.Id, CreatedByUserId = Guid.Empty,
            Name = "Online booking confirmation", Message = message, ScheduledAt = DateTime.UtcNow,
            Status = SmsCampaignStatus.Scheduled, CreatedAt = DateTime.UtcNow
        };
        campaign.Recipients.Add(new SmsRecipient
        {
            Id = Guid.NewGuid(), SmsCampaignId = campaign.Id, CustomerId = customer.Id,
            Mobile = mobile, RenderedMessage = message, Status = SmsRecipientStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        _db.SmsCampaigns.Add(campaign);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PublicBookingResult(appointment.Id, publicCode, appointment.StartAt, appointment.EndAt,
            appointment.Status.ToString(), business.Name, service.Title,
            $"{staff.FirstName} {staff.LastName}".Trim(), publicUrl);
    }

    public async Task<PublicAppointmentResult?> GetAppointmentAsync(string publicCode, CancellationToken cancellationToken = default)
    {
        var appointment = await _db.Appointments.AsNoTracking()
            .Include(x => x.Business)
            .Include(x => x.Customer)
            .Include(x => x.AppointmentServices)
            .ThenInclude(x => x.Staff)
            .SingleOrDefaultAsync(x => x.PublicCode == publicCode.Trim(), cancellationToken);

        if (appointment is null) return null;
        var service = appointment.AppointmentServices.FirstOrDefault();
        if (service is null) return null;
        return new PublicAppointmentResult(appointment.PublicCode, appointment.Business.Name, appointment.Business.Address,
            appointment.Business.City, FullName(appointment.Customer.FirstName, appointment.Customer.LastName),
            service.ServiceTitle, FullName(service.Staff.FirstName, service.Staff.LastName), appointment.StartAt,
            appointment.EndAt, appointment.Status.ToString());
    }

    private async Task<string> CreateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            Span<byte> bytes = stackalloc byte[9];
            RandomNumberGenerator.Fill(bytes);
            var chars = new char[12];
            for (var i = 0; i < chars.Length; i++) chars[i] = CodeAlphabet[bytes[i % bytes.Length] % CodeAlphabet.Length];
            var code = new string(chars);
            if (!await _db.Appointments.AnyAsync(x => x.PublicCode == code, cancellationToken)) return code;
        }
        throw new InvalidOperationException("Could not generate a unique public appointment code.");
    }

    private string BuildPublicAppointmentUrl(string code)
    {
        var baseUrl = _configuration["PublicApp:BaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) throw new InvalidOperationException("Configuration 'PublicApp:BaseUrl' is required.");
        return $"{baseUrl}/a/{code}";
    }

    private static void ValidateRequest(PublicBookingRequest request)
    {
        if (request.ServiceId == Guid.Empty) throw new ArgumentException("Service is required.");
        if (request.StartAt == default) throw new ArgumentException("Start time is required.");
        if (string.IsNullOrWhiteSpace(request.FirstName)) throw new ArgumentException("First name is required.");
        if (string.IsNullOrWhiteSpace(request.Mobile)) throw new ArgumentException("Mobile is required.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string FullName(string firstName, string? lastName) => string.IsNullOrWhiteSpace(lastName) ? firstName : $"{firstName} {lastName}";
    private static string FormatDateTime(DateTime value) => value.ToString("yyyy/MM/dd HH:mm");
}
