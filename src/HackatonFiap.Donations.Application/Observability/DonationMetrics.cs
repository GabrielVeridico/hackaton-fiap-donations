using System.Diagnostics.Metrics;

namespace HackatonFiap.Donations.Application.Observability;

public static class DonationMetrics
{
    public const string ServiceName = "HackatonFiap.Donations";

    private static readonly Meter Meter = new(ServiceName);

    public static readonly Counter<long> DonationsReceived =
        Meter.CreateCounter<long>("donations_received_total", description: "Total de intenções de doação recebidas.");
    public static readonly Counter<long> DonationsApproved =
        Meter.CreateCounter<long>("donations_approved_total", description: "Total de doações aprovadas/consolidadas.");
    public static readonly Counter<long> DonationsDeclined =
        Meter.CreateCounter<long>("donations_declined_total", description: "Total de doações recusadas.");
    public static readonly Counter<long> CampaignsCompleted =
        Meter.CreateCounter<long>("campaigns_completed_total", description: "Total de campanhas concluídas.");
    public static readonly Counter<double> AmountRaised =
        Meter.CreateCounter<double>("amount_raised_total", description: "Valor total consolidado nas campanhas.");
}
