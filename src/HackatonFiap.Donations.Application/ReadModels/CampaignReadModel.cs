namespace HackatonFiap.Donations.Application.ReadModels;

public sealed record CampaignReadModel(
    Guid Id,
    string Title,
    decimal Goal,
    decimal AmountRaised,
    string Status);
