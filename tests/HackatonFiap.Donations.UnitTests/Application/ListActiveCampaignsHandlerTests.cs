using FluentAssertions;
using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.ReadModels;
using HackatonFiap.Donations.Application.Transparency;
using NSubstitute;
using Xunit;

namespace HackatonFiap.Donations.UnitTests.Application;

public class ListActiveCampaignsHandlerTests
{
    [Fact]
    public async Task Maps_read_models_and_computes_percentual()
    {
        var readStore = Substitute.For<ICampaignReadStore>();
        readStore.ListActiveAsync().Returns(new[]
        {
            new CampaignReadModel(Guid.NewGuid(), "Inverno", 1000m, 250m, "Active")
        });
        var handler = new ListActiveCampaignsQueryHandler(readStore);

        var result = await handler.Handle(new ListActiveCampaignsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Title.Should().Be("Inverno");
        result.Value[0].Percentual.Should().Be(25m);
    }
}
