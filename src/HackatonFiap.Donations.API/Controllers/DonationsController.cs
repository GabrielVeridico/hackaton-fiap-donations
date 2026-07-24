using HackatonFiap.Donations.API.Common;
using HackatonFiap.Donations.Application.Donations.CreateDonation;
using HackatonFiap.Donations.Application.Donations.GetDonationById;
using HackatonFiap.Donations.Application.Donations.ListMyDonations;
using HackatonFiap.Donations.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HackatonFiap.Donations.API.Controllers;

[ApiController]
[Route("api/donations")]
[Authorize(Policy = "DonorOnly")]
public sealed class DonationsController : ControllerBase
{
    public sealed record CreateDonationRequest(Guid CampaignId, decimal Amount, PaymentMethod PaymentMethod);

    private readonly CreateDonationCommandHandler _create;
    private readonly GetDonationByIdQueryHandler _getById;
    private readonly ListMyDonationsQueryHandler _listMine;

    public DonationsController(CreateDonationCommandHandler create, GetDonationByIdQueryHandler getById,
        ListMyDonationsQueryHandler listMine)
    {
        _create = create;
        _getById = getById;
        _listMine = listMine;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDonationRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateDonationCommand(request.CampaignId, request.Amount, request.PaymentMethod,
            User.GetUserId(), User.GetEmail(), User.GetName());
        var result = await _create.Handle(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return Accepted(new { donationId = result.Value, status = "Pending" });
    }

    [HttpGet]
    public async Task<IActionResult> ListMine(CancellationToken cancellationToken)
    {
        var result = await _listMine.Handle(new ListMyDonationsQuery(User.GetUserId()), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getById.Handle(new GetDonationByIdQuery(id, User.GetUserId()), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Ok(result.Value);
    }
}
