using HackatonFiap.Donations.Application.ReadModels;
using HackatonFiap.Donations.Domain.Entities;

namespace HackatonFiap.Donations.Application.Campaigns;

public static class CampaignMappings
{
    public static CampaignReadModel ToReadModel(this Campaign campaign)
        => new(campaign.Id, campaign.Title, campaign.Goal, campaign.AmountRaised, campaign.Status.ToString());

    public static CampaignResponse ToResponse(this Campaign campaign)
        => new(campaign.Id, campaign.Title, campaign.Description, campaign.Period.StartDate, campaign.Period.EndDate,
            campaign.Goal, campaign.AmountRaised, campaign.Status.ToString(),
            campaign.CompletionReason?.ToString(), campaign.CreatedAt, campaign.UpdatedAt);
}
