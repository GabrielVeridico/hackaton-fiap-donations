using HackatonFiap.Donations.Domain.Enums;

namespace HackatonFiap.Donations.Application.Donations.CreateDonation;

public sealed record CreateDonationCommand(
    Guid CampaignId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    Guid DonorId,
    string DonorEmail,
    string DonorName);
