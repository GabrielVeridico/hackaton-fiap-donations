using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.ReadModels;
using HackatonFiap.Donations.Domain.Common;
using HackatonFiap.Donations.Domain.Entities;

namespace HackatonFiap.Donations.Application.Campaigns.CreateCampaign;

public sealed class CreateCampaignCommandHandler
{
    private readonly ICampaignRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICampaignReadStore _readStore;
    private readonly IClock _clock;

    public CreateCampaignCommandHandler(ICampaignRepository repository, IUnitOfWork unitOfWork,
        ICampaignReadStore readStore, IClock clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _readStore = readStore;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CreateCampaignCommand command, CancellationToken cancellationToken)
    {
        var creation = Campaign.Create(command.Title, command.Description, command.StartDate, command.EndDate,
            command.Goal, command.CreatedById, _clock.UtcNow);
        if (creation.IsFailure)
        {
            return Result.Failure<Guid>(creation.Error);
        }

        var campaign = creation.Value;
        await _repository.AddAsync(campaign, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _readStore.UpsertAsync(campaign.ToReadModel(), cancellationToken);

        return Result.Success(campaign.Id);
    }
}
