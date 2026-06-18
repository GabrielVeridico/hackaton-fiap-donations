using HackatonFiap.Donations.Domain.Entities;

namespace HackatonFiap.Donations.Application.Abstractions;

public interface ICampaignRepository
{
    Task<Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Campaign campaign, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Campaign>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Campaign>> ListActiveExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
