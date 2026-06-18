using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HackatonFiap.Donations.Infrastructure.Persistence.Repositories;

public sealed class ProcessedEventStore : IProcessedEventStore
{
    private readonly DonationsDbContext _context;

    public ProcessedEventStore(DonationsDbContext context) => _context = context;

    public Task<bool> ExistsAsync(Guid donationId, CancellationToken cancellationToken = default)
        => _context.ProcessedEvents.AsNoTracking().AnyAsync(p => p.DonationId == donationId, cancellationToken);

    public async Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default)
        => await _context.ProcessedEvents.AddAsync(processedEvent, cancellationToken);
}
