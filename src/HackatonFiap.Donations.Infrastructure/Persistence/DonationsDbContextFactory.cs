using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HackatonFiap.Donations.Infrastructure.Persistence;

public sealed class DonationsDbContextFactory : IDesignTimeDbContextFactory<DonationsDbContext>
{
    public DonationsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DonationsDbContext>()
            .UseSqlServer("Server=localhost;Database=HackatonFiapDonationsDb;Trusted_Connection=True;TrustServerCertificate=true;")
            .Options;

        return new DonationsDbContext(options);
    }
}
