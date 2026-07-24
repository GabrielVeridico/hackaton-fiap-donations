using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Domain.Common;

namespace HackatonFiap.Donations.Application.Donations.ListMyDonations;

public sealed record ListMyDonationsQuery(Guid DonorId);

public sealed class ListMyDonationsQueryHandler
{
    private readonly IDonationRepository _repository;

    public ListMyDonationsQueryHandler(IDonationRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyList<DonationResponse>>> Handle(
        ListMyDonationsQuery query, CancellationToken cancellationToken)
    {
        var donations = await _repository.ListByDonorAsync(query.DonorId, cancellationToken);
        IReadOnlyList<DonationResponse> responses = donations
            .Select(d => new DonationResponse(
                d.Id, d.CampaignId, d.Amount, d.Method.ToString(),
                d.Status.ToString(), d.DeclineReason, d.CreatedAt, d.ProcessedAt))
            .ToList();

        return Result.Success(responses);
    }
}
