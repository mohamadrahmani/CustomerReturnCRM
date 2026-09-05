using CustomerReturnCRM.Domain.Entities;
using CustomerReturnCRM.Application.Common;

namespace CustomerReturnCRM.Application.ReminderManagement;

public sealed class CreateReminderRequest
{
    public Guid CustomerId { get; init; }
    public Guid? ServiceId { get; init; }
    public string Title { get; init; } = null!;
    public DateTime DueAt { get; init; }
    public string? Note { get; init; }
}

public sealed record ReminderResult(
    Guid Id,
    Guid BusinessId,
    Guid CustomerId,
    Guid? ServiceId,
    string Title,
    DateTime DueAt,
    ReminderStatus Status,
    string? Note,
    Guid CreatedByUserId,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public interface IReminderManagementService
{
    Task<PagedResult<ReminderResult>> ListAsync(
        Guid businessId,
        Guid userId,
        ReminderStatus? status,
        DateTime? from,
        DateTime? to,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ReminderResult?> GetAsync(
        Guid businessId,
        Guid reminderId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ReminderResult> CreateAsync(
        Guid businessId,
        Guid userId,
        CreateReminderRequest request,
        CancellationToken cancellationToken = default);

    Task<ReminderResult?> CompleteAsync(
        Guid businessId,
        Guid reminderId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ReminderResult?> CancelAsync(
        Guid businessId,
        Guid reminderId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
