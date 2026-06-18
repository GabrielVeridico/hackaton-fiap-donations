namespace HackatonFiap.Donations.Application.Campaigns;

public sealed record CampaignResponse(
    Guid Id,
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    decimal Goal,
    decimal AmountRaised,
    string Status,
    string? CompletionReason,
    DateTime CreatedAt,
    DateTime UpdatedAt);
