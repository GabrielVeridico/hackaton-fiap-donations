using FluentAssertions;
using HackatonFiap.Donations.Domain.Entities;
using HackatonFiap.Donations.Domain.Enums;
using Xunit;

namespace HackatonFiap.Donations.UnitTests.Domain;

public class CampaignTests
{
    private static readonly DateTime Now = new(2026, 06, 18, 12, 0, 0, DateTimeKind.Utc);

    private static Campaign NewActive(decimal goal = 1000m)
        => Campaign.Create("Inverno Solidário", "Agasalhos", Now.AddDays(-1), Now.AddDays(30), goal, Guid.NewGuid(), Now).Value;

    [Fact]
    public void Create_starts_active_with_zero_raised()
    {
        var campaign = NewActive();

        campaign.Status.Should().Be(CampaignStatus.Active);
        campaign.AmountRaised.Should().Be(0m);
        campaign.CompletionReason.Should().BeNull();
        campaign.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_with_non_positive_goal_fails()
    {
        var result = Campaign.Create("t", "d", Now, Now.AddDays(10), 0m, Guid.NewGuid(), Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Campaign.GoalMustBePositive");
    }

    [Fact]
    public void Create_with_end_date_in_past_fails()
    {
        var result = Campaign.Create("t", "d", Now.AddDays(-10), Now.AddDays(-1), 100m, Guid.NewGuid(), Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Campaign.EndDateInPast");
    }

    [Fact]
    public void Create_with_blank_title_fails()
    {
        var result = Campaign.Create("  ", "d", Now, Now.AddDays(10), 100m, Guid.NewGuid(), Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Campaign.TitleRequired");
    }

    [Fact]
    public void AddRaised_increments_amount()
    {
        var campaign = NewActive();

        campaign.AddRaised(250m);
        campaign.AddRaised(100m);

        campaign.AmountRaised.Should().Be(350m);
    }

    [Fact]
    public void Complete_from_active_sets_reason()
    {
        var campaign = NewActive();

        var result = campaign.Complete(CompletionReason.GoalReached);

        result.IsSuccess.Should().BeTrue();
        campaign.Status.Should().Be(CampaignStatus.Completed);
        campaign.CompletionReason.Should().Be(CompletionReason.GoalReached);
    }

    [Fact]
    public void Complete_when_terminal_fails()
    {
        var campaign = NewActive();
        campaign.Cancel();

        var result = campaign.Complete(CompletionReason.Expired);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Campaign.InvalidStatusTransition");
    }

    [Fact]
    public void Cancel_from_active_succeeds()
    {
        var campaign = NewActive();

        var result = campaign.Cancel();

        result.IsSuccess.Should().BeTrue();
        campaign.Status.Should().Be(CampaignStatus.Cancelled);
    }
}
