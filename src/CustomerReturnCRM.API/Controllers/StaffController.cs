using System.Security.Claims;
using CustomerReturnCRM.Application.StaffManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/businesses/{businessId:guid}/staff")]
public sealed class StaffController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<object>> List(
        Guid businessId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IStaffManagementService service = null!,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return Ok(await service.ListAsync(businessId, userId, page, pageSize, cancellationToken)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("{staffId:guid}")]
    public async Task<ActionResult<StaffResult>> Get(
        Guid businessId,
        Guid staffId,
        [FromServices] IStaffManagementService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            var result = await service.GetAsync(businessId, staffId, userId, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost]
    public async Task<ActionResult<StaffResult>> Create(
        Guid businessId,
        CreateStaffRequest request,
        [FromServices] IStaffManagementService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return StatusCode(StatusCodes.Status201Created, await service.CreateAsync(businessId, userId, request, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("{staffId:guid}")]
    public async Task<ActionResult<StaffResult>> Update(
        Guid businessId,
        Guid staffId,
        UpdateStaffRequest request,
        [FromServices] IStaffManagementService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            var result = await service.UpdateAsync(businessId, staffId, userId, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpDelete("{staffId:guid}")]
    public async Task<IActionResult> Delete(
        Guid businessId,
        Guid staffId,
        [FromServices] IStaffManagementService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return await service.DeactivateAsync(businessId, staffId, userId, cancellationToken) ? NoContent() : NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}
