using System.Security.Claims;
using CustomerReturnCRM.Application.Common;
using CustomerReturnCRM.Application.ReturnAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/businesses/{businessId:guid}/smart-lists")]
public sealed class SmartListsController : ControllerBase
{
    [HttpGet("overdue")]
    public Task<ActionResult<PagedResult<SmartListItemResult>>> GetOverdue(Guid businessId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromServices] IReturnAnalysisService service = null!, CancellationToken cancellationToken = default) => ExecuteAsync((userId, token) => service.GetOverdueAsync(businessId, userId, page, pageSize, token), cancellationToken);
    [HttpGet("due-soon")]
    public Task<ActionResult<PagedResult<SmartListItemResult>>> GetDueSoon(Guid businessId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromServices] IReturnAnalysisService service = null!, CancellationToken cancellationToken = default) => ExecuteAsync((userId, token) => service.GetDueSoonAsync(businessId, userId, page, pageSize, token), cancellationToken);
    [HttpGet("at-risk")]
    public Task<ActionResult<PagedResult<SmartListItemResult>>> GetAtRisk(Guid businessId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromServices] IReturnAnalysisService service = null!, CancellationToken cancellationToken = default) => ExecuteAsync((userId, token) => service.GetAtRiskAsync(businessId, userId, page, pageSize, token), cancellationToken);
    [HttpGet("no-recent-visit")]
    public Task<ActionResult<PagedResult<SmartListItemResult>>> GetNoRecentVisit(Guid businessId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromServices] IReturnAnalysisService service = null!, CancellationToken cancellationToken = default) => ExecuteAsync((userId, token) => service.GetNoRecentVisitAsync(businessId, userId, page, pageSize, token), cancellationToken);
    [HttpGet("dismissed")]
    public Task<ActionResult<PagedResult<SmartListItemResult>>> GetDismissed(Guid businessId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromServices] IReturnAnalysisService service = null!, CancellationToken cancellationToken = default) => ExecuteAsync((userId, token) => service.GetDismissedAsync(businessId, userId, page, pageSize, token), cancellationToken);

    [HttpPost("dismiss")]
    public Task<IActionResult> Dismiss(Guid businessId, DismissSmartListItemRequest request, [FromServices] IReturnAnalysisService service, CancellationToken cancellationToken) => ExecuteMutationAsync((userId, token) => service.DismissAsync(businessId, userId, request, token), cancellationToken);
    [HttpPost("restore")]
    public Task<IActionResult> Restore(Guid businessId, RestoreSmartListItemRequest request, [FromServices] IReturnAnalysisService service, CancellationToken cancellationToken) => ExecuteMutationAsync((userId, token) => service.RestoreAsync(businessId, userId, request, token), cancellationToken);

    private async Task<ActionResult<PagedResult<SmartListItemResult>>> ExecuteAsync(Func<Guid, CancellationToken, Task<PagedResult<SmartListItemResult>>> query, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return Ok(await query(userId, cancellationToken)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
    private async Task<IActionResult> ExecuteMutationAsync(Func<Guid, CancellationToken, Task<bool>> mutation, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return await mutation(userId, cancellationToken) ? NoContent() : NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}
