namespace DeadlineGuard.Services;

public class PriorityEngineOptions
{
    public double UrgencyWeight { get; set; } = 0.40;
    public double ConsequenceWeight { get; set; } = 0.30;
    public double EffortWeight { get; set; } = 0.15;
    public double ImportanceWeight { get; set; } = 0.15;
}
