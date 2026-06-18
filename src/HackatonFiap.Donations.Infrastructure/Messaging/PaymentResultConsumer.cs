using System.Text.Json;
using Azure.Messaging.ServiceBus;
using HackatonFiap.Donations.Application.Donations.ProcessPaymentApproved;
using HackatonFiap.Donations.Application.Donations.ProcessPaymentDeclined;
using HackatonFiap.Donations.Application.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HackatonFiap.Donations.Infrastructure.Messaging;

public sealed class PaymentResultConsumer : BackgroundService
{
    public const string ApprovedSubject = "PaymentApproved";
    public const string DeclinedSubject = "PaymentDeclined";

    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentResultConsumer> _logger;

    public PaymentResultConsumer(ServiceBusClient client, string resultTopicName, string subscriptionName,
        IServiceScopeFactory scopeFactory, ILogger<PaymentResultConsumer> logger)
    {
        _processor = client.CreateProcessor(resultTopicName, subscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += OnMessageAsync;
        _processor.ProcessErrorAsync += OnErrorAsync;
        await _processor.StartProcessingAsync(stoppingToken);
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        var subject = args.Message.Subject;
        var body = args.Message.Body.ToString();

        try
        {
            using var scope = _scopeFactory.CreateScope();

            if (subject == ApprovedSubject)
            {
                var evt = JsonSerializer.Deserialize<PaymentApprovedEvent>(body, MessagingJson.Options);
                if (evt is null)
                {
                    await args.DeadLetterMessageAsync(args.Message, "InvalidPayload", "PaymentApprovedEvent nulo.");
                    return;
                }

                var handler = scope.ServiceProvider.GetRequiredService<ProcessPaymentApprovedCommandHandler>();
                var result = await handler.Handle(new ProcessPaymentApprovedCommand(
                    evt.DonationId, evt.CampaignId, evt.Amount, evt.PaymentId, evt.DonorId, evt.DonorEmail, evt.DonorName),
                    args.CancellationToken);

                if (result.IsFailure)
                {
                    _logger.LogWarning("Falha ao consolidar doação {DonationId}: {Error}. Abandonando p/ reentrega.",
                        evt.DonationId, result.Error.Message);
                    await args.AbandonMessageAsync(args.Message);
                    return;
                }
            }
            else if (subject == DeclinedSubject)
            {
                var evt = JsonSerializer.Deserialize<PaymentDeclinedEvent>(body, MessagingJson.Options);
                if (evt is null)
                {
                    await args.DeadLetterMessageAsync(args.Message, "InvalidPayload", "PaymentDeclinedEvent nulo.");
                    return;
                }

                var handler = scope.ServiceProvider.GetRequiredService<ProcessPaymentDeclinedCommandHandler>();
                var result = await handler.Handle(new ProcessPaymentDeclinedCommand(
                    evt.DonationId, evt.CampaignId, evt.Reason, evt.Amount, evt.DonorId, evt.DonorEmail, evt.DonorName),
                    args.CancellationToken);

                if (result.IsFailure)
                {
                    _logger.LogWarning("Falha ao recusar doação {DonationId}: {Error}. Abandonando p/ reentrega.",
                        evt.DonationId, result.Error.Message);
                    await args.AbandonMessageAsync(args.Message);
                    return;
                }
            }
            else
            {
                _logger.LogWarning("Subject desconhecido '{Subject}' — dead-letter.", subject);
                await args.DeadLetterMessageAsync(args.Message, "UnknownSubject", subject);
                return;
            }

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar resultado de pagamento; abandonando para reentrega.");
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Erro no ServiceBusProcessor. Source={Source}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        await _processor.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
