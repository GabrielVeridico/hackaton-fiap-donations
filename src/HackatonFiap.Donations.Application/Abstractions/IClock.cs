namespace HackatonFiap.Donations.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
