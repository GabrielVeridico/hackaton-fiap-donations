using HackatonFiap.Donations.Application.Transparency;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HackatonFiap.Donations.API.Controllers;

[ApiController]
[Route("api/transparency")]
[AllowAnonymous]
public sealed class TransparencyController : ControllerBase
{
    private readonly ListActiveCampaignsQueryHandler _handler;

    public TransparencyController(ListActiveCampaignsQueryHandler handler) => _handler = handler;

    [HttpGet("campaigns")]
    public async Task<IActionResult> ListCampaigns(CancellationToken cancellationToken)
    {
        var result = await _handler.Handle(new ListActiveCampaignsQuery(), cancellationToken);
        return Ok(result.Value);
    }
}
