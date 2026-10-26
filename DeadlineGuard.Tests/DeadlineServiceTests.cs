using DeadlineGuard.Data;
using DeadlineGuard.Models;
using DeadlineGuard.Models.Enums;
using DeadlineGuard.Services;
using DeadlineGuard.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeadlineGuard.Tests;

public class DeadlineServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateDeadlineAsync_CreatesDeadline_AndAuditHistoryAndReminders()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var priorityService = new DeadlinePriorityService();
        var conflictService = new DeadlineConflictService();
        var loggerMock = new Mock<ILogger<DeadlineService>>();

        var service = new DeadlineService(context, priorityService, conflictService, loggerMock.Object);
        var userId = "test-user-1";

        var form = new DeadlineFormViewModel
        {
            Title = "Electric Bill Payment",
            Description = "Pay electric utility bill online",
            Category = DeadlineCategory.Bills,
            DueDateTime = DateTime.UtcNow.AddDays(5),
            EstimatedEffortMinutes = 20,
            ConsequenceLevel = ConsequenceLevel.Moderate,
            ImportanceLevel = ImportanceLevel.Medium,
            Reminder1Day = true,
            Reminder1Hour = true
        };

        // Act
        var created = await service.CreateDeadlineAsync(form, userId);

        // Assert
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        var dbDeadline = await context.Deadlines.FindAsync(created.Id);
        Assert.NotNull(dbDeadline);
        Assert.Equal("Electric Bill Payment", dbDeadline.Title);
        Assert.Equal(userId, dbDeadline.UserId);

        var histories = await context.DeadlineHistories.Where(h => h.DeadlineId == created.Id).ToListAsync();
        Assert.Single(histories);
        Assert.Equal("Created", histories[0].Action);

        var reminders = await context.Reminders.Where(r => r.DeadlineId == created.Id).ToListAsync();
        Assert.NotEmpty(reminders);
    }

    [Fact]
    public async Task MarkCompletedAsync_RecurringDeadline_GeneratesNextOccurrenceAndPreservesHistory()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var priorityService = new DeadlinePriorityService();
        var conflictService = new DeadlineConflictService();
        var loggerMock = new Mock<ILogger<DeadlineService>>();

        var service = new DeadlineService(context, priorityService, conflictService, loggerMock.Object);
        var userId = "test-user-recurring";

        var initialDue = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var deadline = new Deadline
        {
            UserId = userId,
            Title = "Monthly Rent Payment",
            Category = DeadlineCategory.Bills,
            DueDateTime = initialDue,
            EstimatedEffortMinutes = 15,
            ImportanceLevel = ImportanceLevel.Critical,
            ConsequenceLevel = ConsequenceLevel.Severe,
            Status = DeadlineStatus.Pending,
            IsRecurring = true,
            RecurrenceRule = RecurrenceRule.Monthly,
            RecurrenceInterval = 1
        };

        context.Deadlines.Add(deadline);
        await context.SaveChangesAsync();

        // Act
        var result = await service.MarkCompletedAsync(deadline.Id, userId, "Paid via direct bank transfer");

        // Assert
        Assert.True(result);

        // Verify original deadline status
        var original = await context.Deadlines.FindAsync(deadline.Id);
        Assert.NotNull(original);
        Assert.Equal(DeadlineStatus.Completed, original.Status);
        Assert.NotNull(original.LastCompletedAt);
        Assert.Equal("Paid via direct bank transfer", original.CompletionNotes);

        // Verify next occurrence was generated
        var userDeadlines = await context.Deadlines.Where(d => d.UserId == userId).ToListAsync();
        Assert.Equal(2, userDeadlines.Count);

        var nextOccurrence = userDeadlines.FirstOrDefault(d => d.Id != deadline.Id);
        Assert.NotNull(nextOccurrence);
        Assert.Equal(DeadlineStatus.Pending, nextOccurrence.Status);
        Assert.Equal(new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc), nextOccurrence.DueDateTime);
    }

    [Fact]
    public async Task GetDeadlineByIdAsync_EnforcesUserIsolation()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var priorityService = new DeadlinePriorityService();
        var conflictService = new DeadlineConflictService();
        var loggerMock = new Mock<ILogger<DeadlineService>>();

        var service = new DeadlineService(context, priorityService, conflictService, loggerMock.Object);

        var d1 = new Deadline { UserId = "user-A", Title = "User A Private Deadline", Status = DeadlineStatus.Pending };
        context.Deadlines.Add(d1);
        await context.SaveChangesAsync();

        // Act
        var unauthorizedAccess = await service.GetDeadlineByIdAsync(d1.Id, "user-B");
        var authorizedAccess = await service.GetDeadlineByIdAsync(d1.Id, "user-A");

        // Assert
        Assert.Null(unauthorizedAccess);
        Assert.NotNull(authorizedAccess);
        Assert.Equal(d1.Id, authorizedAccess.Id);
    }
}
