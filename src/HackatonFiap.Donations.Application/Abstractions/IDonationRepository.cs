using HackatonFiap.Donations.Domain.Entities;

namespace HackatonFiap.Donations.Application.Abstractions;

public interface IDonationRepository
{
    Task<Donation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Donation donation, CancellationToken cancellationToken = default);
}
