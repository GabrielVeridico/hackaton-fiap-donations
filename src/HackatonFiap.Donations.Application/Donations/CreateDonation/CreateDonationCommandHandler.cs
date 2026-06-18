using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.Errors;
using HackatonFiap.Donations.Application.IntegrationEvents;
using HackatonFiap.Donations.Application.Observability;
using HackatonFiap.Donations.Domain.Common;
using HackatonFiap.Donations.Domain.Entities;
using HackatonFiap.Donations.Domain.Enums;

namespace HackatonFiap.Donations.Application.Donations.CreateDonation;

public sealed class CreateDonationCommandHandler
{
    public const string EventSubject = "DonationRequested";

    private readonly ICampaignRepository _campaigns;
    private readonly IDonationRepository _donations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;
    private readonly IClock _clock;

    public CreateDonationCommandHandler(ICampaignRepository campaigns, IDonationRepository donations,
        IUnitOfWork unitOfWork, IEventPublisher publisher, IClock clock)
    {
        _campaigns = campaigns;
        _donations = donations;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CreateDonationCommand command, CancellationToken cancellationToken)
    {
        var campaign = await _campaigns.GetByIdAsync(command.CampaignId, cancellationToken);
        if (campaign is null)
        {
            return Result.Failure<Guid>(DonationErrors.CampaignNotFound);
        }

        if (campaign.Status != CampaignStatus.Active)
        {
            return Result.Failure<Guid>(DonationErrors.CampaignNotActive);
        }

        if (!campaign.Period.Contains(_clock.UtcNow))
        {
            return Result.Failure<Guid>(DonationErrors.OutsidePeriod);
        }

        var creation = Donation.Create(command.CampaignId, command.Amount, command.PaymentMethod,
            command.DonorId, command.DonorEmail, command.DonorName);
        if (creation.IsFailure)
        {
            return Result.Failure<Guid>(creation.Error);
        }

        var donation = creation.Value;
        await _donations.AddAsync(donation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var integrationEvent = new DonationRequestedEvent(
            donation.Id, donation.CampaignId, donation.Amount, donation.Method,
            donation.DonorId, donation.DonorEmail, donation.DonorName);
        await _publisher.PublishAsync(integrationEvent, EventSubject, cancellationToken);

        DonationMetrics.DonationsReceived.Add(1);
        return Result.Success(donation.Id);
    }
}
