namespace HackatonFiap.Donations.Application.Campaigns.ChangeCampaignStatus;

public sealed record ChangeCampaignStatusCommand(Guid Id, CampaignStatusAction Action);
