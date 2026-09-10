using CustomerReturnCRM.Application.PublicBooking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public")]
public sealed class PublicBookingController : ControllerBase
{
    [HttpGet("businesses/{slug}/profile")]
    public async Task<ActionResult<PublicBusinessProfileResult>> GetBusiness(
        string slug,
        [FromServices] IPublicBookingService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetBusinessAsync(slug, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("businesses/{slug}/bookings")]
    public async Task<ActionResult<PublicBookingResult>> Book(
        string slug,
        PublicBookingRequest request,
        [FromServices] IPublicBookingService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return StatusCode(StatusCodes.Status201Created, await service.BookAsync(slug, request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { error = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return Conflict(new { error = e.Message });
        }
    }

    [HttpGet("appointments/{publicCode}")]
    public async Task<ActionResult<PublicAppointmentResult>> GetAppointment(
        string publicCode,
        [FromServices] IPublicBookingService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAppointmentAsync(publicCode, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
