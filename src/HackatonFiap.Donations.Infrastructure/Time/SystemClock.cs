using HackatonFiap.Donations.Application.Abstractions;

namespace HackatonFiap.Donations.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
