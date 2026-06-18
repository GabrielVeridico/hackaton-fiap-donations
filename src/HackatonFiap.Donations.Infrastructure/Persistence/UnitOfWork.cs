using HackatonFiap.Donations.Application.Abstractions;

namespace HackatonFiap.Donations.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DonationsDbContext _context;

    public UnitOfWork(DonationsDbContext context) => _context = context;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
