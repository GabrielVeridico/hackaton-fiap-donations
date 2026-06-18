using System.Text.Json;
using Azure.Messaging.ServiceBus;
using HackatonFiap.Donations.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace HackatonFiap.Donations.Infrastructure.Messaging;

public sealed class ServiceBusEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusEventPublisher> _logger;

    public ServiceBusEventPublisher(ServiceBusClient client, string topicName, ILogger<ServiceBusEventPublisher> logger)
    {
        _sender = client.CreateSender(topicName);
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, string subject, CancellationToken cancellationToken = default)
        where TEvent : notnull
    {
        var body = JsonSerializer.Serialize(integrationEvent, MessagingJson.Options);
        var message = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            Subject = subject
        };

        await _sender.SendMessageAsync(message, cancellationToken);
        _logger.LogInformation("Evento publicado. Subject={Subject}", subject);
    }

    public ValueTask DisposeAsync() => _sender.DisposeAsync();
}
