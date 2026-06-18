using FluentAssertions;
using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.Campaigns;
using HackatonFiap.Donations.Application.Campaigns.ChangeCampaignStatus;
using HackatonFiap.Donations.Application.Campaigns.CreateCampaign;
using HackatonFiap.Donations.Application.ReadModels;
using HackatonFiap.Donations.Domain.Entities;
using HackatonFiap.Donations.Domain.Enums;
using NSubstitute;
using Xunit;

namespace HackatonFiap.Donations.UnitTests.Application;

public class CampaignHandlersTests
{
    private static readonly DateTime Now = new(2026, 06, 18, 12, 0, 0, DateTimeKind.Utc);

    private readonly ICampaignRepository _repository = Substitute.For<ICampaignRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICampaignReadStore _readStore = Substitute.For<ICampaignReadStore>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CampaignHandlersTests() => _clock.UtcNow.Returns(Now);

    [Fact]
    public async Task Create_persists_and_projects_to_read_store()
    {
        var handler = new CreateCampaignCommandHandler(_repository, _uow, _readStore, _clock);
        var command = new CreateCampaignCommand("Inverno", "Agasalhos", Now, Now.AddDays(30), 1000m, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Campaign>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _readStore.Received(1).UpsertAsync(
            Arg.Is<CampaignReadModel>(c => c.Status == "Active" && c.Goal == 1000m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_with_invalid_goal_fails_without_persisting()
    {
        var handler = new CreateCampaignCommandHandler(_repository, _uow, _readStore, _clock);
        var command = new CreateCampaignCommand("Inverno", "Agasalhos", Now, Now.AddDays(30), 0m, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Campaign.GoalMustBePositive");
        await _repository.DidNotReceive().AddAsync(Arg.Any<Campaign>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatus_close_completes_with_manually_closed()
    {
        var campaign = Campaign.Create("t", "d", Now.AddDays(-1), Now.AddDays(10), 100m, Guid.NewGuid(), Now).Value;
        _repository.GetByIdAsync(campaign.Id).Returns(campaign);
        var handler = new ChangeCampaignStatusCommandHandler(_repository, _uow, _readStore);

        var result = await handler.Handle(new ChangeCampaignStatusCommand(campaign.Id, CampaignStatusAction.Close), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        campaign.Status.Should().Be(CampaignStatus.Completed);
        campaign.CompletionReason.Should().Be(CompletionReason.ManuallyClosed);
        await _readStore.Received(1).UpsertAsync(Arg.Any<CampaignReadModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatus_when_not_found_returns_not_found()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((Campaign?)null);
        var handler = new ChangeCampaignStatusCommandHandler(_repository, _uow, _readStore);

        var result = await handler.Handle(new ChangeCampaignStatusCommand(Guid.NewGuid(), CampaignStatusAction.Cancel), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Campaign.NotFound");
    }
}
