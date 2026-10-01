using CustomerReturnCRM.Application.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthenticationResult>> Register(RegisterRequest request,[FromServices] IAuthenticationService service,CancellationToken cancellationToken)
    {
        try { return StatusCode(StatusCodes.Status201Created, await service.RegisterAsync(request, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResult>> Login(LoginRequest request,[FromServices] IAuthenticationService service,CancellationToken cancellationToken)
    {
        try { var result = await service.LoginAsync(request, cancellationToken); return result is null ? Unauthorized(new { error = "شماره موبایل یا رمز عبور نادرست است." }) : Ok(result); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost("forgot-password/request")]
    public async Task<IActionResult> RequestPasswordReset(ForgotPasswordRequest request,[FromServices] IAuthenticationService service,CancellationToken cancellationToken)
    {
        try { await service.RequestPasswordResetAsync(request, cancellationToken); } catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); } catch (InvalidOperationException exception) { return BadRequest(new { error = exception.Message }); }
        return Ok(new { message = "اگر شماره موبایل ثبت شده باشد، کد تأیید ارسال می‌شود." });
    }

    [HttpPost("forgot-password/verify")]
    public async Task<ActionResult<OtpVerificationResult>> VerifyOtp(VerifyOtpRequest request,[FromServices] IAuthenticationService service,CancellationToken cancellationToken)
    {
        return Ok(await service.VerifyPasswordResetOtpAsync(request, cancellationToken));
    }

    [HttpPost("forgot-password/reset")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request,[FromServices] IAuthenticationService service,CancellationToken cancellationToken)
    {
        try { await service.ResetPasswordAsync(request, cancellationToken); return Ok(new { message = "رمز عبور با موفقیت تغییر کرد." }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return BadRequest(new { error = exception.Message }); }
    }
}
