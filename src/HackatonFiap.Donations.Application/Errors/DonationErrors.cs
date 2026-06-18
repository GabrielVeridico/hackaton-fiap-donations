using HackatonFiap.Donations.Domain.Common;

namespace HackatonFiap.Donations.Application.Errors;

public static class DonationErrors
{
    public static readonly Error CampaignNotFound =
        new("Donation.CampaignNotFound", "Campanha informada não existe.");
    public static readonly Error CampaignNotActive =
        new("Donation.CampaignNotActive", "A campanha não está ativa.");
    public static readonly Error OutsidePeriod =
        new("Donation.OutsidePeriod", "A doação está fora do período da campanha.");
    public static readonly Error NotFound =
        new("Donation.NotFound", "Doação não encontrada.");
}
