using FluentAssertions;
using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.Donations.ProcessPaymentApproved;
using HackatonFiap.Donations.Application.Donations.ProcessPaymentDeclined;
using HackatonFiap.Donations.Application.ReadModels;
using HackatonFiap.Donations.Domain.Entities;
using HackatonFiap.Donations.Domain.Enums;
using NSubstitute;
using Xunit;

namespace HackatonFiap.Donations.UnitTests.Application;

public class PaymentResultHandlersTests
{
    private static readonly DateTime Now = new(2026, 06, 18, 12, 0, 0, DateTimeKind.Utc);

    private readonly IDonationRepository _donations = Substitute.For<IDonationRepository>();
    private readonly ICampaignRepository _campaigns = Substitute.For<ICampaignRepository>();
    private readonly IProcessedEventStore _processed = Substitute.For<IProcessedEventStore>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICampaignReadStore _readStore = Substitute.For<ICampaignReadStore>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public PaymentResultHandlersTests() => _clock.UtcNow.Returns(Now);

    private ProcessPaymentApprovedCommandHandler ApprovedHandler()
        => new(_donations, _campaigns, _processed, _uow, _readStore, _clock);

    private ProcessPaymentDeclinedCommandHandler DeclinedHandler()
        => new(_donations, _processed, _uow, _clock);

    private static Campaign CampaignWithGoal(decimal goal)
        => Campaign.Create("t", "d", Now.AddDays(-1), Now.AddDays(10), goal, Guid.NewGuid(), Now).Value;

    private static Donation PendingDonation(Guid campaignId, decimal amount)
        => Donation.Create(campaignId, amount, PaymentMethod.Pix, Guid.NewGuid(), "d@e.com", "D").Value;

    [Fact]
    public async Task Approved_consolidates_amount_and_projects()
    {
        var campaign = CampaignWithGoal(1000m);
        var donation = PendingDonation(campaign.Id, 200m);
        _processed.ExistsAsync(donation.Id).Returns(false);
        _donations.GetByIdAsync(donation.Id).Returns(donation);
        _campaigns.GetByIdAsync(campaign.Id).Returns(campaign);

        var command = new ProcessPaymentApprovedCommand(donation.Id, campaign.Id, 200m, Guid.NewGuid(), donation.DonorId, "d@e.com", "D");
        var result = await ApprovedHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        donation.Status.Should().Be(DonationStatus.Approved);
        campaign.AmountRaised.Should().Be(200m);
        campaign.Status.Should().Be(CampaignStatus.Active);
        await _processed.Received(1).AddAsync(Arg.Any<ProcessedEvent>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _readStore.Received(1).UpsertAsync(Arg.Is<CampaignReadModel>(c => c.AmountRaised == 200m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approved_reaching_goal_completes_campaign()
    {
        var campaign = CampaignWithGoal(150m);
        var donation = PendingDonation(campaign.Id, 200m);
        _processed.ExistsAsync(donation.Id).Returns(false);
        _donations.GetByIdAsync(donation.Id).Returns(donation);
        _campaigns.GetByIdAsync(campaign.Id).Returns(campaign);

        var command = new ProcessPaymentApprovedCommand(donation.Id, campaign.Id, 200m, Guid.NewGuid(), donation.DonorId, "d@e.com", "D");
        var result = await ApprovedHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        campaign.Status.Should().Be(CampaignStatus.Completed);
        campaign.CompletionReason.Should().Be(CompletionReason.GoalReached);
    }

    [Fact]
    public async Task Approved_is_idempotent_when_already_processed()
    {
        var donationId = Guid.NewGuid();
        _processed.ExistsAsync(donationId).Returns(true);

        var command = new ProcessPaymentApprovedCommand(donationId, Guid.NewGuid(), 200m, Guid.NewGuid(), Guid.NewGuid(), "d@e.com", "D");
        var result = await ApprovedHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _donations.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Declined_marks_donation_declined_without_consolidating()
    {
        var donation = PendingDonation(Guid.NewGuid(), 99.99m);
        _processed.ExistsAsync(donation.Id).Returns(false);
        _donations.GetByIdAsync(donation.Id).Returns(donation);

        var command = new ProcessPaymentDeclinedCommand(donation.Id, donation.CampaignId, "centavos ,99", 99.99m, donation.DonorId, "d@e.com", "D");
        var result = await DeclinedHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        donation.Status.Should().Be(DonationStatus.Declined);
        await _processed.Received(1).AddAsync(Arg.Any<ProcessedEvent>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
