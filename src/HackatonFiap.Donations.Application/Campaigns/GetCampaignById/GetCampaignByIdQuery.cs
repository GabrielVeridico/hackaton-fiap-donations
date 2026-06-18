using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.Campaigns;
using HackatonFiap.Donations.Application.Errors;
using HackatonFiap.Donations.Domain.Common;

namespace HackatonFiap.Donations.Application.Campaigns.GetCampaignById;

public sealed record GetCampaignByIdQuery(Guid Id);

public sealed class GetCampaignByIdQueryHandler
{
    private readonly ICampaignRepository _repository;

    public GetCampaignByIdQueryHandler(ICampaignRepository repository) => _repository = repository;

    public async Task<Result<CampaignResponse>> Handle(GetCampaignByIdQuery query, CancellationToken cancellationToken)
    {
        var campaign = await _repository.GetByIdAsync(query.Id, cancellationToken);
        if (campaign is null)
        {
            return Result.Failure<CampaignResponse>(CampaignErrors.NotFound);
        }

        return Result.Success(campaign.ToResponse());
    }
}
