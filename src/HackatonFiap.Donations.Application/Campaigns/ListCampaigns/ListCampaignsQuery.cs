using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.Campaigns;
using HackatonFiap.Donations.Domain.Common;

namespace HackatonFiap.Donations.Application.Campaigns.ListCampaigns;

public sealed record ListCampaignsQuery;

public sealed class ListCampaignsQueryHandler
{
    private readonly ICampaignRepository _repository;

    public ListCampaignsQueryHandler(ICampaignRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyList<CampaignResponse>>> Handle(ListCampaignsQuery query, CancellationToken cancellationToken)
    {
        var campaigns = await _repository.ListAsync(cancellationToken);
        IReadOnlyList<CampaignResponse> responses = campaigns.Select(c => c.ToResponse()).ToList();
        return Result.Success(responses);
    }
}
