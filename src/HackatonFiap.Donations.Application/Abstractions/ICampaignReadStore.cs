using HackatonFiap.Donations.Application.ReadModels;

namespace HackatonFiap.Donations.Application.Abstractions;

public interface ICampaignReadStore
{
    Task UpsertAsync(CampaignReadModel campaign, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CampaignReadModel>> ListActiveAsync(CancellationToken cancellationToken = default);
}
