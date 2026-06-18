using HackatonFiap.Donations.Domain.Common;

namespace HackatonFiap.Donations.Domain.ValueObjects;

public sealed class Period
{
    private Period() { } // EF

    private Period(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    public static readonly Error EndBeforeStart =
        new("Period.EndBeforeStart", "A data fim não pode ser anterior à data início.");

    public static Result<Period> Create(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
        {
            return Result.Failure<Period>(EndBeforeStart);
        }

        return Result.Success(new Period(startDate, endDate));
    }

    public bool Contains(DateTime instant) => instant >= StartDate && instant <= EndDate;
}
