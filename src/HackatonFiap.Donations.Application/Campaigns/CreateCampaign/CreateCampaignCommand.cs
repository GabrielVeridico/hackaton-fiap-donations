namespace HackatonFiap.Donations.Application.Campaigns.CreateCampaign;

public sealed record CreateCampaignCommand(
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    decimal Goal,
    Guid CreatedById);
