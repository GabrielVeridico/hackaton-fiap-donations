using HackatonFiap.Donations.Application.Abstractions;
using HackatonFiap.Donations.Application.Errors;
using HackatonFiap.Donations.Application.Observability;
using HackatonFiap.Donations.Domain.Common;
using HackatonFiap.Donations.Domain.Entities;

namespace HackatonFiap.Donations.Application.Donations.ProcessPaymentDeclined;

public sealed class ProcessPaymentDeclinedCommandHandler
{
    private readonly IDonationRepository _donations;
    private readonly IProcessedEventStore _processed;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ProcessPaymentDeclinedCommandHandler(IDonationRepository donations, IProcessedEventStore processed,
        IUnitOfWork unitOfWork, IClock clock)
    {
        _donations = donations;
        _processed = processed;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(ProcessPaymentDeclinedCommand command, CancellationToken cancellationToken)
    {
        if (await _processed.ExistsAsync(command.DonationId, cancellationToken))
        {
            return Result.Success();
        }

        var donation = await _donations.GetByIdAsync(command.DonationId, cancellationToken);
        if (donation is null)
        {
            return Result.Failure(DonationErrors.NotFound);
        }

        donation.Decline(command.Reason);
        await _processed.AddAsync(new ProcessedEvent(command.DonationId, _clock.UtcNow), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        DonationMetrics.DonationsDeclined.Add(1);
        return Result.Success();
    }
}
