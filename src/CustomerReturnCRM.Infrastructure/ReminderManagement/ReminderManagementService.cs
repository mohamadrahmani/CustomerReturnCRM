using CustomerReturnCRM.Application.ReminderManagement;
using CustomerReturnCRM.Application.Common;
using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerReturnCRM.Infrastructure.ReminderManagement;

public sealed class ReminderManagementService : IReminderManagementService
{
    private readonly ApplicationDbContext _dbContext;

    public ReminderManagementService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<ReminderResult>> ListAsync(
        Guid businessId,
        Guid userId,
        ReminderStatus? status,
        DateTime? from,
        DateTime? to,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var query = _dbContext.Reminders.AsNoTracking().Where(x => x.BusinessId == businessId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (from.HasValue) query = query.Where(x => x.DueAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.DueAt < to.Value);

        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Status)
            .ThenBy(x => x.DueAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ToResult(x))
            .ToListAsync(cancellationToken);
        return Pagination.Create(items, page, pageSize, total);
    }

    public async Task<ReminderResult?> GetAsync(Guid businessId, Guid reminderId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        return await _dbContext.Reminders.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.Id == reminderId)
            .Select(x => ToResult(x))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ReminderResult> CreateAsync(Guid businessId, Guid userId, CreateReminderRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        if (request.CustomerId == Guid.Empty) throw new ArgumentException("Customer is required.");
        if (string.IsNullOrWhiteSpace(request.Title)) throw new ArgumentException("Title is required.");
        if (request.DueAt == default) throw new ArgumentException("Due date is required.");

        if (!await _dbContext.Customers.AnyAsync(x => x.BusinessId == businessId && x.Id == request.CustomerId && x.IsActive, cancellationToken))
            throw new ArgumentException("Customer does not belong to the business or is inactive.");

        if (request.ServiceId.HasValue && !await _dbContext.Services.AnyAsync(x => x.BusinessId == businessId && x.Id == request.ServiceId.Value && x.IsActive, cancellationToken))
            throw new ArgumentException("Service does not belong to the business or is inactive.");

        var reminder = new Reminder
        {
            Id = Guid.NewGuid(), BusinessId = businessId, CustomerId = request.CustomerId, ServiceId = request.ServiceId,
            Title = request.Title.Trim(), DueAt = request.DueAt, Status = ReminderStatus.Pending,
            Note = Normalize(request.Note), CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _dbContext.Reminders.Add(reminder);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResult(reminder);
    }

    public Task<ReminderResult?> CompleteAsync(Guid businessId, Guid reminderId, Guid userId, CompleteReminderRequest request, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(businessId, reminderId, userId, ReminderStatus.Completed, request.Note, cancellationToken);

    public Task<ReminderResult?> CancelAsync(Guid businessId, Guid reminderId, Guid userId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(businessId, reminderId, userId, ReminderStatus.Cancelled, null, cancellationToken);

    private async Task<ReminderResult?> ChangeStatusAsync(
        Guid businessId,
        Guid reminderId,
        Guid userId,
        ReminderStatus status,
        string? completionNote,
        CancellationToken cancellationToken)
    {
        await EnsureMemberAsync(businessId, userId, cancellationToken);
        var reminder = await _dbContext.Reminders.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Id == reminderId, cancellationToken);
        if (reminder is null) return null;
        if (reminder.Status != ReminderStatus.Pending)
            throw new InvalidOperationException("Only pending reminders can be completed or cancelled.");

        if (status == ReminderStatus.Completed && !string.IsNullOrWhiteSpace(completionNote))
        {
            var resultNote = $"نتیجه اقدام: {completionNote.Trim()}";
            reminder.Note = string.IsNullOrWhiteSpace(reminder.Note)
                ? resultNote
                : $"{reminder.Note.Trim()}\n{resultNote}";
        }

        reminder.Status = status;
        reminder.CompletedAt = status == ReminderStatus.Completed ? DateTime.UtcNow : null;
        reminder.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResult(reminder);
    }

    private async Task EnsureMemberAsync(Guid businessId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.BusinessMembers.AnyAsync(x => x.BusinessId == businessId && x.UserId == userId, cancellationToken))
            throw new UnauthorizedAccessException("The user is not a member of this business.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ReminderResult ToResult(Reminder x) => new(x.Id, x.BusinessId, x.CustomerId, x.ServiceId, x.Title, x.DueAt, x.Status, x.Note, x.CreatedByUserId, x.CompletedAt, x.CreatedAt, x.UpdatedAt);
}
