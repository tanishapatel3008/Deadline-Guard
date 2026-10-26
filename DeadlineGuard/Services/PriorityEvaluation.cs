using DeadlineGuard.Models.Enums;

namespace DeadlineGuard.Services;

public class PriorityEvaluation
{
    public double Score { get; set; }
    public PriorityClassification Classification { get; set; }
    public double UrgencyScore { get; set; }
    public double ConsequenceScore { get; set; }
    public double EffortScore { get; set; }
    public double ImportanceScore { get; set; }

    public bool IsOverdue { get; set; }
    public TimeSpan TimeRemaining { get; set; }
    public string TimeRemainingText { get; set; } = string.Empty;
    public string ConsequenceSummary { get; set; } = string.Empty;
    public string EffortSummary { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;

    public string BadgeCssClass => Classification switch
    {
        PriorityClassification.Critical => "badge bg-danger text-white",
        PriorityClassification.High => "badge bg-warning text-dark",
        PriorityClassification.Medium => "badge bg-info text-dark",
        _ => "badge bg-secondary text-white"
    };

    public string ProgressColorClass => Classification switch
    {
        PriorityClassification.Critical => "bg-danger",
        PriorityClassification.High => "bg-warning",
        PriorityClassification.Medium => "bg-info",
        _ => "bg-secondary"
    };
}
