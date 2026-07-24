namespace HackatonFiap.Donations.Application.Transparency;

public sealed record TransparencyCampaignResponse(
    Guid Id,
    string Title,
    decimal Goal,
    decimal AmountRaised,
    decimal Percentual);
