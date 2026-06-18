using System.Collections.Concurrent;
using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.ReadModels;

namespace HackatonFiap.Donations.Infrastructure.ReadStore;

public sealed class InMemoryCampaignReadStore : ICampaignReadStore
{
    private readonly ConcurrentDictionary<Guid, CampaignReadModel> _store = new();

    public Task UpsertAsync(CampaignReadModel campaign, CancellationToken cancellationToken = default)
    {
        _store[campaign.Id] = campaign;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CampaignReadModel>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CampaignReadModel> active = _store.Values
            .Where(c => c.Status == "Active")
            .ToList();
        return Task.FromResult(active);
    }
}
