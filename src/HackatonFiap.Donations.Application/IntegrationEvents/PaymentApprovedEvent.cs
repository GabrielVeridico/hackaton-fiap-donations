namespace HackatonFiap.Donations.Application.IntegrationEvents;

public sealed record PaymentApprovedEvent(
    Guid DonationId,
    Guid CampaignId,
    decimal Amount,
    Guid PaymentId,
    Guid DonorId,
    string DonorEmail,
    string DonorName);
