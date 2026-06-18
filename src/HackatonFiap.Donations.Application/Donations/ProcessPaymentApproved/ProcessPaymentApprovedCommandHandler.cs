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

        // Só consolida em campanha ATIVA. Se ela encerrou (cancelada/expirada) entre o request e
        // o resultado do pagamento, a doação fica aprovada (o pagamento ocorreu), mas o valor NÃO
        // é somado a uma campanha terminal (invariante: estados terminais não mudam).
        var consolidated = campaign is not null && campaign.Status == CampaignStatus.Active;
        if (consolidated)
        {
            campaign!.AddRaised(donation.Amount);
            if (campaign.AmountRaised >= campaign.Goal)
            {
                campaign.Complete(CompletionReason.GoalReached);
                DonationMetrics.CampaignsCompleted.Add(1);
            }
        }

        await _processed.AddAsync(new ProcessedEvent(command.DonationId, _clock.UtcNow), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (consolidated)
        {
            await _readStore.UpsertAsync(campaign!.ToReadModel(), cancellationToken);
            DonationMetrics.AmountRaised.Add((double)donation.Amount);
        }

        DonationMetrics.DonationsApproved.Add(1);
        return Result.Success();
    }
}
