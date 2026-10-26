using System.ComponentModel.DataAnnotations;
using DeadlineGuard.Data;
using DeadlineGuard.Models;
using DeadlineGuard.Models.Enums;
using DeadlineGuard.Services;
using DeadlineGuard.ViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DeadlineGuard.Tests;

public class ValidationAndReminderTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void DeadlineFormViewModel_InvalidWhenRecurringWithoutRule()
    {
        // Arrange
        var model = new DeadlineFormViewModel
        {
            Title = "Invalid Recurring Task",
            DueDateTime = DateTime.UtcNow.AddDays(2),
            EstimatedEffortMinutes = 60,
            IsRecurring = true,
            RecurrenceRule = RecurrenceRule.None // Invalid: is recurring but rule is None
        };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();

        // Act
        bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);
        var customResults = model.Validate(validationContext).ToList();

        // Assert
        Assert.Contains(customResults, r => r.ErrorMessage!.Contains("Please select a recurrence rule"));
    }

    [Fact]
    public void DeadlineFormViewModel_InvalidWhenFinancialPenaltyWithoutAmount()
    {
        // Arrange
        var model = new DeadlineFormViewModel
        {
            Title = "Traffic Fine",
            DueDateTime = DateTime.UtcNow.AddDays(5),
            EstimatedEffortMinutes = 15,
            ConsequenceType = ConsequenceType.FinancialPenalty,
            PotentialPenaltyAmount = null // Invalid: selected financial penalty but omitted amount
        };

        var validationContext = new ValidationContext(model);

        // Act
        var customResults = model.Validate(validationContext).ToList();

        // Assert
        Assert.Contains(customResults, r => r.ErrorMessage!.Contains("Please specify a potential penalty amount"));
    }

    [Fact]
    public async Task ReminderService_AddReminder_PreventsDuplicatesForSameTimeAndChannel()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new ReminderService(context);

        var deadline = new Deadline
        {
            Id = 1,
            UserId = "user-1",
            Title = "Tax Deadline",
            DueDateTime = DateTime.UtcNow.AddDays(3),
            Status = DeadlineStatus.Pending
        };
        context.Deadlines.Add(deadline);
        await context.SaveChangesAsync();

        var reminderTime = DateTime.UtcNow.AddDays(2);

        // Act
        var first = await service.AddReminderAsync(deadline.Id, "user-1", reminderTime, ReminderChannel.InApp);
        var duplicate = await service.AddReminderAsync(deadline.Id, "user-1", reminderTime, ReminderChannel.InApp);

        // Assert
        Assert.NotNull(first);
        Assert.NotNull(duplicate);
        Assert.Equal(first.Id, duplicate.Id); // Should return the existing reminder without creating a duplicate record

        var totalReminders = await context.Reminders.Where(r => r.DeadlineId == deadline.Id).CountAsync();
        Assert.Equal(1, totalReminders);
    }
}
