using System.Security.Claims;
using CustomerReturnCRM.Application.BusinessSetup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/businesses")]
public sealed class BusinessesController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BusinessSetupResult>> Create(
        BusinessSetupRequest request,
        [FromServices] IBusinessSetupService businessSetupService,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await businessSetupService.CreateAsync(request, userId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Unauthorized(new { error = exception.Message });
        }
    }
}
