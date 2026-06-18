using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Domain.Entities;
using HackatonFiap.Donations.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HackatonFiap.Donations.Infrastructure.Persistence.Repositories;

public sealed class CampaignRepository : ICampaignRepository
{
    private readonly DonationsDbContext _context;

    public CampaignRepository(DonationsDbContext context) => _context = context;

    public Task<Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Campaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Campaign campaign, CancellationToken cancellationToken = default)
        => await _context.Campaigns.AddAsync(campaign, cancellationToken);

    public async Task<IReadOnlyList<Campaign>> ListAsync(CancellationToken cancellationToken = default)
        => await _context.Campaigns.AsNoTracking().OrderByDescending(c => c.CreatedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Campaign>> ListActiveExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default)
        => await _context.Campaigns
            .Where(c => c.Status == CampaignStatus.Active && c.Period.EndDate < utcNow)
            .ToListAsync(cancellationToken);
}
