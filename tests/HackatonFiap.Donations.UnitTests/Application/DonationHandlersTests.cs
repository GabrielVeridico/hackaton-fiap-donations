using FluentAssertions;
using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.Donations.CreateDonation;
using HackatonFiap.Donations.Application.Donations.GetDonationById;
using HackatonFiap.Donations.Application.Donations.ListMyDonations;
using HackatonFiap.Donations.Application.IntegrationEvents;
using HackatonFiap.Donations.Domain.Entities;
using HackatonFiap.Donations.Domain.Enums;
using NSubstitute;
using Xunit;

namespace HackatonFiap.Donations.UnitTests.Application;

public class DonationHandlersTests
{
    private static readonly DateTime Now = new(2026, 06, 18, 12, 0, 0, DateTimeKind.Utc);

    private readonly ICampaignRepository _campaigns = Substitute.For<ICampaignRepository>();
    private readonly IDonationRepository _donations = Substitute.For<IDonationRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public DonationHandlersTests() => _clock.UtcNow.Returns(Now);

    private CreateDonationCommandHandler CreateHandler()
        => new(_campaigns, _donations, _uow, _publisher, _clock);

    private static Campaign ActiveCampaign()
        => Campaign.Create("t", "d", Now.AddDays(-1), Now.AddDays(10), 1000m, Guid.NewGuid(), Now).Value;

    private static CreateDonationCommand Command(Guid campaignId, decimal amount = 50m)
        => new(campaignId, amount, PaymentMethod.Pix, Guid.NewGuid(), "donor@example.com", "Donor");

    [Fact]
    public async Task Create_in_active_campaign_persists_and_publishes_event()
    {
        var campaign = ActiveCampaign();
        _campaigns.GetByIdAsync(campaign.Id).Returns(campaign);

        var result = await CreateHandler().Handle(Command(campaign.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _donations.Received(1).AddAsync(Arg.Is<Donation>(d => d.Status == DonationStatus.Pending), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(
            Arg.Is<DonationRequestedEvent>(e => e.CampaignId == campaign.Id && e.PaymentMethod == PaymentMethod.Pix),
            "DonationRequested",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_for_missing_campaign_returns_422_and_publishes_nothing()
    {
        _campaigns.GetByIdAsync(Arg.Any<Guid>()).Returns((Campaign?)null);

        var result = await CreateHandler().Handle(Command(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Donation.CampaignNotFound");
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<DonationRequestedEvent>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_in_cancelled_campaign_returns_not_active()
    {
        var campaign = ActiveCampaign();
        campaign.Cancel();
        _campaigns.GetByIdAsync(campaign.Id).Returns(campaign);

        var result = await CreateHandler().Handle(Command(campaign.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Donation.CampaignNotActive");
    }

    [Fact]
    public async Task Create_outside_period_returns_outside_period()
    {
        // Campanha ativa, mas o período já terminou (janela do job de expiração — RN04.11 / RN06.3).
        var campaign = Campaign.Create("t", "d", Now.AddDays(-10), Now.AddDays(-1), 1000m, Guid.NewGuid(), Now.AddDays(-11)).Value;
        _campaigns.GetByIdAsync(campaign.Id).Returns(campaign);

        var result = await CreateHandler().Handle(Command(campaign.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Donation.OutsidePeriod");
    }

    [Fact]
    public async Task GetById_returns_donation_of_owner()
    {
        var donation = Donation.Create(Guid.NewGuid(), 50m, PaymentMethod.Pix, Guid.NewGuid(), "d@e.com", "D").Value;
        _donations.GetByIdAsync(donation.Id).Returns(donation);
        var handler = new GetDonationByIdQueryHandler(_donations);

        var result = await handler.Handle(new GetDonationByIdQuery(donation.Id, donation.DonorId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(donation.Id);
    }

    [Fact]
    public async Task GetById_for_other_donor_returns_not_found()
    {
        var donation = Donation.Create(Guid.NewGuid(), 50m, PaymentMethod.Pix, Guid.NewGuid(), "d@e.com", "D").Value;
        _donations.GetByIdAsync(donation.Id).Returns(donation);
        var handler = new GetDonationByIdQueryHandler(_donations);

        var result = await handler.Handle(new GetDonationByIdQuery(donation.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Donation.NotFound");
    }

    [Fact]
    public async Task ListMine_returns_requesting_donor_donations()
    {
        var donorId = Guid.NewGuid();
        var mine = Donation.Create(Guid.NewGuid(), 50m, PaymentMethod.Pix, donorId, "d@e.com", "D").Value;
        _donations.ListByDonorAsync(donorId).Returns(new[] { mine });
        var handler = new ListMyDonationsQueryHandler(_donations);

        var result = await handler.Handle(new ListMyDonationsQuery(donorId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Id.Should().Be(mine.Id);
    }
}
