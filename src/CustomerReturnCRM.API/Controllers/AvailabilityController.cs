using System.Security.Claims;
using CustomerReturnCRM.Application.Availability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Route("api/businesses/{businessId:guid}/availability")]
[Authorize]
public sealed class AvailabilityController : ControllerBase
{
    [HttpGet("working-hours")]
    public async Task<ActionResult<IReadOnlyList<WorkingHourResult>>> GetBusinessHours(Guid businessId, [FromServices] IAvailabilityManagementService service, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return Ok(await service.GetBusinessWorkingHoursAsync(businessId, userId, ct)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("working-hours")]
    public async Task<ActionResult<IReadOnlyList<WorkingHourResult>>> ReplaceBusinessHours(Guid businessId, IReadOnlyCollection<WorkingHourRequest> request, [FromServices] IAvailabilityManagementService service, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return Ok(await service.ReplaceBusinessWorkingHoursAsync(businessId, userId, request, ct)); }
        catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("staff/{staffId:guid}/working-hours")]
    public async Task<ActionResult<IReadOnlyList<WorkingHourResult>>> GetStaffHours(Guid businessId, Guid staffId, [FromServices] IAvailabilityManagementService service, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return Ok(await service.GetStaffWorkingHoursAsync(businessId, staffId, userId, ct)); }
        catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("staff/{staffId:guid}/working-hours")]
    public async Task<ActionResult<IReadOnlyList<WorkingHourResult>>> ReplaceStaffHours(Guid businessId, Guid staffId, IReadOnlyCollection<WorkingHourRequest> request, [FromServices] IAvailabilityManagementService service, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return Ok(await service.ReplaceStaffWorkingHoursAsync(businessId, staffId, userId, request, ct)); }
        catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("staff/{staffId:guid}/time-off")]
    public async Task<ActionResult<IReadOnlyList<StaffTimeOffResult>>> ListTimeOff(Guid businessId, Guid staffId, [FromQuery] DateTime from, [FromQuery] DateTime to, [FromServices] IAvailabilityManagementService service, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return Ok(await service.ListTimeOffAsync(businessId, staffId, userId, from, to, ct)); }
        catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("staff/{staffId:guid}/time-off")]
    public async Task<ActionResult<StaffTimeOffResult>> AddTimeOff(Guid businessId, Guid staffId, [FromBody] CreateTimeOffRequest request, [FromServices] IAvailabilityManagementService service, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return StatusCode(StatusCodes.Status201Created, await service.AddTimeOffAsync(businessId, staffId, userId, request.StartAt, request.EndAt, request.Reason, ct)); }
        catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpDelete("staff/{staffId:guid}/time-off/{timeOffId:guid}")]
    public async Task<IActionResult> DeleteTimeOff(Guid businessId, Guid staffId, Guid timeOffId, [FromServices] IAvailabilityManagementService service, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return await service.DeleteTimeOffAsync(businessId, staffId, timeOffId, userId, ct) ? NoContent() : NotFound(); }
        catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}

public sealed class CreateTimeOffRequest
{
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public string? Reason { get; init; }
}

[ApiController]
[Route("api/public/businesses/{businessId:guid}/availability")]
public sealed class PublicAvailabilityController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<AvailabilitySlotResult>>> Get(Guid businessId, [FromQuery] AvailabilityRequest request, [FromServices] IAvailabilityManagementService service, CancellationToken ct)
    {
        try { return Ok(await service.GetAvailableSlotsAsync(businessId, request, ct)); }
        catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
    }
}
