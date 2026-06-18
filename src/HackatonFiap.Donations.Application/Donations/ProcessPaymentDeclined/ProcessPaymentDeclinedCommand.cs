namespace HackatonFiap.Donations.Application.Donations.ProcessPaymentDeclined;

public sealed record ProcessPaymentDeclinedCommand(
    Guid DonationId,
    Guid CampaignId,
    string Reason,
    decimal Amount,
    Guid DonorId,
    string DonorEmail,
    string DonorName);
