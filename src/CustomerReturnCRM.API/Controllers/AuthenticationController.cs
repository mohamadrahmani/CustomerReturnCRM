using CustomerReturnCRM.Application.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthenticationResult>> Register(
        RegisterRequest request,
        [FromServices] IAuthenticationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return StatusCode(
                StatusCodes.Status201Created,
                await service.RegisterAsync(request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResult>> Login(
        LoginRequest request,
        [FromServices] IAuthenticationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.LoginAsync(request, cancellationToken);
            return result is null
                ? Unauthorized(new { error = "Invalid email or password." })
                : Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
