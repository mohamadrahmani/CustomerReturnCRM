using System.Security.Claims;
using CustomerReturnCRM.Application.CustomerManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/businesses/{businessId:guid}/customers")]
public sealed class CustomersController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<object>> List(Guid businessId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromServices] ICustomerManagementService service = null!, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return Ok(await service.ListAsync(businessId, userId, page, pageSize, search, isActive, cancellationToken)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("{customerId:guid}")]
    public async Task<ActionResult<CustomerResult>> Get(Guid businessId, Guid customerId, [FromServices] ICustomerManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { var result = await service.GetAsync(businessId, customerId, userId, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResult>> Create(Guid businessId, CreateCustomerRequest request, [FromServices] ICustomerManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return StatusCode(StatusCodes.Status201Created, await service.CreateAsync(businessId, userId, request, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("{customerId:guid}")]
    public async Task<ActionResult<CustomerResult>> Update(Guid businessId, Guid customerId, UpdateCustomerRequest request, [FromServices] ICustomerManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { var result = await service.UpdateAsync(businessId, customerId, userId, request, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpDelete("{customerId:guid}")]
    public async Task<IActionResult> Delete(Guid businessId, Guid customerId, [FromServices] ICustomerManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return await service.DeactivateAsync(businessId, customerId, userId, cancellationToken) ? NoContent() : NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}
