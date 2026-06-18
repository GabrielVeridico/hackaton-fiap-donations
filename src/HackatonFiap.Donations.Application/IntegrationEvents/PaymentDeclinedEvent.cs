namespace HackatonFiap.Donations.Application.IntegrationEvents;

public sealed record PaymentDeclinedEvent(
    Guid DonationId,
    Guid CampaignId,
    string Reason,
    decimal Amount,
    Guid DonorId,
    string DonorEmail,
    string DonorName);
