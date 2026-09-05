using System.Security.Claims;
using CustomerReturnCRM.Application.AppointmentManagement;
using CustomerReturnCRM.Application.VisitManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/businesses/{businessId:guid}/appointments")]
public sealed class AppointmentsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<object>> List(Guid businessId,[FromQuery]DateTime? from,[FromQuery]DateTime? to,[FromQuery]int page=1,[FromQuery]int pageSize=20,[FromServices]IAppointmentManagementService service=null!,CancellationToken cancellationToken=default){if(!TryGetUserId(out var userId))return Unauthorized();try{return Ok(await service.ListAsync(businessId,userId,from,to,page,pageSize,cancellationToken));}catch(UnauthorizedAccessException){return Forbid();}}
    [HttpGet("{appointmentId:guid}")]
    public async Task<ActionResult<AppointmentResult>> Get(Guid businessId,Guid appointmentId,[FromServices]IAppointmentManagementService service,CancellationToken cancellationToken){if(!TryGetUserId(out var userId))return Unauthorized();try{var result=await service.GetAsync(businessId,appointmentId,userId,cancellationToken);return result is null?NotFound():Ok(result);}catch(UnauthorizedAccessException){return Forbid();}}
    [HttpPost]
    public async Task<ActionResult<AppointmentResult>> Create(Guid businessId,CreateAppointmentRequest request,[FromServices]IAppointmentManagementService service,CancellationToken cancellationToken){if(!TryGetUserId(out var userId))return Unauthorized();try{return StatusCode(StatusCodes.Status201Created,await service.CreateAsync(businessId,userId,request,cancellationToken));}catch(ArgumentException e){return BadRequest(new{error=e.Message});}catch(InvalidOperationException e){return Conflict(new{error=e.Message});}catch(UnauthorizedAccessException){return Forbid();}}
    [HttpPut("{appointmentId:guid}")]
    public async Task<ActionResult<AppointmentResult>> Update(Guid businessId,Guid appointmentId,UpdateAppointmentRequest request,[FromServices]IAppointmentManagementService service,CancellationToken cancellationToken){if(!TryGetUserId(out var userId))return Unauthorized();try{var result=await service.UpdateAsync(businessId,appointmentId,userId,request,cancellationToken);return result is null?NotFound():Ok(result);}catch(ArgumentException e){return BadRequest(new{error=e.Message});}catch(InvalidOperationException e){return Conflict(new{error=e.Message});}catch(UnauthorizedAccessException){return Forbid();}}
    [HttpPost("{appointmentId:guid}/confirm")]
    public async Task<ActionResult<AppointmentResult>> Confirm(Guid businessId,Guid appointmentId,[FromServices]IAppointmentManagementService service,CancellationToken cancellationToken){if(!TryGetUserId(out var userId))return Unauthorized();try{var result=await service.ConfirmAsync(businessId,appointmentId,userId,cancellationToken);return result is null?NotFound():Ok(result);}catch(InvalidOperationException e){return Conflict(new{error=e.Message});}catch(UnauthorizedAccessException){return Forbid();}}
    [HttpPost("{appointmentId:guid}/cancel")]
    public async Task<ActionResult<AppointmentResult>> Cancel(Guid businessId,Guid appointmentId,[FromServices]IAppointmentManagementService service,CancellationToken cancellationToken){if(!TryGetUserId(out var userId))return Unauthorized();try{var result=await service.CancelAsync(businessId,appointmentId,userId,cancellationToken);return result is null?NotFound():Ok(result);}catch(InvalidOperationException e){return Conflict(new{error=e.Message});}catch(UnauthorizedAccessException){return Forbid();}}
    [HttpPost("{appointmentId:guid}/complete")]
    public async Task<ActionResult<VisitResult>> Complete(Guid businessId,Guid appointmentId,CompleteAppointmentRequest request,[FromServices]IVisitManagementService service,CancellationToken cancellationToken){if(!TryGetUserId(out var userId))return Unauthorized();try{var result=await service.CompleteAppointmentAsync(businessId,appointmentId,userId,request,cancellationToken);return result is null?NotFound():StatusCode(StatusCodes.Status201Created,result);}catch(ArgumentException e){return BadRequest(new{error=e.Message});}catch(InvalidOperationException e){return Conflict(new{error=e.Message});}catch(UnauthorizedAccessException){return Forbid();}}
    private bool TryGetUserId(out Guid userId)=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)??User.FindFirstValue("sub"),out userId);
}
