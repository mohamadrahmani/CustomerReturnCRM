using System.Security.Claims;
using CustomerReturnCRM.Application.ServiceManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/businesses/{businessId:guid}/services")]
public sealed class ServicesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceResult>>> List(Guid businessId, [FromServices] IServiceManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return Ok(await service.ListAsync(businessId, userId, cancellationToken)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("{serviceId:guid}")]
    public async Task<ActionResult<ServiceResult>> Get(Guid businessId, Guid serviceId, [FromServices] IServiceManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { var result = await service.GetAsync(businessId, serviceId, userId, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResult>> Create(Guid businessId, CreateServiceRequest request, [FromServices] IServiceManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return StatusCode(StatusCodes.Status201Created, await service.CreateAsync(businessId, userId, request, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("{serviceId:guid}")]
    public async Task<ActionResult<ServiceResult>> Update(Guid businessId, Guid serviceId, UpdateServiceRequest request, [FromServices] IServiceManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { var result = await service.UpdateAsync(businessId, serviceId, userId, request, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpDelete("{serviceId:guid}")]
    public async Task<IActionResult> Delete(Guid businessId, Guid serviceId, [FromServices] IServiceManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return await service.DeactivateAsync(businessId, serviceId, userId, cancellationToken) ? NoContent() : NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}
