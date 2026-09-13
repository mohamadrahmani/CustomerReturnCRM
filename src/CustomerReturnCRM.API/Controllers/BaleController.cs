using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CustomerReturnCRM.Application.Bale;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Route("api/bale")]
public sealed class BaleController : ControllerBase
{
    [HttpPost("businesses/{businessId:guid}/customers/{customerId:guid}/connect")]
    [Authorize]
    public async Task<ActionResult<BaleConnectInviteResult>> CreateConnectInvite(
        Guid businessId,
        Guid customerId,
        [FromServices] IBaleService baleService,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        try
        {
            var result = await baleService.CreateConnectInviteAsync(businessId, customerId, userId, cancellationToken);
            return result is null ? Forbid() : Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: exception.Message);
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(
        [FromBody] JsonElement update,
        [FromServices] IBaleService baleService,
        [FromServices] IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var expectedSecret = configuration["Bale:WebhookSecret"];
        if (!string.IsNullOrWhiteSpace(expectedSecret))
        {
            var suppliedSecret = Request.Headers["X-Bot-Api-Secret-Token"].FirstOrDefault() ?? string.Empty;
            var expectedBytes = Encoding.UTF8.GetBytes(expectedSecret);
            var suppliedBytes = Encoding.UTF8.GetBytes(suppliedSecret);
            if (expectedBytes.Length != suppliedBytes.Length || !CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes))
                return Unauthorized();
        }

        await baleService.HandleUpdateAsync(update.GetRawText(), cancellationToken);
        return Ok();
    }
}
