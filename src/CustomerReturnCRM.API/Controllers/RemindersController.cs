using System.Security.Claims;
using CustomerReturnCRM.Application.ReminderManagement;
using CustomerReturnCRM.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/businesses/{businessId:guid}/reminders")]
public sealed class RemindersController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReminderResult>>> List(Guid businessId, [FromQuery] ReminderStatus? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromServices] IReminderManagementService service = null!, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return Ok(await service.ListAsync(businessId, userId, status, from, to, page, pageSize, cancellationToken)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("{reminderId:guid}")]
    public async Task<ActionResult<ReminderResult>> Get(Guid businessId, Guid reminderId, [FromServices] IReminderManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { var result = await service.GetAsync(businessId, reminderId, userId, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost]
    public async Task<ActionResult<ReminderResult>> Create(Guid businessId, CreateReminderRequest request, [FromServices] IReminderManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return StatusCode(StatusCodes.Status201Created, await service.CreateAsync(businessId, userId, request, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("{reminderId:guid}/complete")]
    public Task<IActionResult> Complete(Guid businessId, Guid reminderId, CompleteReminderRequest request, [FromServices] IReminderManagementService service, CancellationToken cancellationToken) =>
        ChangeStatusAsync((userId, token) => service.CompleteAsync(businessId, reminderId, userId, request, token), cancellationToken);

    [HttpPost("{reminderId:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid businessId, Guid reminderId, [FromServices] IReminderManagementService service, CancellationToken cancellationToken) =>
        ChangeStatusAsync((userId, token) => service.CancelAsync(businessId, reminderId, userId, token), cancellationToken);

    private async Task<IActionResult> ChangeStatusAsync(Func<Guid, CancellationToken, Task<ReminderResult?>> operation, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { var result = await operation(userId, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}
