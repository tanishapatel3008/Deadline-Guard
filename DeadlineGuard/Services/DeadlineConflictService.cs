using DeadlineGuard.Models;
using DeadlineGuard.Models.Enums;

namespace DeadlineGuard.Services;

public interface IDeadlineConflictService
{
    List<DeadlineConflict> DetectConflicts(IEnumerable<Deadline> activeDeadlines, int dailyPrepMinutesLimit = 240, DateTime? referenceTimeUtc = null);
    List<DeadlineConflict> GetConflictsForDeadline(Deadline targetDeadline, IEnumerable<Deadline> allActiveDeadlines, int dailyPrepMinutesLimit = 240, DateTime? referenceTimeUtc = null);
}

public class DeadlineConflictService : IDeadlineConflictService
{
    public List<DeadlineConflict> DetectConflicts(
        IEnumerable<Deadline> activeDeadlines,
        int dailyPrepMinutesLimit = 240,
        DateTime? referenceTimeUtc = null)
    {
        var now = referenceTimeUtc ?? DateTime.UtcNow;
        var deadlines = activeDeadlines
            .Where(d => d.Status != DeadlineStatus.Completed && d.Status != DeadlineStatus.Cancelled)
            .OrderBy(d => d.DueDateTime)
            .ToList();

        var conflicts = new List<DeadlineConflict>();

        if (deadlines.Count < 2)
        {
            return conflicts;
        }

        // 1. Check Same-Day Overload
        var byDay = deadlines.GroupBy(d => d.DueDateTime.Date);
        foreach (var group in byDay)
        {
            var dayDeadlines = group.ToList();
            var totalDayMinutes = dayDeadlines.Sum(d => d.EstimatedEffortMinutes);
            var highConsequenceCount = dayDeadlines.Count(d => d.ConsequenceLevel >= ConsequenceLevel.Significant);

            if (dayDeadlines.Count >= 2 && (totalDayMinutes > dailyPrepMinutesLimit || highConsequenceCount >= 2 || totalDayMinutes >= 180))
            {
                var severity = (totalDayMinutes > dailyPrepMinutesLimit * 1.5 || highConsequenceCount >= 2)
                    ? ConflictSeverity.Critical
                    : ConflictSeverity.High;

                conflicts.Add(new DeadlineConflict
                {
                    Type = ConflictType.SameDayOverload,
                    Severity = severity,
                    Title = $"Same-Day Overload on {group.Key:MMM dd, yyyy}",
                    Description = $"{dayDeadlines.Count} major deadlines fall on {group.Key:MMM dd, yyyy} requiring an estimated {totalDayMinutes} minutes ({totalDayMinutes / 60.0:F1} hrs) of total preparation time.",
                    AffectedDeadlines = dayDeadlines,
                    WindowStart = group.Key,
                    WindowEnd = group.Key.AddDays(1).AddSeconds(-1),
                    TotalEstimatedMinutes = totalDayMinutes,
                    AvailableMinutes = dailyPrepMinutesLimit,
                    RecommendedAction = totalDayMinutes > dailyPrepMinutesLimit
                        ? $"Total required prep time ({totalDayMinutes}m) exceeds your daily capacity ({dailyPrepMinutesLimit}m). Begin the highest-consequence task at least 2 days early or negotiate an extension."
                        : "Stagger your submission preparations across the preceding days to avoid last-minute rush."
                });
            }
        }

        // 2. High Consequence Clash (within 48-hour sliding window)
        for (int i = 0; i < deadlines.Count; i++)
        {
            var d1 = deadlines[i];
            if (d1.ConsequenceLevel < ConsequenceLevel.Significant) continue;

            var windowEnd = d1.DueDateTime.AddHours(48);
            var clashing = deadlines
                .Where(d => d.Id != d1.Id &&
                            d.ConsequenceLevel >= ConsequenceLevel.Significant &&
                            d.DueDateTime >= d1.DueDateTime &&
                            d.DueDateTime <= windowEnd)
                .ToList();

            if (clashing.Any())
            {
                var cluster = new List<Deadline> { d1 };
                cluster.AddRange(clashing);

                // Avoid duplicate conflict reporting if already covered
                bool alreadyCovered = conflicts.Any(c =>
                    c.Type == ConflictType.HighConsequenceClash &&
                    c.AffectedDeadlines.Any(ad => ad.Id == d1.Id) &&
                    c.AffectedDeadlines.Any(ad => clashing.Select(x => x.Id).Contains(ad.Id)));

                if (!alreadyCovered)
                {
                    var totalPenalty = cluster.Sum(d => d.PotentialPenaltyAmount ?? 0);
                    conflicts.Add(new DeadlineConflict
                    {
                        Type = ConflictType.HighConsequenceClash,
                        Severity = ConflictSeverity.Critical,
                        Title = "Critical High-Consequence Clash",
                        Description = $"{cluster.Count} high-stakes deadlines fall within a 48-hour window between {d1.DueDateTime:MMM dd, HH:mm} and {cluster.Max(x => x.DueDateTime):MMM dd, HH:mm}." +
                                      (totalPenalty > 0 ? $" Combined potential financial exposure: ${totalPenalty:F2}." : ""),
                        AffectedDeadlines = cluster,
                        WindowStart = d1.DueDateTime,
                        WindowEnd = cluster.Max(x => x.DueDateTime),
                        TotalEstimatedMinutes = cluster.Sum(d => d.EstimatedEffortMinutes),
                        RecommendedAction = "Complete the earliest high-consequence deadline well before its due date to free your mental focus for the second."
                    });
                }
            }
        }

        // 3. Clustered Deadlines (3 or more deadlines within a 72-hour window)
        for (int i = 0; i < deadlines.Count; i++)
        {
            var d1 = deadlines[i];
            var windowEnd = d1.DueDateTime.AddHours(72);
            var inCluster = deadlines
                .Where(d => d.DueDateTime >= d1.DueDateTime && d.DueDateTime <= windowEnd)
                .ToList();

            if (inCluster.Count >= 3)
            {
                bool alreadyCovered = conflicts.Any(c =>
                    c.Type == ConflictType.ClusteredDeadlines &&
                    c.AffectedDeadlines.Count == inCluster.Count &&
                    inCluster.All(ic => c.AffectedDeadlines.Any(ad => ad.Id == ic.Id)));

                if (!alreadyCovered)
                {
                    var totalClusterMins = inCluster.Sum(d => d.EstimatedEffortMinutes);
                    conflicts.Add(new DeadlineConflict
                    {
                        Type = ConflictType.ClusteredDeadlines,
                        Severity = ConflictSeverity.High,
                        Title = $"High Density Cluster ({inCluster.Count} deadlines in 3 days)",
                        Description = $"{inCluster.Count} distinct tasks converge within 72 hours around {d1.DueDateTime:MMM dd}.",
                        AffectedDeadlines = inCluster,
                        WindowStart = d1.DueDateTime,
                        WindowEnd = inCluster.Max(x => x.DueDateTime),
                        TotalEstimatedMinutes = totalClusterMins,
                        RecommendedAction = "Break tasks into 30-minute milestones and front-load easier administrative items."
                    });
                }
            }
        }

        // 4. Overlapping Workload / Preparation Bottleneck
        // Tasks due shortly where the combined effort exceeds the remaining hours until the earliest deadline
        var nearDeadlines = deadlines.Where(d => d.DueDateTime > now && (d.DueDateTime - now).TotalHours <= 36).ToList();
        if (nearDeadlines.Count >= 2)
        {
            var totalPrepMinutes = nearDeadlines.Sum(d => d.EstimatedEffortMinutes);
            var earliestDueHours = (nearDeadlines.Min(d => d.DueDateTime) - now).TotalHours;
            var availableHoursUntilEarliest = Math.Max(0.5, earliestDueHours);

            if ((totalPrepMinutes / 60.0) >= availableHoursUntilEarliest * 0.75)
            {
                conflicts.Add(new DeadlineConflict
                {
                    Type = ConflictType.OverlappingWorkload,
                    Severity = ConflictSeverity.Critical,
                    Title = "Immediate Workload Bottleneck",
                    Description = $"You have {nearDeadlines.Count} tasks due in under 36 hours requiring {totalPrepMinutes} minutes ({totalPrepMinutes / 60.0:F1} hrs) of active effort, leaving dangerously little buffer before the first deadline.",
                    AffectedDeadlines = nearDeadlines,
                    WindowStart = now,
                    WindowEnd = nearDeadlines.Max(d => d.DueDateTime),
                    TotalEstimatedMinutes = totalPrepMinutes,
                    RecommendedAction = "Eliminate non-essential tasks today. Focus 100% on the nearest deadline first."
                });
            }
        }

        return conflicts;
    }

    public List<DeadlineConflict> GetConflictsForDeadline(
        Deadline targetDeadline,
        IEnumerable<Deadline> allActiveDeadlines,
        int dailyPrepMinutesLimit = 240,
        DateTime? referenceTimeUtc = null)
    {
        var allConflicts = DetectConflicts(allActiveDeadlines, dailyPrepMinutesLimit, referenceTimeUtc);
        return allConflicts
            .Where(c => c.AffectedDeadlines.Any(d => d.Id == targetDeadline.Id))
            .ToList();
    }
}
