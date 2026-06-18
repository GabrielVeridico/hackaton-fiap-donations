using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.Campaigns;
using HackatonFiap.Donations.Application.Errors;
using HackatonFiap.Donations.Domain.Common;
using HackatonFiap.Donations.Domain.Enums;

namespace HackatonFiap.Donations.Application.Campaigns.ChangeCampaignStatus;

public sealed class ChangeCampaignStatusCommandHandler
{
    private readonly ICampaignRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICampaignReadStore _readStore;

    public ChangeCampaignStatusCommandHandler(ICampaignRepository repository, IUnitOfWork unitOfWork, ICampaignReadStore readStore)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _readStore = readStore;
    }

    public async Task<Result> Handle(ChangeCampaignStatusCommand command, CancellationToken cancellationToken)
    {
        var campaign = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (campaign is null)
        {
            return Result.Failure(CampaignErrors.NotFound);
        }

        var transition = command.Action == CampaignStatusAction.Close
            ? campaign.Complete(CompletionReason.ManuallyClosed)
            : campaign.Cancel();

        if (transition.IsFailure)
        {
            return transition;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _readStore.UpsertAsync(campaign.ToReadModel(), cancellationToken);
        return Result.Success();
    }
}
