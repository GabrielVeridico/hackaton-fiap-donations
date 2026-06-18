using HackatonFiap.Donations.Domain.Entities;

namespace HackatonFiap.Donations.Application.Abstractions;

/// <summary>
/// Inbox de idempotência do consumer (RN06.10). Implementações DEVEM garantir unicidade de
/// <c>DonationId</c> (chave primária/índice único), de modo que duas consolidações concorrentes
/// da mesma doação não persistam ambas: a segunda falha no SaveChanges da transação única
/// (Donation + Campaign + ProcessedEvent), faz rollback atômico e converge para no-op na reentrega.
/// </summary>
public interface IProcessedEventStore
{
    Task<bool> ExistsAsync(Guid donationId, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default);
}
