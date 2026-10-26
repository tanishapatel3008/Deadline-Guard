
using DeadlineGuard.Models;
using DeadlineGuard.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DeadlineGuard.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DbInitializer");

        // Ensure database is created/migrated
        await context.Database.MigrateAsync();

        // Read demo password from an environment variable, not source code.
        var demoPassword = Environment.GetEnvironmentVariable(
            "DEADLINEGUARD_DEMO_PASSWORD");

        if (string.IsNullOrWhiteSpace(demoPassword))
        {
            logger.LogInformation(
                "Demo seeding skipped. Set DEADLINEGUARD_DEMO_PASSWORD to enable it.");
            return;
        }

        var demoEmail = "demo@deadlineguard.com";
        var demoUser = await userManager.FindByEmailAsync(demoEmail);

        if (demoUser == null)
        {
            demoUser = new ApplicationUser
            {
                UserName = demoEmail,
                Email = demoEmail,
                FullName = "Alex Mercer",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                TimeZoneId = "UTC",
                PreferredTheme = "light",
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(demoUser, demoPassword);

            if (!createResult.Succeeded)
            {
                logger.LogError(
                    "Failed to create demo user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            var preferences = new UserPreferences
            {
                UserId = demoUser.Id,
                DefaultReminderMinutes = 1440,
                EmailNotificationsEnabled = false,
                InAppNotificationsEnabled = true,
                DailySummaryEnabled = true,
                DailySummaryTime = new TimeSpan(9, 0, 0),
                DefaultTimeZone = "UTC",
                AvailableDailyPrepMinutes = 240,
                UpdatedAt = DateTime.UtcNow
            };

            context.UserPreferences.Add(preferences);
            await context.SaveChangesAsync();
        }

        if (!await context.Deadlines.AnyAsync(d => d.UserId == demoUser.Id))
        {
            var now = DateTime.UtcNow;

            var deadlines = new List<Deadline>
            {
                new()
                {
                    UserId = demoUser.Id,
                    Title = "Q3 Estimated Federal Tax Filing",
                    Description = "Submit quarterly estimated tax return to the IRS along with state income tax schedule.",
                    Category = DeadlineCategory.Bills,
                    DueDateTime = now.AddHours(18),
                    EstimatedEffortMinutes = 180,
                    ImportanceLevel = ImportanceLevel.Critical,
                    ConsequenceLevel = ConsequenceLevel.Severe,
                    ConsequenceType = ConsequenceType.FinancialPenalty,
                    PotentialPenaltyAmount = 450.00m,
                    CurrencyCode = "USD",
                    Status = DeadlineStatus.InProgress,
                    CreatedAt = now.AddDays(-7),
                    UpdatedAt = now.AddDays(-1)
                },
                new()
                {
                    UserId = demoUser.Id,
                    Title = "Master's Degree Program Application",
                    Description = "Submit final statement of purpose, academic transcripts, and letters of recommendation to Stanford Graduate Admissions.",
                    Category = DeadlineCategory.Applications,
                    DueDateTime = now.AddDays(2).AddHours(4),
                    EstimatedEffortMinutes = 240,
                    ImportanceLevel = ImportanceLevel.Critical,
                    ConsequenceLevel = ConsequenceLevel.Severe,
                    ConsequenceType = ConsequenceType.LostOpportunity,
                    PotentialPenaltyAmount = null,
                    CurrencyCode = "USD",
                    Status = DeadlineStatus.InProgress,
                    CreatedAt = now.AddDays(-14),
                    UpdatedAt = now.AddDays(-2)
                },
                new()
                {
                    UserId = demoUser.Id,
                    Title = "Automobile Comprehensive Insurance Renewal",
                    Description = "Renew policy #AU-883921 with State Farm before midnight to avoid lapse in mandatory coverage.",
                    Category = DeadlineCategory.Renewals,
                    DueDateTime = now.AddDays(3).AddHours(6),
                    EstimatedEffortMinutes = 30,
                    ImportanceLevel = ImportanceLevel.High,
                    ConsequenceLevel = ConsequenceLevel.Significant,
                    ConsequenceType = ConsequenceType.LegalAdministrative,
                    PotentialPenaltyAmount = 150.00m,
                    CurrencyCode = "USD",
                    Status = DeadlineStatus.Pending,
                    CreatedAt = now.AddDays(-5),
                    UpdatedAt = now.AddDays(-5)
                },
                new()
                {
                    UserId = demoUser.Id,
                    Title = "Passport Renewal Submission (Form DS-82)",
                    Description = "Mail passport renewal package with passport photos and cashier's check before international travel in 6 months.",
                    Category = DeadlineCategory.GovernmentDocuments,
                    DueDateTime = now.AddDays(12),
                    EstimatedEffortMinutes = 60,
                    ImportanceLevel = ImportanceLevel.High,
                    ConsequenceLevel = ConsequenceLevel.Significant,
                    ConsequenceType = ConsequenceType.ServiceInterruption,
                    PotentialPenaltyAmount = 60.00m,
                    CurrencyCode = "USD",
                    Status = DeadlineStatus.Pending,
                    CreatedAt = now.AddDays(-3),
                    UpdatedAt = now.AddDays(-3)
                },
                new()
                {
                    UserId = demoUser.Id,
                    Title = "AWS Certified Solutions Architect Exam",
                    Description = "Complete scheduled Pearson VUE remote proctored certification exam.",
                    Category = DeadlineCategory.Education,
                    DueDateTime = now.AddDays(5).AddHours(2),
                    EstimatedEffortMinutes = 300,
                    ImportanceLevel = ImportanceLevel.High,
                    ConsequenceLevel = ConsequenceLevel.Moderate,
                    ConsequenceType = ConsequenceType.LostOpportunity,
                    PotentialPenaltyAmount = 150.00m,
                    CurrencyCode = "USD",
                    Status = DeadlineStatus.Pending,
                    CreatedAt = now.AddDays(-10),
                    UpdatedAt = now.AddDays(-1)
                },
                new()
                {
                    UserId = demoUser.Id,
                    Title = "Electric Utility Bill Payment",
                    Description = "Pay monthly power utility bill to City Light & Power via online portal.",
                    Category = DeadlineCategory.Bills,
                    DueDateTime = now.AddDays(6),
                    EstimatedEffortMinutes = 15,
                    ImportanceLevel = ImportanceLevel.Medium,
                    ConsequenceLevel = ConsequenceLevel.Moderate,
                    ConsequenceType = ConsequenceType.FinancialPenalty,
                    PotentialPenaltyAmount = 25.00m,
                    CurrencyCode = "USD",
                    Status = DeadlineStatus.Pending,
                    IsRecurring = true,
                    RecurrenceRule = RecurrenceRule.Monthly,
                    RecurrenceInterval = 1,
                    CreatedAt = now.AddDays(-1),
                    UpdatedAt = now.AddDays(-1)
                },
                new()
                {
                    UserId = demoUser.Id,
                    Title = "Municipal Property Tax Assessment Appeal",
                    Description = "Submit formal grievance packet to the County Board of Assessors.",
                    Category = DeadlineCategory.GovernmentDocuments,
                    DueDateTime = now.AddHours(-14),
                    EstimatedEffortMinutes = 90,
                    ImportanceLevel = ImportanceLevel.High,
                    ConsequenceLevel = ConsequenceLevel.Severe,
                    ConsequenceType = ConsequenceType.FinancialPenalty,
                    PotentialPenaltyAmount = 850.00m,
                    CurrencyCode = "USD",
                    Status = DeadlineStatus.Pending,
                    CreatedAt = now.AddDays(-20),
                    UpdatedAt = now.AddHours(-14)
                },
                new()
                {
                    UserId = demoUser.Id,
                    Title = "Quarterly Software Engineering OKR Review",
                    Description = "Submit self-evaluation and project deliverables for Q2 team review.",
                    Category = DeadlineCategory.Work,
                    DueDateTime = now.AddDays(-3),
                    EstimatedEffortMinutes = 120,
                    ImportanceLevel = ImportanceLevel.Medium,
                    ConsequenceLevel = ConsequenceLevel.Moderate,
                    ConsequenceType = ConsequenceType.OrdinaryInconvenience,
                    PotentialPenaltyAmount = null,
                    CurrencyCode = "USD",
                    Status = DeadlineStatus.Completed,
                    LastCompletedAt = now.AddDays(-3).AddHours(2),
                    CompletionNotes = "Completed on time. All metrics and engineering pull requests were linked.",
                    CreatedAt = now.AddDays(-15),
                    UpdatedAt = now.AddDays(-3)
                }
            };

            context.Deadlines.AddRange(deadlines);
            await context.SaveChangesAsync();

            foreach (var deadline in deadlines.Where(
                d => d.Status != DeadlineStatus.Completed))
            {
                context.Reminders.Add(new Reminder
                {
                    DeadlineId = deadline.Id,
                    UserId = demoUser.Id,
                    ReminderTime = deadline.DueDateTime.AddHours(-12),
                    Channel = ReminderChannel.InApp,
                    IsEnabled = true,
                    Status = ReminderStatus.Pending,
                    CreatedAt = now
                });

                context.DeadlineHistories.Add(new DeadlineHistory
                {
                    DeadlineId = deadline.Id,
                    UserId = demoUser.Id,
                    Action = "Created",
                    NewValue = deadline.Title,
                    ChangedAt = deadline.CreatedAt,
                    Notes = "Initial deadline record created during onboarding."
                });
            }

            context.Notifications.Add(new Notification
            {
                UserId = demoUser.Id,
                Title = "Welcome to DeadlineGuard!",
                Message = "Your intelligent deadline and penalty guardian is active. Track consequences, prevent penalties, and keep your priorities crystal clear.",
                NotificationType = "System",
                CreatedAt = now.AddDays(-2),
                ReadAt = now.AddDays(-1)
            });

            context.Notifications.Add(new Notification
            {
                UserId = demoUser.Id,
                Title = "Urgent: Q3 Estimated Federal Tax Filing due soon",
                Message = "Your estimated tax filing is due in less than 24 hours. Potential late penalty: $450.00.",
                NotificationType = "UrgencyWarning",
                CreatedAt = now.AddHours(-2),
                ReadAt = null
            });

            await context.SaveChangesAsync();
            logger.LogInformation(
                "Database seeded successfully with demo user and sample deadlines.");
        }
    }
}
