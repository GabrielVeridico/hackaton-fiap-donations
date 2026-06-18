using HackatonFiap.Donations.Domain.Entities;

namespace HackatonFiap.Donations.Application.Abstractions;

public interface IProcessedEventStore
{
    Task<bool> ExistsAsync(Guid donationId, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default);
}
