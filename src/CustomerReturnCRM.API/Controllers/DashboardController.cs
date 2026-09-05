using System.Security.Claims;
using CustomerReturnCRM.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/businesses/{businessId:guid}/dashboard")]
public sealed class DashboardController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardResult>> Get(
        Guid businessId,
        [FromServices] IDashboardService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            return Ok(await service.GetAsync(businessId, userId, cancellationToken));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
            out userId);
}
