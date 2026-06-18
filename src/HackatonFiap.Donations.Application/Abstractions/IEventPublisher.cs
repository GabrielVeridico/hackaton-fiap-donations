namespace HackatonFiap.Donations.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, string subject, CancellationToken cancellationToken = default)
        where TEvent : notnull;
}
