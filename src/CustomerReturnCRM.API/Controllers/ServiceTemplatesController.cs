using CustomerReturnCRM.Application.ServiceTemplateManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/service-templates")]
public sealed class ServiceTemplatesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceTemplateResult>>> List(
        [FromQuery] string? businessType,
        [FromServices] IServiceTemplateManagementService service,
        CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(businessType, cancellationToken));
}
