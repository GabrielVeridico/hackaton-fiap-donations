using HackatonFiap.Donations.Domain.Common;

namespace HackatonFiap.Donations.Application.Errors;

public static class CampaignErrors
{
    public static readonly Error NotFound = new("Campaign.NotFound", "Campanha não encontrada.");
}
