using DeadlineGuard.Models;
using DeadlineGuard.Models.Enums;
using Microsoft.Extensions.Options;

namespace DeadlineGuard.Services;

public interface IDeadlinePriorityService
{
    PriorityEvaluation CalculatePriority(Deadline deadline, DateTime? referenceTimeUtc = null);
    IEnumerable<(Deadline Deadline, PriorityEvaluation Evaluation)> RankDeadlines(IEnumerable<Deadline> deadlines, DateTime? referenceTimeUtc = null);
}

public class DeadlinePriorityService : IDeadlinePriorityService
{
    private readonly PriorityEngineOptions _options;

    public DeadlinePriorityService(IOptions<PriorityEngineOptions>? options = null)
    {
        _options = options?.Value ?? new PriorityEngineOptions();
    }

    public PriorityEvaluation CalculatePriority(Deadline deadline, DateTime? referenceTimeUtc = null)
    {
        var now = referenceTimeUtc ?? DateTime.UtcNow;

        // If completed or cancelled, score is zero and low priority
        if (deadline.Status == DeadlineStatus.Completed || deadline.Status == DeadlineStatus.Cancelled)
        {
            return new PriorityEvaluation
            {
                Score = 0,
                Classification = PriorityClassification.Low,
                UrgencyScore = 0,
                ConsequenceScore = 0,
                EffortScore = 0,
                ImportanceScore = 0,
                IsOverdue = false,
                TimeRemaining = TimeSpan.Zero,
                TimeRemainingText = deadline.Status == DeadlineStatus.Completed ? "Completed" : "Cancelled",
                ConsequenceSummary = "No active risk",
                EffortSummary = $"{deadline.EstimatedEffortMinutes} mins planned",
                Explanation = deadline.Status == DeadlineStatus.Completed
                    ? $"This deadline was completed on {deadline.LastCompletedAt:MMM dd, yyyy}."
                    : "This deadline has been cancelled.",
                SuggestedAction = "No action required."
            };
        }

        var timeRemaining = deadline.DueDateTime - now;
        var isOverdue = timeRemaining < TimeSpan.Zero;

        // 1. Calculate Urgency Score (0-100)
        double urgencyScore;
        string timeRemainingText;

        if (isOverdue)
        {
            urgencyScore = 100.0;
            var pastTime = now - deadline.DueDateTime;
            if (pastTime.TotalHours < 24)
            {
                timeRemainingText = $"Overdue by {(int)Math.Max(1, pastTime.TotalHours)} hours";
            }
            else
            {
                timeRemainingText = $"Overdue by {(int)pastTime.TotalDays} days";
            }
        }
        else
        {
            var totalHours = timeRemaining.TotalHours;
            if (totalHours < 24)
            {
                // Less than 24 hours: 85 to 99 continuous scale as deadline approaches
                urgencyScore = 85.0 + ((24.0 - Math.Max(0, totalHours)) / 24.0) * 14.0;
                timeRemainingText = totalHours <= 1 ? "Due in under an hour" : $"Due in {(int)totalHours} hours";
            }
            else if (totalHours <= 48) // 1-2 days
            {
                urgencyScore = 65.0;
                timeRemainingText = $"Due in {(int)timeRemaining.TotalDays} days";
            }
            else if (totalHours <= 144) // 3-6 days
            {
                urgencyScore = 45.0;
                timeRemainingText = $"Due in {(int)timeRemaining.TotalDays} days";
            }
            else if (totalHours <= 336) // 7-14 days
            {
                urgencyScore = 25.0;
                timeRemainingText = $"Due in {(int)timeRemaining.TotalDays} days";
            }
            else
            {
                urgencyScore = 10.0;
                timeRemainingText = $"Due in {(int)timeRemaining.TotalDays} days";
            }
        }

        // 2. Calculate Consequence Score (0-100)
        double baseConsequence = deadline.ConsequenceLevel switch
        {
            ConsequenceLevel.Minor => 25.0,
            ConsequenceLevel.Moderate => 50.0,
            ConsequenceLevel.Significant => 75.0,
            ConsequenceLevel.Severe => 100.0,
            _ => 50.0
        };

        // Fine-tune consequence if there's an explicit financial penalty
        double consequenceScore = baseConsequence;
        if (deadline.PotentialPenaltyAmount.HasValue && deadline.PotentialPenaltyAmount.Value > 0)
        {
            // If penalty is substantial, ensure consequence reflects the tangible loss
            if (deadline.PotentialPenaltyAmount.Value >= 500)
            {
                consequenceScore = Math.Min(100.0, consequenceScore + 15.0);
            }
            else if (deadline.PotentialPenaltyAmount.Value >= 100)
            {
                consequenceScore = Math.Min(100.0, consequenceScore + 8.0);
            }
        }

        string consequenceSummary = deadline.ConsequenceType switch
        {
            ConsequenceType.FinancialPenalty => deadline.PotentialPenaltyAmount.HasValue
                ? $"Penalty fee of {deadline.CurrencyCode} {deadline.PotentialPenaltyAmount.Value:F2}"
                : "Financial penalty applies",
            ConsequenceType.LostOpportunity => "Risk of forfeited admission/career opportunity",
            ConsequenceType.ServiceInterruption => "Risk of utility or service interruption",
            ConsequenceType.LegalAdministrative => "Risk of legal penalties or administrative sanctions",
            ConsequenceType.OrdinaryInconvenience => "Moderate inconvenience or reschedule fees",
            _ => "Standard consequences"
        };

        // 3. Calculate Effort Factor (0-100)
        double effortMinutes = Math.Max(0, deadline.EstimatedEffortMinutes);
        double baseEffort;
        if (effortMinutes <= 30)
        {
            baseEffort = 20.0;
        }
        else if (effortMinutes <= 120)
        {
            baseEffort = 45.0;
        }
        else if (effortMinutes <= 300)
        {
            baseEffort = 70.0;
        }
        else
        {
            baseEffort = 90.0;
        }

        // Preparation urgency boost: if effort hours is close to or exceeds remaining hours
        double effortHours = effortMinutes / 60.0;
        double effortScore = baseEffort;

        if (!isOverdue && timeRemaining.TotalHours > 0)
        {
            // If preparation requires more than 40% of the remaining time, effort urgency scales to 100
            if (effortHours >= (timeRemaining.TotalHours * 0.4))
            {
                effortScore = Math.Min(100.0, baseEffort + 25.0);
            }
        }
        else if (isOverdue)
        {
            // When overdue, effort indicates backlog burden
            effortScore = Math.Min(100.0, baseEffort + 10.0);
        }

        string effortSummary = effortMinutes < 60
            ? $"{effortMinutes:0} mins required"
            : $"{effortHours:0.#} hrs required";

        // 4. Calculate Importance Score (0-100)
        double importanceScore = deadline.ImportanceLevel switch
        {
            ImportanceLevel.Low => 25.0,
            ImportanceLevel.Medium => 50.0,
            ImportanceLevel.High => 75.0,
            ImportanceLevel.Critical => 100.0,
            _ => 50.0
        };

        // 5. Final Score Calculation with configured weights
        double rawScore = (urgencyScore * _options.UrgencyWeight)
                        + (consequenceScore * _options.ConsequenceWeight)
                        + (effortScore * _options.EffortWeight)
                        + (importanceScore * _options.ImportanceWeight);

        // Apply custom priority weight override if set
        if (deadline.CustomPriorityWeight.HasValue)
        {
            rawScore = (rawScore * 0.8) + (deadline.CustomPriorityWeight.Value * 0.2);
        }

        double finalScore = Math.Round(Math.Clamp(rawScore, 0.0, 100.0), 1);

        // 6. Classification
        var classification = finalScore switch
        {
            >= 80.0 => PriorityClassification.Critical,
            >= 60.0 => PriorityClassification.High,
            >= 35.0 => PriorityClassification.Medium,
            _ => PriorityClassification.Low
        };

        // 7. Human-readable explanation and suggested action
        string explanation;
        string suggestedAction;

        if (isOverdue)
        {
            explanation = $"Overdue by {timeRemainingText.Replace("Overdue by ", "")}. Urgency is at maximum (100). {consequenceSummary}.";
            suggestedAction = "Resolve immediately to halt compounding fees or administrative penalties.";
        }
        else if (classification == PriorityClassification.Critical)
        {
            explanation = $"Critical priority: {timeRemainingText}, {deadline.ConsequenceLevel.ToString().ToLower()} consequence severity, with {effortSummary}.";
            suggestedAction = "Prioritize this task first. Begin the preparation work today without delay.";
        }
        else if (classification == PriorityClassification.High)
        {
            explanation = $"High priority: {timeRemainingText}, significant impact if missed ({consequenceSummary}), requiring {effortSummary}.";
            suggestedAction = effortHours >= 2.0
                ? "Block out a focused work block today to start this task before time runs short."
                : "Schedule completion within the next 24 hours.";
        }
        else if (classification == PriorityClassification.Medium)
        {
            explanation = $"Medium priority: {timeRemainingText}. Moderate urgency and consequence level.";
            suggestedAction = "Review requirements and keep on your radar for completion this week.";
        }
        else
        {
            explanation = $"Low priority: {timeRemainingText}. Ample time remaining with manageable effort.";
            suggestedAction = "Add to regular backlog; no immediate action required today.";
        }

        return new PriorityEvaluation
        {
            Score = finalScore,
            Classification = classification,
            UrgencyScore = Math.Round(urgencyScore, 1),
            ConsequenceScore = Math.Round(consequenceScore, 1),
            EffortScore = Math.Round(effortScore, 1),
            ImportanceScore = Math.Round(importanceScore, 1),
            IsOverdue = isOverdue,
            TimeRemaining = timeRemaining,
            TimeRemainingText = timeRemainingText,
            ConsequenceSummary = consequenceSummary,
            EffortSummary = effortSummary,
            Explanation = explanation,
            SuggestedAction = suggestedAction
        };
    }

    public IEnumerable<(Deadline Deadline, PriorityEvaluation Evaluation)> RankDeadlines(
        IEnumerable<Deadline> deadlines,
        DateTime? referenceTimeUtc = null)
    {
        return deadlines
            .Select(d => (Deadline: d, Evaluation: CalculatePriority(d, referenceTimeUtc)))
            .OrderByDescending(x => x.Evaluation.Score)
            .ThenBy(x => x.Deadline.DueDateTime);
    }
}
