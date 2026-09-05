using System.Security.Claims;
using CustomerReturnCRM.Application.ReturnAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/businesses/{businessId:guid}/return-analysis")]
public sealed class ReturnAnalysisController : ControllerBase
{
    [HttpGet("customers/{customerId:guid}")]
    public async Task<ActionResult<CustomerReturnAnalysisResult>> GetCustomerAnalysis(
        Guid businessId,
        Guid customerId,
        [FromServices] IReturnAnalysisService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await service.GetCustomerAnalysisAsync(
                businessId,
                customerId,
                userId,
                cancellationToken);
            return result is null ? NotFound() : Ok(result);
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
