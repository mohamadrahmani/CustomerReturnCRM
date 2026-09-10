using System.Security.Claims;
using CustomerReturnCRM.Application.Common;
using CustomerReturnCRM.Application.Sms;
using CustomerReturnCRM.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerReturnCRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/businesses/{businessId:guid}/sms")]
public sealed class SmsController : ControllerBase
{
    [HttpGet("templates")]
    public async Task<ActionResult<IReadOnlyList<SmsTemplateResult>>> ListTemplates(Guid businessId, [FromQuery] bool activeOnly = true, [FromServices] ISmsManagementService service = null!, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return Ok(await service.ListTemplatesAsync(businessId, userId, activeOnly, cancellationToken)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("templates/{templateId:guid}")]
    public async Task<ActionResult<SmsTemplateResult>> GetTemplate(Guid businessId, Guid templateId, [FromServices] ISmsManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { var result = await service.GetTemplateAsync(businessId, templateId, userId, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("templates")]
    public async Task<ActionResult<SmsTemplateResult>> CreateTemplate(Guid businessId, CreateSmsTemplateRequest request, [FromServices] ISmsManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return StatusCode(StatusCodes.Status201Created, await service.CreateTemplateAsync(businessId, userId, request, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("templates/{templateId:guid}")]
    public async Task<ActionResult<SmsTemplateResult>> UpdateTemplate(Guid businessId, Guid templateId, UpdateSmsTemplateRequest request, [FromServices] ISmsManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { var result = await service.UpdateTemplateAsync(businessId, templateId, userId, request, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("campaigns")]
    public async Task<ActionResult<PagedResult<SmsCampaignResult>>> ListCampaigns(Guid businessId, [FromQuery] SmsCampaignStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromServices] ISmsManagementService service = null!, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return Ok(await service.ListCampaignsAsync(businessId, userId, status, page, pageSize, cancellationToken)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("campaigns/{campaignId:guid}")]
    public async Task<ActionResult<SmsCampaignResult>> GetCampaign(Guid businessId, Guid campaignId, [FromQuery] bool includeRecipients = true, [FromServices] ISmsManagementService service = null!, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { var result = await service.GetCampaignAsync(businessId, campaignId, userId, includeRecipients, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("campaigns")]
    public async Task<ActionResult<SmsCampaignResult>> CreateCampaign(Guid businessId, CreateSmsCampaignRequest request, [FromServices] ISmsManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { return StatusCode(StatusCodes.Status201Created, await service.CreateCampaignAsync(businessId, userId, request, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("campaigns/{campaignId:guid}/cancel")]
    public async Task<ActionResult<SmsCampaignResult>> CancelCampaign(Guid businessId, Guid campaignId, [FromServices] ISmsManagementService service, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try { var result = await service.CancelCampaignAsync(businessId, campaignId, userId, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}
