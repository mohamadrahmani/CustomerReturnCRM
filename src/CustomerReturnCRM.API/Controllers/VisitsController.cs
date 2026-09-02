using System.Security.Claims;
using CustomerReturnCRM.Application.VisitManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/businesses/{businessId:guid}/visits")]
public sealed class VisitsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VisitResult>>> List(
        Guid businessId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromServices] IVisitManagementService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await service.ListAsync(businessId, userId, from, to, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{visitId:guid}")]
    public async Task<ActionResult<VisitResult>> Get(
        Guid businessId,
        Guid visitId,
        [FromServices] IVisitManagementService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await service.GetAsync(businessId, visitId, userId, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    public async Task<ActionResult<VisitResult>> Create(
        Guid businessId,
        CreateVisitRequest request,
        [FromServices] IVisitManagementService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            return StatusCode(
                StatusCodes.Status201Created,
                await service.CreateAsync(businessId, userId, request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
            out userId);
}
