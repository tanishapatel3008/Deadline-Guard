using DeadlineGuard.Models;
using DeadlineGuard.Models.Enums;
using DeadlineGuard.Services;
using Xunit;

namespace DeadlineGuard.Tests;

public class ConflictServiceTests
{
    private readonly DeadlineConflictService _conflictService = new();

    [Fact]
    public void DetectConflicts_SameDayOverload_DetectedWhenEffortExceedsDailyLimit()
    {
        // Arrange
        var date = new DateTime(2026, 11, 15, 14, 0, 0, DateTimeKind.Utc);
        var deadlines = new List<Deadline>
        {
            new() { Id = 1, Title = "Task A", DueDateTime = date, EstimatedEffortMinutes = 150, Status = DeadlineStatus.Pending },
            new() { Id = 2, Title = "Task B", DueDateTime = date.AddHours(2), EstimatedEffortMinutes = 180, Status = DeadlineStatus.Pending }
        };

        // Act: 150 + 180 = 330 mins > 240 mins limit
        var conflicts = _conflictService.DetectConflicts(deadlines, dailyPrepMinutesLimit: 240, referenceTimeUtc: date.AddDays(-2));

        // Assert
        Assert.NotEmpty(conflicts);
        var sameDay = conflicts.FirstOrDefault(c => c.Type == ConflictType.SameDayOverload);
        Assert.NotNull(sameDay);
        Assert.Equal(330, sameDay.TotalEstimatedMinutes);
        Assert.Contains("exceeds your daily capacity", sameDay.RecommendedAction);
    }

    [Fact]
    public void DetectConflicts_HighConsequenceClash_DetectedWithin48Hours()
    {
        // Arrange
        var start = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var deadlines = new List<Deadline>
        {
            new()
            {
                Id = 1,
                Title = "Critical Court Filing",
                DueDateTime = start,
                ConsequenceLevel = ConsequenceLevel.Severe,
                PotentialPenaltyAmount = 500,
                Status = DeadlineStatus.Pending
            },
            new()
            {
                Id = 2,
                Title = "Visa Renewal Deadline",
                DueDateTime = start.AddHours(24),
                ConsequenceLevel = ConsequenceLevel.Significant,
                PotentialPenaltyAmount = 300,
                Status = DeadlineStatus.Pending
            }
        };

        // Act
        var conflicts = _conflictService.DetectConflicts(deadlines, dailyPrepMinutesLimit: 480, referenceTimeUtc: start.AddDays(-3));

        // Assert
        Assert.NotEmpty(conflicts);
        var clash = conflicts.FirstOrDefault(c => c.Type == ConflictType.HighConsequenceClash);
        Assert.NotNull(clash);
        Assert.Equal(ConflictSeverity.Critical, clash.Severity);
        Assert.Equal(2, clash.AffectedDeadlines.Count);
    }

    [Fact]
    public void DetectConflicts_NoConflicts_WhenDeadlinesAreSpacedAndManageable()
    {
        // Arrange
        var now = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var deadlines = new List<Deadline>
        {
            new() { Id = 1, Title = "Light Task 1", DueDateTime = now.AddDays(5), EstimatedEffortMinutes = 30, ConsequenceLevel = ConsequenceLevel.Minor, Status = DeadlineStatus.Pending },
            new() { Id = 2, Title = "Light Task 2", DueDateTime = now.AddDays(15), EstimatedEffortMinutes = 45, ConsequenceLevel = ConsequenceLevel.Moderate, Status = DeadlineStatus.Pending }
        };

        // Act
        var conflicts = _conflictService.DetectConflicts(deadlines, dailyPrepMinutesLimit: 240, referenceTimeUtc: now);

        // Assert
        Assert.Empty(conflicts);
    }
}
