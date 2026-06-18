namespace HackatonFiap.Donations.Domain.Entities;

public class ProcessedEvent
{
    private ProcessedEvent() { } // EF

    public ProcessedEvent(Guid donationId, DateTime processedAt)
    {
        DonationId = donationId;
        ProcessedAt = processedAt;
    }

    public Guid DonationId { get; private set; }
    public DateTime ProcessedAt { get; private set; }
}
