using DeadlineGuard.Models.Enums;
using DeadlineGuard.Services;
using Moq;
using Xunit;

namespace DeadlineGuard.Tests;

public class RecurrenceTests
{
    private readonly DeadlineService _deadlineService;

    public RecurrenceTests()
    {
        // Mock dependencies for unit testing pure recurrence algorithm
        var priorityMock = new Mock<IDeadlinePriorityService>();
        var conflictMock = new Mock<IDeadlineConflictService>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<DeadlineService>>();

        _deadlineService = new DeadlineService(null!, priorityMock.Object, conflictMock.Object, loggerMock.Object);
    }

    [Fact]
    public void CalculateNextOccurrence_Daily_AddsCorrectDays()
    {
        var due = new DateTime(2026, 5, 10, 14, 0, 0, DateTimeKind.Utc);
        var next = _deadlineService.CalculateNextOccurrence(due, RecurrenceRule.Daily, interval: 2);

        Assert.Equal(new DateTime(2026, 5, 12, 14, 0, 0, DateTimeKind.Utc), next);
    }

    [Fact]
    public void CalculateNextOccurrence_Weekly_AddsCorrectWeeks()
    {
        var due = new DateTime(2026, 5, 10, 14, 0, 0, DateTimeKind.Utc);
        var next = _deadlineService.CalculateNextOccurrence(due, RecurrenceRule.Weekly, interval: 1);

        Assert.Equal(new DateTime(2026, 5, 17, 14, 0, 0, DateTimeKind.Utc), next);
    }

    [Fact]
    public void CalculateNextOccurrence_Monthly_HandlesMonthEndSafe()
    {
        // Jan 31 + 1 month in non-leap year 2027 => Feb 28
        var due = new DateTime(2027, 1, 31, 10, 0, 0, DateTimeKind.Utc);
        var next = _deadlineService.CalculateNextOccurrence(due, RecurrenceRule.Monthly, interval: 1);

        Assert.Equal(new DateTime(2027, 2, 28, 10, 0, 0, DateTimeKind.Utc), next);
    }

    [Fact]
    public void CalculateNextOccurrence_Monthly_HandlesLeapYearFeb()
    {
        // Jan 31 + 1 month in leap year 2028 => Feb 29
        var due = new DateTime(2028, 1, 31, 10, 0, 0, DateTimeKind.Utc);
        var next = _deadlineService.CalculateNextOccurrence(due, RecurrenceRule.Monthly, interval: 1);

        Assert.Equal(new DateTime(2028, 2, 29, 10, 0, 0, DateTimeKind.Utc), next);
    }

    [Fact]
    public void CalculateNextOccurrence_Yearly_HandlesLeapDaySafe()
    {
        // Feb 29, 2028 + 1 year => Feb 28, 2029
        var due = new DateTime(2028, 2, 29, 9, 30, 0, DateTimeKind.Utc);
        var next = _deadlineService.CalculateNextOccurrence(due, RecurrenceRule.Yearly, interval: 1);

        Assert.Equal(new DateTime(2029, 2, 28, 9, 30, 0, DateTimeKind.Utc), next);
    }
}
