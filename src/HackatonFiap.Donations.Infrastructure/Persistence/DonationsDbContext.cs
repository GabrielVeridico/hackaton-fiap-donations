using HackatonFiap.Donations.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HackatonFiap.Donations.Infrastructure.Persistence;

public class DonationsDbContext : DbContext
{
    public DonationsDbContext(DbContextOptions<DonationsDbContext> options) : base(options) { }

    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DonationsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
