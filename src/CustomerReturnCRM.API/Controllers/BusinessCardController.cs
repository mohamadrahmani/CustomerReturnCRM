using System.Security.Claims;
using CustomerReturnCRM.Application.BusinessCard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/business-card")]
public sealed class BusinessCardController : ControllerBase
{
    [HttpPost("businesses/{businessId:guid}/customers/{customerId:guid}/share")]
    public async Task<ActionResult<BusinessCardShareResult>> Share(
        Guid businessId,
        Guid customerId,
        [FromQuery] BusinessCardShareChannel channel,
        [FromServices] IBusinessCardSharingService service,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        try
        {
            var result = await service.ShareAsync(businessId, customerId, userId, channel, cancellationToken);
            return result is null ? Forbid() : Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: exception.Message);
        }
    }
}
