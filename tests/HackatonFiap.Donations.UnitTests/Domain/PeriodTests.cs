using FluentAssertions;
using HackatonFiap.Donations.Domain.ValueObjects;

namespace HackatonFiap.Donations.UnitTests.Domain;

public class PeriodTests
{
    [Fact]
    public void Create_with_end_after_start_succeeds()
    {
        var start = new DateTime(2026, 06, 01, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 07, 01, 0, 0, 0, DateTimeKind.Utc);

        var result = Period.Create(start, end);

        result.IsSuccess.Should().BeTrue();
        result.Value.StartDate.Should().Be(start);
        result.Value.EndDate.Should().Be(end);
    }

    [Fact]
    public void Create_with_end_before_start_fails()
    {
        var start = new DateTime(2026, 07, 01, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 06, 01, 0, 0, 0, DateTimeKind.Utc);

        var result = Period.Create(start, end);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Period.EndBeforeStart");
    }
}
