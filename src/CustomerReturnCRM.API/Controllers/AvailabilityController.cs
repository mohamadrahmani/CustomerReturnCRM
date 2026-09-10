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
        => Ok(await service.GetBusinessWorkingHoursAsync(businessId, UserId(), ct));

    [HttpPut("working-hours")]
    public async Task<ActionResult<IReadOnlyList<WorkingHourResult>>> ReplaceBusinessHours(Guid businessId, IReadOnlyCollection<WorkingHourRequest> request, [FromServices] IAvailabilityManagementService service, CancellationToken ct)
        => Ok(await service.ReplaceBusinessWorkingHoursAsync(businessId, UserId(), request, ct));

    [HttpGet("staff/{staffId:guid}/working-hours")]
    public async Task<ActionResult<IReadOnlyList<WorkingHourResult>>> GetStaffHours(Guid businessId, Guid staffId, [FromServices] IAvailabilityManagementService service, CancellationToken ct)
        => Ok(await service.GetStaffWorkingHoursAsync(businessId, staffId, UserId(), ct));

    [HttpPut("staff/{staffId:guid}/working-hours")]
    public async Task<ActionResult<IReadOnlyList<WorkingHourResult>>> ReplaceStaffHours(Guid businessId, Guid staffId, IReadOnlyCollection<WorkingHourRequest> request, [FromServices] IAvailabilityManagementService service, CancellationToken ct)
        => Ok(await service.ReplaceStaffWorkingHoursAsync(businessId, staffId, UserId(), request, ct));

    [HttpGet("staff/{staffId:guid}/time-off")]
    public async Task<ActionResult<IReadOnlyList<StaffTimeOffResult>>> ListTimeOff(Guid businessId, Guid staffId, [FromQuery] DateTime from, [FromQuery] DateTime to, [FromServices] IAvailabilityManagementService service, CancellationToken ct)
        => Ok(await service.ListTimeOffAsync(businessId, staffId, UserId(), from, to, ct));

    [HttpPost("staff/{staffId:guid}/time-off")]
    public async Task<ActionResult<StaffTimeOffResult>> AddTimeOff(Guid businessId, Guid staffId, [FromBody] CreateTimeOffRequest request, [FromServices] IAvailabilityManagementService service, CancellationToken ct)
        => Ok(await service.AddTimeOffAsync(businessId, staffId, UserId(), request.StartAt, request.EndAt, request.Reason, ct));

    [HttpDelete("staff/{staffId:guid}/time-off/{timeOffId:guid}")]
    public async Task<IActionResult> DeleteTimeOff(Guid businessId, Guid staffId, Guid timeOffId, [FromServices] IAvailabilityManagementService service, CancellationToken ct)
        => await service.DeleteTimeOffAsync(businessId, staffId, timeOffId, UserId(), ct) ? NoContent() : NotFound();

    private Guid UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(value, out var id)) throw new UnauthorizedAccessException("Authenticated user id is missing.");
        return id;
    }
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
        => Ok(await service.GetAvailableSlotsAsync(businessId, request, ct));
}
