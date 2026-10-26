using DeadlineGuard.Models;

namespace DeadlineGuard.Services;

public enum ConflictType
{
    SameDayOverload,
    ClusteredDeadlines,
    OverlappingWorkload,
    HighConsequenceClash,
    DailyPrepExceeded
}

public enum ConflictSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public class DeadlineConflict
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public ConflictType Type { get; set; }
    public ConflictSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Deadline> AffectedDeadlines { get; set; } = new();
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
    public int TotalEstimatedMinutes { get; set; }
    public int? AvailableMinutes { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;

    public string SeverityBadgeClass => Severity switch
    {
        ConflictSeverity.Critical => "badge bg-danger",
        ConflictSeverity.High => "badge bg-warning text-dark",
        ConflictSeverity.Medium => "badge bg-info text-dark",
        _ => "badge bg-secondary"
    };
}
