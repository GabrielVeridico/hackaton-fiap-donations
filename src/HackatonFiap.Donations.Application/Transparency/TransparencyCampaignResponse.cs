namespace HackatonFiap.Donations.Application.Transparency;

public sealed record TransparencyCampaignResponse(
    string Title,
    decimal Goal,
    decimal AmountRaised,
    decimal Percentual);
