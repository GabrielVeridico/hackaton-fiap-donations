using HackatonFiap.Donations.Domain.Common;
using HackatonFiap.Donations.Domain.Enums;
using HackatonFiap.Donations.Domain.ValueObjects;

namespace HackatonFiap.Donations.Domain.Entities;

public class Campaign
{
    private Campaign() { } // EF

    private Campaign(string title, string description, Period period, decimal goal, Guid createdById, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Period = period;
        Goal = goal;
        CreatedById = createdById;
        AmountRaised = 0m;
        Status = CampaignStatus.Active;
        CompletionReason = null;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Period Period { get; private set; } = null!;
    public decimal Goal { get; private set; }
    public decimal AmountRaised { get; private set; }
    public CampaignStatus Status { get; private set; }
    public CompletionReason? CompletionReason { get; private set; }
    public Guid CreatedById { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static readonly Error TitleRequired = new("Campaign.TitleRequired", "O título é obrigatório.");
    public static readonly Error GoalMustBePositive = new("Campaign.GoalMustBePositive", "A meta financeira deve ser maior que zero.");
    public static readonly Error EndDateInPast = new("Campaign.EndDateInPast", "A data fim não pode estar no passado.");
    public static readonly Error InvalidStatusTransition = new("Campaign.InvalidStatusTransition", "Transição de status inválida: a campanha não está ativa.");

    public static Result<Campaign> Create(string title, string description, DateTime startDate, DateTime endDate,
        decimal goal, Guid createdById, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<Campaign>(TitleRequired);
        }

        if (goal <= 0m)
        {
            return Result.Failure<Campaign>(GoalMustBePositive);
        }

        if (endDate < utcNow)
        {
            return Result.Failure<Campaign>(EndDateInPast);
        }

        var period = Period.Create(startDate, endDate);
        if (period.IsFailure)
        {
            return Result.Failure<Campaign>(period.Error);
        }

        return Result.Success(new Campaign(title.Trim(), description ?? string.Empty, period.Value, goal, createdById, utcNow));
    }

    public Result Update(string title, string description, DateTime startDate, DateTime endDate, decimal goal, DateTime utcNow)
    {
        if (Status != CampaignStatus.Active)
        {
            return Result.Failure(InvalidStatusTransition);
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure(TitleRequired);
        }

        if (goal <= 0m)
        {
            return Result.Failure(GoalMustBePositive);
        }

        if (endDate < utcNow)
        {
            return Result.Failure(EndDateInPast);
        }

        var period = Period.Create(startDate, endDate);
        if (period.IsFailure)
        {
            return Result.Failure(period.Error);
        }

        Title = title.Trim();
        Description = description ?? string.Empty;
        Period = period.Value;
        Goal = goal;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public void AddRaised(decimal amount)
    {
        if (amount <= 0m)
        {
            return;
        }

        AmountRaised += amount;
    }

    public Result Complete(CompletionReason reason)
    {
        if (Status != CampaignStatus.Active)
        {
            return Result.Failure(InvalidStatusTransition);
        }

        Status = CampaignStatus.Completed;
        CompletionReason = reason;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status != CampaignStatus.Active)
        {
            return Result.Failure(InvalidStatusTransition);
        }

        Status = CampaignStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
