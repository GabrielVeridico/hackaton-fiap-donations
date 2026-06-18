namespace HackatonFiap.Donations.Application.Donations.ProcessPaymentApproved;

public sealed record ProcessPaymentApprovedCommand(
    Guid DonationId,
    Guid CampaignId,
    decimal Amount,
    Guid PaymentId,
    Guid DonorId,
    string DonorEmail,
    string DonorName);
