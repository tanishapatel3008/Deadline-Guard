using DeadlineGuard.Models;
using DeadlineGuard.Models.Enums;
using DeadlineGuard.Services;
using Xunit;

namespace DeadlineGuard.Tests;

public class PriorityEngineTests
{
    private readonly DeadlinePriorityService _priorityService = new();

    [Fact]
    public void CalculatePriority_OverdueDeadline_ReceivesMaxUrgencyAndOverdueFlag()
    {
        // Arrange
        var now = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        var deadline = new Deadline
        {
            Id = 1,
            Title = "Overdue Tax Return",
            DueDateTime = now.AddHours(-5), // 5 hours overdue
            ConsequenceLevel = ConsequenceLevel.Severe,
            ImportanceLevel = ImportanceLevel.Critical,
            EstimatedEffortMinutes = 60,
            Status = DeadlineStatus.Pending
        };

        // Act
        var result = _priorityService.CalculatePriority(deadline, now);

        // Assert
        Assert.True(result.IsOverdue);
        Assert.Equal(100.0, result.UrgencyScore);
        Assert.Equal(PriorityClassification.Critical, result.Classification);
        Assert.True(result.Score >= 80.0, $"Expected score >= 80, got {result.Score}");
        Assert.Contains("Overdue", result.TimeRemainingText);
        Assert.Contains("immediately", result.SuggestedAction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CalculatePriority_CompletedDeadline_ReceivesZeroScore()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var deadline = new Deadline
        {
            Id = 2,
            Title = "Completed Project",
            DueDateTime = now.AddDays(-1),
            Status = DeadlineStatus.Completed,
            LastCompletedAt = now.AddDays(-1)
        };

        // Act
        var result = _priorityService.CalculatePriority(deadline, now);

        // Assert
        Assert.Equal(0, result.Score);
        Assert.Equal(PriorityClassification.Low, result.Classification);
        Assert.False(result.IsOverdue);
        Assert.Equal("Completed", result.TimeRemainingText);
    }

    [Theory]
    [InlineData(10, 85.0)] // < 24 hours => urgency >= 85
    [InlineData(36, 65.0)] // 1-2 days => urgency 65
    [InlineData(96, 45.0)] // 3-6 days => urgency 45
    [InlineData(240, 25.0)] // 7-14 days => urgency 25
    [InlineData(400, 10.0)] // > 14 days => urgency 10
    public void CalculatePriority_UrgencyScale_AlignsWithTimeRemaining(int hoursRemaining, double expectedBaseUrgency)
    {
        // Arrange
        var now = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        var deadline = new Deadline
        {
            Id = 3,
            Title = "Upcoming Task",
            DueDateTime = now.AddHours(hoursRemaining),
            ConsequenceLevel = ConsequenceLevel.Moderate,
            ImportanceLevel = ImportanceLevel.Medium,
            EstimatedEffortMinutes = 30,
            Status = DeadlineStatus.Pending
        };

        // Act
        var result = _priorityService.CalculatePriority(deadline, now);

        // Assert
        if (hoursRemaining < 24)
        {
            Assert.True(result.UrgencyScore >= expectedBaseUrgency, $"Expected urgency >= {expectedBaseUrgency}, got {result.UrgencyScore}");
        }
        else
        {
            Assert.Equal(expectedBaseUrgency, result.UrgencyScore);
        }
    }

    [Fact]
    public void CalculatePriority_EffortPreparationBoost_WhenRunwayIsTight()
    {
        // Arrange: Task takes 5 hours (300 mins) and is due in 8 hours (effort >= 40% of remaining time)
        var now = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        var deadline = new Deadline
        {
            Id = 4,
            Title = "Large Thesis Submission",
            DueDateTime = now.AddHours(8),
            EstimatedEffortMinutes = 300, // 5 hours
            ConsequenceLevel = ConsequenceLevel.Significant,
            ImportanceLevel = ImportanceLevel.High,
            Status = DeadlineStatus.Pending
        };

        // Act
        var result = _priorityService.CalculatePriority(deadline, now);

        // Assert
        Assert.True(result.EffortScore >= 95.0, $"Effort score should be boosted due to preparation bottleneck. Got {result.EffortScore}");
        Assert.Equal(PriorityClassification.Critical, result.Classification);
        Assert.Contains("Prioritize this task first", result.SuggestedAction);
    }
}
