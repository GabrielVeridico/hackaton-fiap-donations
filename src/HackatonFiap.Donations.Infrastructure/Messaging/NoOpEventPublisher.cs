using HackatonFiap.Donations.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace HackatonFiap.Donations.Infrastructure.Messaging;

public sealed class NoOpEventPublisher : IEventPublisher
{
    private readonly ILogger<NoOpEventPublisher> _logger;

    public NoOpEventPublisher(ILogger<NoOpEventPublisher> logger) => _logger = logger;

    public Task PublishAsync<TEvent>(TEvent integrationEvent, string subject, CancellationToken cancellationToken = default)
        where TEvent : notnull
    {
        _logger.LogWarning("ServiceBus não configurado — evento {Subject} NÃO publicado (NoOp).", subject);
        return Task.CompletedTask;
    }
}
