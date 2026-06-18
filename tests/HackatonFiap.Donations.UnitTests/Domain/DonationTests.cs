using FluentAssertions;
using HackatonFiap.Donations.Domain.Entities;
using HackatonFiap.Donations.Domain.Enums;
using Xunit;

namespace HackatonFiap.Donations.UnitTests.Domain;

public class DonationTests
{
    private static Donation NewPending(decimal amount = 50m)
        => Donation.Create(Guid.NewGuid(), amount, PaymentMethod.Pix, Guid.NewGuid(), "donor@example.com", "Donor").Value;

    [Fact]
    public void Create_starts_pending()
    {
        var donation = NewPending();

        donation.Status.Should().Be(DonationStatus.Pending);
        donation.Id.Should().NotBe(Guid.Empty);
        donation.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void Create_with_non_positive_amount_fails()
    {
        var result = Donation.Create(Guid.NewGuid(), 0m, PaymentMethod.Pix, Guid.NewGuid(), "d@e.com", "D");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Donation.AmountMustBePositive");
    }

    [Fact]
    public void Approve_sets_status_and_processed_at()
    {
        var donation = NewPending();

        donation.Approve();

        donation.Status.Should().Be(DonationStatus.Approved);
        donation.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Decline_sets_status_and_reason()
    {
        var donation = NewPending();

        donation.Decline("centavos ,99");

        donation.Status.Should().Be(DonationStatus.Declined);
        donation.DeclineReason.Should().Be("centavos ,99");
        donation.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Approve_when_not_pending_throws()
    {
        var donation = NewPending();
        donation.Approve();

        var act = () => donation.Approve();

        act.Should().Throw<InvalidOperationException>();
    }
}
