using CustomerReturnCRM.Application.Availability;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.Availability;

public sealed class AvailabilityManagementService : IAvailabilityManagementService
{
    private readonly ApplicationDbContext _db;
    public AvailabilityManagementService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<WorkingHourResult>> GetBusinessWorkingHoursAsync(Guid businessId, Guid userId, CancellationToken ct = default)
    {
        await EnsureMemberAsync(businessId, userId, ct);
        return await _db.Set<BusinessWorkingHour>().AsNoTracking().Where(x => x.BusinessId == businessId).OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime).Select(ToResult).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WorkingHourResult>> ReplaceBusinessWorkingHoursAsync(Guid businessId, Guid userId, IReadOnlyCollection<WorkingHourRequest> hours, CancellationToken ct = default)
    {
        await EnsureMemberAsync(businessId, userId, ct); ValidateHours(hours);
        var existing = await _db.Set<BusinessWorkingHour>().Where(x => x.BusinessId == businessId).ToListAsync(ct);
        _db.Set<BusinessWorkingHour>().RemoveRange(existing);
        _db.Set<BusinessWorkingHour>().AddRange(hours.Select(x => new BusinessWorkingHour { Id = Guid.NewGuid(), BusinessId = businessId, DayOfWeek = x.DayOfWeek, StartTime = x.StartTime, EndTime = x.EndTime }));
        await _db.SaveChangesAsync(ct);
        return await GetBusinessWorkingHoursAsync(businessId, userId, ct);
    }

    public async Task<IReadOnlyList<WorkingHourResult>> GetStaffWorkingHoursAsync(Guid businessId, Guid staffId, Guid userId, CancellationToken ct = default)
    {
        await EnsureStaffAsync(businessId, staffId, userId, ct);
        return await _db.Set<StaffWorkingHour>().AsNoTracking().Where(x => x.StaffId == staffId).OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime).Select(ToResult).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WorkingHourResult>> ReplaceStaffWorkingHoursAsync(Guid businessId, Guid staffId, Guid userId, IReadOnlyCollection<WorkingHourRequest> hours, CancellationToken ct = default)
    {
        await EnsureStaffAsync(businessId, staffId, userId, ct); ValidateHours(hours);
        var existing = await _db.Set<StaffWorkingHour>().Where(x => x.StaffId == staffId).ToListAsync(ct);
        _db.Set<StaffWorkingHour>().RemoveRange(existing);
        _db.Set<StaffWorkingHour>().AddRange(hours.Select(x => new StaffWorkingHour { Id = Guid.NewGuid(), StaffId = staffId, DayOfWeek = x.DayOfWeek, StartTime = x.StartTime, EndTime = x.EndTime }));
        await _db.SaveChangesAsync(ct);
        return await GetStaffWorkingHoursAsync(businessId, staffId, userId, ct);
    }

    public async Task<StaffTimeOffResult> AddTimeOffAsync(Guid businessId, Guid staffId, Guid userId, DateTime startAt, DateTime endAt, string? reason, CancellationToken ct = default)
    {
        await EnsureStaffAsync(businessId, staffId, userId, ct);
        if (endAt <= startAt) throw new ArgumentException("Time off end must be after its start.");
        var item = new StaffTimeOff { Id = Guid.NewGuid(), StaffId = staffId, StartAt = startAt, EndAt = endAt, Reason = Normalize(reason) };
        _db.Set<StaffTimeOff>().Add(item); await _db.SaveChangesAsync(ct);
        return new(item.Id, item.StaffId, item.StartAt, item.EndAt, item.Reason);
    }

    public async Task<IReadOnlyList<StaffTimeOffResult>> ListTimeOffAsync(Guid businessId, Guid staffId, Guid userId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureStaffAsync(businessId, staffId, userId, ct);
        if (to <= from) throw new ArgumentException("The time-off range is invalid.");
        return await _db.Set<StaffTimeOff>().AsNoTracking().Where(x => x.StaffId == staffId && x.EndAt > from && x.StartAt < to).OrderBy(x => x.StartAt).Select(x => new StaffTimeOffResult(x.Id, x.StaffId, x.StartAt, x.EndAt, x.Reason)).ToListAsync(ct);
    }

    public async Task<bool> DeleteTimeOffAsync(Guid businessId, Guid staffId, Guid timeOffId, Guid userId, CancellationToken ct = default)
    {
        await EnsureStaffAsync(businessId, staffId, userId, ct);
        var item = await _db.Set<StaffTimeOff>().SingleOrDefaultAsync(x => x.Id == timeOffId && x.StaffId == staffId, ct);
        if (item is null) return false; _db.Set<StaffTimeOff>().Remove(item); await _db.SaveChangesAsync(ct); return true;
    }

    public async Task<IReadOnlyList<AvailabilitySlotResult>> GetAvailableSlotsAsync(Guid businessId, AvailabilityRequest request, CancellationToken ct = default)
    {
        if (request.ServiceId == Guid.Empty) throw new ArgumentException("Service is required.");
        if (request.Date == default) throw new ArgumentException("Date is required.");
        if (request.SlotIntervalMinutes < 5 || request.SlotIntervalMinutes > 240) throw new ArgumentException("Slot interval must be between 5 and 240 minutes.");
        if (!await _db.Businesses.AsNoTracking().AnyAsync(x => x.Id == businessId && x.IsActive, ct)) throw new ArgumentException("Business was not found or is inactive.");
        var service = await _db.Services.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.ServiceId && x.BusinessId == businessId && x.IsActive, ct) ?? throw new ArgumentException("Service was not found or is inactive.");

        var staffQuery = _db.Staff.AsNoTracking().Where(x => x.BusinessId == businessId && x.IsActive);
        if (request.StaffId.HasValue) staffQuery = staffQuery.Where(x => x.Id == request.StaffId.Value);
        var staff = await staffQuery.Select(x => new { x.Id }).ToListAsync(ct);
        if (request.StaffId.HasValue && staff.Count == 0) throw new ArgumentException("Staff member was not found or is inactive.");

        var day = request.Date.DayOfWeek;
        var businessHours = await _db.Set<BusinessWorkingHour>().AsNoTracking().Where(x => x.BusinessId == businessId && x.DayOfWeek == day).OrderBy(x => x.StartTime).ToListAsync(ct);
        if (businessHours.Count == 0) return Array.Empty<AvailabilitySlotResult>();

        var dayStart = request.Date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);
        var staffIds = staff.Select(x => x.Id).ToList();
        var appointments = await _db.Appointments.AsNoTracking().Where(x => x.BusinessId == businessId && x.StartAt < dayEnd && x.EndAt > dayStart && x.Status != AppointmentStatus.Cancelled && x.Status != AppointmentStatus.NoShow).Select(x => new { x.StartAt, x.EndAt, StaffIds = x.AppointmentServices.Select(s => s.StaffId) }).ToListAsync(ct);
        var timeOffs = await _db.Set<StaffTimeOff>().AsNoTracking().Where(x => staffIds.Contains(x.StaffId) && x.StartAt < dayEnd && x.EndAt > dayStart).ToListAsync(ct);
        var staffHours = await _db.Set<StaffWorkingHour>().AsNoTracking().Where(x => staffIds.Contains(x.StaffId) && x.DayOfWeek == day).OrderBy(x => x.StartTime).ToListAsync(ct);

        var result = new List<AvailabilitySlotResult>();
        foreach (var staffMember in staff)
        {
            var configured = staffHours.Where(x => x.StaffId == staffMember.Id).ToList();
            var effective = configured.Count == 0 ? businessHours.Select(x => (x.StartTime, x.EndTime)).ToList() : configured.Select(x => (x.StartTime, x.EndTime)).ToList();
            foreach (var businessHour in businessHours)
            foreach (var staffHour in effective)
            {
                var rangeStart = dayStart.Add(Max(businessHour.StartTime, staffHour.StartTime));
                var rangeEnd = dayStart.Add(Min(businessHour.EndTime, staffHour.EndTime));
                for (var start = rangeStart; start.AddMinutes(service.DefaultDurationMinutes) <= rangeEnd; start = start.AddMinutes(request.SlotIntervalMinutes))
                {
                    var end = start.AddMinutes(service.DefaultDurationMinutes);
                    if (appointments.Any(a => a.StaffIds.Contains(staffMember.Id) && a.StartAt < end && a.EndAt > start)) continue;
                    if (timeOffs.Any(t => t.StaffId == staffMember.Id && t.StartAt < end && t.EndAt > start)) continue;
                    result.Add(new AvailabilitySlotResult(staffMember.Id, start, end));
                }
            }
        }
        return result.OrderBy(x => x.StartAt).ThenBy(x => x.StaffId).ToList();
    }

    private async Task EnsureMemberAsync(Guid businessId, Guid userId, CancellationToken ct)
    { if (!await _db.BusinessMembers.AnyAsync(x => x.BusinessId == businessId && x.UserId == userId, ct)) throw new UnauthorizedAccessException("The user is not a member of this business."); }

    private async Task EnsureStaffAsync(Guid businessId, Guid staffId, Guid userId, CancellationToken ct)
    { await EnsureMemberAsync(businessId, userId, ct); if (!await _db.Staff.AnyAsync(x => x.Id == staffId && x.BusinessId == businessId && x.IsActive, ct)) throw new ArgumentException("Staff member was not found or is inactive."); }

    private static void ValidateHours(IReadOnlyCollection<WorkingHourRequest> hours)
    {
        foreach (var group in hours.GroupBy(x => x.DayOfWeek))
        {
            var ordered = group.OrderBy(x => x.StartTime).ToList();
            foreach (var hour in ordered)
                if (hour.StartTime < TimeSpan.Zero || hour.EndTime > TimeSpan.FromDays(1) || hour.EndTime <= hour.StartTime) throw new ArgumentException("Working-hour intervals must be within the day and have an end after their start.");
            for (var i = 1; i < ordered.Count; i++) if (ordered[i].StartTime < ordered[i - 1].EndTime) throw new ArgumentException("Working-hour intervals cannot overlap.");
        }
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static WorkingHourResult ToResult(BusinessWorkingHour x) => new(x.Id, x.DayOfWeek, x.StartTime, x.EndTime);
    private static WorkingHourResult ToResult(StaffWorkingHour x) => new(x.Id, x.DayOfWeek, x.StartTime, x.EndTime);
}
