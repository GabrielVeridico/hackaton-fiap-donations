using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HackatonFiap.Donations.Infrastructure.Persistence.Repositories;

public sealed class DonationRepository : IDonationRepository
{
    private readonly DonationsDbContext _context;

    public DonationRepository(DonationsDbContext context) => _context = context;

    public Task<Donation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Donations.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Donation>> ListByDonorAsync(Guid donorId, CancellationToken cancellationToken = default)
        => await _context.Donations
            .AsNoTracking()
            .Where(d => d.DonorId == donorId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Donation donation, CancellationToken cancellationToken = default)
        => await _context.Donations.AddAsync(donation, cancellationToken);
}
