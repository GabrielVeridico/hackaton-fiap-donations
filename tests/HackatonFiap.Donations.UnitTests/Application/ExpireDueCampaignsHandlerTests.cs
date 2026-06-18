using FluentAssertions;
using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.Campaigns.ExpireDueCampaigns;
using HackatonFiap.Donations.Application.ReadModels;
using HackatonFiap.Donations.Domain.Entities;
using HackatonFiap.Donations.Domain.Enums;
using NSubstitute;
using Xunit;

namespace HackatonFiap.Donations.UnitTests.Application;

public class ExpireDueCampaignsHandlerTests
{
    private static readonly DateTime Now = new(2026, 06, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Expires_active_campaigns_past_end_date_and_projects()
    {
        var repository = Substitute.For<ICampaignRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var readStore = Substitute.For<ICampaignReadStore>();
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);

        var expired = Campaign.Create("t", "d", Now.AddDays(-30), Now.AddDays(-1), 1000m, Guid.NewGuid(), Now.AddDays(-31)).Value;
        repository.ListActiveExpiredAsync(Now).Returns(new[] { expired });

        var handler = new ExpireDueCampaignsCommandHandler(repository, uow, readStore, clock);
        await handler.Handle(CancellationToken.None);

        expired.Status.Should().Be(CampaignStatus.Completed);
        expired.CompletionReason.Should().Be(CompletionReason.Expired);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await readStore.Received(1).UpsertAsync(Arg.Any<CampaignReadModel>(), Arg.Any<CancellationToken>());
    }
}
