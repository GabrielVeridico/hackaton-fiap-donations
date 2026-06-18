using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.Campaigns;
using HackatonFiap.Donations.Application.Errors;
using HackatonFiap.Donations.Domain.Common;

namespace HackatonFiap.Donations.Application.Campaigns.UpdateCampaign;

public sealed class UpdateCampaignCommandHandler
{
    private readonly ICampaignRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICampaignReadStore _readStore;
    private readonly IClock _clock;

    public UpdateCampaignCommandHandler(ICampaignRepository repository, IUnitOfWork unitOfWork,
        ICampaignReadStore readStore, IClock clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _readStore = readStore;
        _clock = clock;
    }

    public async Task<Result> Handle(UpdateCampaignCommand command, CancellationToken cancellationToken)
    {
        var campaign = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (campaign is null)
        {
            return Result.Failure(CampaignErrors.NotFound);
        }

        var update = campaign.Update(command.Title, command.Description, command.StartDate, command.EndDate,
            command.Goal, _clock.UtcNow);
        if (update.IsFailure)
        {
            return update;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _readStore.UpsertAsync(campaign.ToReadModel(), cancellationToken);
        return Result.Success();
    }
}
