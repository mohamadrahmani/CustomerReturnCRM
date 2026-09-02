using System.Security.Claims;
using CustomerReturnCRM.Application.AppointmentManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/businesses/{businessId:guid}/appointments")]
public sealed class AppointmentsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppointmentResult>>> List(Guid businessId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromServices] IAppointmentManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return Ok(await service.ListAsync(businessId, userId, from, to, cancellationToken)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("{appointmentId:guid}")]
    public async Task<ActionResult<AppointmentResult>> Get(Guid businessId, Guid appointmentId, [FromServices] IAppointmentManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { var result = await service.GetAsync(businessId, appointmentId, userId, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentResult>> Create(Guid businessId, CreateAppointmentRequest request, [FromServices] IAppointmentManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return StatusCode(StatusCodes.Status201Created, await service.CreateAsync(businessId, userId, request, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("{appointmentId:guid}")]
    public async Task<ActionResult<AppointmentResult>> Update(Guid businessId, Guid appointmentId, UpdateAppointmentRequest request, [FromServices] IAppointmentManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { var result = await service.UpdateAsync(businessId, appointmentId, userId, request, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("{appointmentId:guid}/cancel")]
    public async Task<ActionResult<AppointmentResult>> Cancel(Guid businessId, Guid appointmentId, [FromServices] IAppointmentManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { var result = await service.CancelAsync(businessId, appointmentId, userId, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}
