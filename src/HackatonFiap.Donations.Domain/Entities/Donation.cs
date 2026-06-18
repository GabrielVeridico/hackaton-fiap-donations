using HackatonFiap.Donations.Domain.Common;
using HackatonFiap.Donations.Domain.Enums;

namespace HackatonFiap.Donations.Domain.Entities;

public class Donation
{
    private Donation() { } // EF

    private Donation(Guid campaignId, decimal amount, PaymentMethod method, Guid donorId, string donorEmail, string donorName)
    {
        Id = Guid.NewGuid();
        CampaignId = campaignId;
        Amount = amount;
        Method = method;
        DonorId = donorId;
        DonorEmail = donorEmail;
        DonorName = donorName;
        Status = DonationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public DonationStatus Status { get; private set; }
    public Guid DonorId { get; private set; }
    public string DonorEmail { get; private set; } = string.Empty;
    public string DonorName { get; private set; } = string.Empty;
    public string? DeclineReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    public static readonly Error AmountMustBePositive = new("Donation.AmountMustBePositive", "O valor da doação deve ser maior que zero.");
    public static readonly Error CampaignRequired = new("Donation.CampaignRequired", "A campanha é obrigatória.");

    public static Result<Donation> Create(Guid campaignId, decimal amount, PaymentMethod method,
        Guid donorId, string donorEmail, string donorName)
    {
        if (campaignId == Guid.Empty)
        {
            return Result.Failure<Donation>(CampaignRequired);
        }

        if (amount <= 0m)
        {
            return Result.Failure<Donation>(AmountMustBePositive);
        }

        return Result.Success(new Donation(campaignId, amount, method, donorId, donorEmail ?? string.Empty, donorName ?? string.Empty));
    }

    public void Approve()
    {
        if (Status != DonationStatus.Pending)
        {
            throw new InvalidOperationException($"Não é possível aprovar doação com status {Status}.");
        }

        Status = DonationStatus.Approved;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Decline(string reason)
    {
        if (Status != DonationStatus.Pending)
        {
            throw new InvalidOperationException($"Não é possível recusar doação com status {Status}.");
        }

        Status = DonationStatus.Declined;
        DeclineReason = string.IsNullOrWhiteSpace(reason) ? "Pagamento recusado." : reason;
        ProcessedAt = DateTime.UtcNow;
    }
}
