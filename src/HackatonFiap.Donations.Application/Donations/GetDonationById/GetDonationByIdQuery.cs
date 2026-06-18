using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.Errors;
using HackatonFiap.Donations.Domain.Common;

namespace HackatonFiap.Donations.Application.Donations.GetDonationById;

public sealed record GetDonationByIdQuery(Guid Id, Guid RequestingDonorId);

public sealed class GetDonationByIdQueryHandler
{
    private readonly IDonationRepository _repository;

    public GetDonationByIdQueryHandler(IDonationRepository repository) => _repository = repository;

    public async Task<Result<DonationResponse>> Handle(GetDonationByIdQuery query, CancellationToken cancellationToken)
    {
        var donation = await _repository.GetByIdAsync(query.Id, cancellationToken);
        if (donation is null || donation.DonorId != query.RequestingDonorId)
        {
            return Result.Failure<DonationResponse>(DonationErrors.NotFound);
        }

        return Result.Success(new DonationResponse(
            donation.Id, donation.CampaignId, donation.Amount, donation.Method.ToString(),
            donation.Status.ToString(), donation.DeclineReason, donation.CreatedAt, donation.ProcessedAt));
    }
}
