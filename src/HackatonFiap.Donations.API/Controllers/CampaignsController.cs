using HackatonFiap.Donations.API.Common;
using HackatonFiap.Donations.Application.Campaigns;
using HackatonFiap.Donations.Application.Campaigns.ChangeCampaignStatus;
using HackatonFiap.Donations.Application.Campaigns.CreateCampaign;
using HackatonFiap.Donations.Application.Campaigns.GetCampaignById;
using HackatonFiap.Donations.Application.Campaigns.ListCampaigns;
using HackatonFiap.Donations.Application.Campaigns.UpdateCampaign;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HackatonFiap.Donations.API.Controllers;

[ApiController]
[Route("api/campaigns")]
[Authorize(Policy = "ManagersOnly")]
public sealed class CampaignsController : ControllerBase
{
    public sealed record CreateCampaignRequest(string Title, string Description, DateTime StartDate, DateTime EndDate, decimal Goal);
    public sealed record UpdateCampaignRequest(string Title, string Description, DateTime StartDate, DateTime EndDate, decimal Goal);
    public sealed record ChangeStatusRequest(CampaignStatusAction Action);

    private readonly CreateCampaignCommandHandler _create;
    private readonly UpdateCampaignCommandHandler _update;
    private readonly ChangeCampaignStatusCommandHandler _changeStatus;
    private readonly GetCampaignByIdQueryHandler _getById;
    private readonly ListCampaignsQueryHandler _list;

    public CampaignsController(CreateCampaignCommandHandler create, UpdateCampaignCommandHandler update,
        ChangeCampaignStatusCommandHandler changeStatus, GetCampaignByIdQueryHandler getById, ListCampaignsQueryHandler list)
    {
        _create = create;
        _update = update;
        _changeStatus = changeStatus;
        _getById = getById;
        _list = list;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCampaignRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCampaignCommand(request.Title, request.Description, request.StartDate, request.EndDate, request.Goal, User.GetUserId());
        var result = await _create.Handle(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCampaignRequest request, CancellationToken cancellationToken)
    {
        var result = await _update.Handle(new UpdateCampaignCommand(id, request.Title, request.Description, request.StartDate, request.EndDate, request.Goal), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _changeStatus.Handle(new ChangeCampaignStatusCommand(id, request.Action), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _list.Handle(new ListCampaignsQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getById.Handle(new GetCampaignByIdQuery(id), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Ok(result.Value);
    }
}
