namespace HackatonFiap.Donations.Application.Donations;

public sealed record DonationResponse(
    Guid Id,
    Guid CampaignId,
    decimal Amount,
    string Method,
    string Status,
    string? DeclineReason,
    DateTime CreatedAt,
    DateTime? ProcessedAt);
