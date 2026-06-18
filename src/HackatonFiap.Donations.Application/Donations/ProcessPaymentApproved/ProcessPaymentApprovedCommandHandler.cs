using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.Campaigns;
using HackatonFiap.Donations.Application.Errors;
using HackatonFiap.Donations.Application.Observability;
using HackatonFiap.Donations.Domain.Common;
using HackatonFiap.Donations.Domain.Entities;
using HackatonFiap.Donations.Domain.Enums;

namespace HackatonFiap.Donations.Application.Donations.ProcessPaymentApproved;

public sealed class ProcessPaymentApprovedCommandHandler
{
    private readonly IDonationRepository _donations;
    private readonly ICampaignRepository _campaigns;
    private readonly IProcessedEventStore _processed;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICampaignReadStore _readStore;
    private readonly IClock _clock;

    public ProcessPaymentApprovedCommandHandler(IDonationRepository donations, ICampaignRepository campaigns,
        IProcessedEventStore processed, IUnitOfWork unitOfWork, ICampaignReadStore readStore, IClock clock)
    {
        _donations = donations;
        _campaigns = campaigns;
        _processed = processed;
        _unitOfWork = unitOfWork;
        _readStore = readStore;
        _clock = clock;
    }

    public async Task<Result> Handle(ProcessPaymentApprovedCommand command, CancellationToken cancellationToken)
    {
        if (await _processed.ExistsAsync(command.DonationId, cancellationToken))
        {
            return Result.Success(); // idempotência (RN06.10)
        }

        var donation = await _donations.GetByIdAsync(command.DonationId, cancellationToken);
        if (donation is null)
        {
            return Result.Failure(DonationErrors.NotFound); // doação ainda não persistida -> abandona p/ reentrega
        }

        donation.Approve();

        // Fonte de verdade: a doação persistida (campanha e valor validados por nós no POST),
        // não os campos do evento recebido — evita creditar campanha/valor divergentes.
        Campaign? campaign = await _campaigns.GetByIdAsync(donation.CampaignId, cancellationToken);
        if (campaign is not null)
        {
            campaign.AddRaised(donation.Amount);
            if (campaign.Status == CampaignStatus.Active && campaign.AmountRaised >= campaign.Goal)
            {
                campaign.Complete(CompletionReason.GoalReached);
                DonationMetrics.CampaignsCompleted.Add(1);
            }
        }

        await _processed.AddAsync(new ProcessedEvent(command.DonationId, _clock.UtcNow), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (campaign is not null)
        {
            await _readStore.UpsertAsync(campaign.ToReadModel(), cancellationToken);
        }

        DonationMetrics.DonationsApproved.Add(1);
        DonationMetrics.AmountRaised.Add((double)donation.Amount);
        return Result.Success();
    }
}
