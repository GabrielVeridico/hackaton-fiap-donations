using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.Campaigns;
using HackatonFiap.Donations.Application.Observability;
using HackatonFiap.Donations.Domain.Enums;

namespace HackatonFiap.Donations.Application.Campaigns.ExpireDueCampaigns;

public sealed class ExpireDueCampaignsCommandHandler
{
    private readonly ICampaignRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICampaignReadStore _readStore;
    private readonly IClock _clock;

    public ExpireDueCampaignsCommandHandler(ICampaignRepository repository, IUnitOfWork unitOfWork,
        ICampaignReadStore readStore, IClock clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _readStore = readStore;
        _clock = clock;
    }

    public async Task Handle(CancellationToken cancellationToken)
    {
        var due = await _repository.ListActiveExpiredAsync(_clock.UtcNow, cancellationToken);
        if (due.Count == 0)
        {
            return;
        }

        foreach (var campaign in due)
        {
            var transition = campaign.Complete(CompletionReason.Expired);
            if (transition.IsSuccess)
            {
                DonationMetrics.CampaignsCompleted.Add(1);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var campaign in due)
        {
            await _readStore.UpsertAsync(campaign.ToReadModel(), cancellationToken);
        }
    }
}
