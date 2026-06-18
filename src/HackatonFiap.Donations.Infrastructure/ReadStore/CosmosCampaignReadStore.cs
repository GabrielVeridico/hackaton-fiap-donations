using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.ReadModels;
using Microsoft.Azure.Cosmos;

namespace HackatonFiap.Donations.Infrastructure.ReadStore;

public sealed class CosmosCampaignReadStore : ICampaignReadStore
{
    private readonly Container _container;

    private sealed record CampaignDocument(string id, string title, decimal goal, decimal amountRaised, string status);

    public CosmosCampaignReadStore(CosmosClient client, CosmosOptions options)
    {
        _container = client.GetContainer(options.Database, options.Container);
    }

    public async Task UpsertAsync(CampaignReadModel campaign, CancellationToken cancellationToken = default)
    {
        var doc = new CampaignDocument(campaign.Id.ToString(), campaign.Title, campaign.Goal, campaign.AmountRaised, campaign.Status);
        await _container.UpsertItemAsync(doc, new PartitionKey(doc.id), cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<CampaignReadModel>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.status = @status").WithParameter("@status", "Active");
        var iterator = _container.GetItemQueryIterator<CampaignDocument>(query);

        var results = new List<CampaignReadModel>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page.Select(d => new CampaignReadModel(Guid.Parse(d.id), d.title, d.goal, d.amountRaised, d.status)));
        }

        return results;
    }
}
