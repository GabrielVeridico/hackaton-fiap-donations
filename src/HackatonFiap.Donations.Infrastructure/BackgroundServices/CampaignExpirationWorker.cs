using HackatonFiap.Donations.Application.Campaigns.ExpireDueCampaigns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HackatonFiap.Donations.Infrastructure.BackgroundServices;

public sealed class CampaignExpirationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CampaignExpirationWorker> _logger;
    private readonly TimeSpan _interval;

    public CampaignExpirationWorker(IServiceScopeFactory scopeFactory, ILogger<CampaignExpirationWorker> logger, TimeSpan interval)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ExpireDueCampaignsCommandHandler>();
                await handler.Handle(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao expirar campanhas vencidas.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
