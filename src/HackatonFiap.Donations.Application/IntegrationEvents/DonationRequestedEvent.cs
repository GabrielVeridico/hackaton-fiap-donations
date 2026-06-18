using HackatonFiap.Donations.Domain.Enums;

namespace HackatonFiap.Donations.Application.IntegrationEvents;

public sealed record DonationRequestedEvent(
    Guid DonationId,
    Guid CampaignId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    Guid DonorId,
    string DonorEmail,
    string DonorName);
