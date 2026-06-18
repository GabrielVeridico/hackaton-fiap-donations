using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Domain.Common;

namespace HackatonFiap.Donations.Application.Transparency;

public sealed record ListActiveCampaignsQuery;

public sealed class ListActiveCampaignsQueryHandler
{
    private readonly ICampaignReadStore _readStore;

    public ListActiveCampaignsQueryHandler(ICampaignReadStore readStore) => _readStore = readStore;

    public async Task<Result<IReadOnlyList<TransparencyCampaignResponse>>> Handle(
        ListActiveCampaignsQuery query, CancellationToken cancellationToken)
    {
        var campaigns = await _readStore.ListActiveAsync(cancellationToken);
        IReadOnlyList<TransparencyCampaignResponse> responses = campaigns
            .Select(c => new TransparencyCampaignResponse(
                c.Title, c.Goal, c.AmountRaised,
                c.Goal > 0m ? Math.Round(c.AmountRaised / c.Goal * 100m, 2) : 0m))
            .ToList();

        return Result.Success(responses);
    }
}
