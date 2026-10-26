using DeadlineGuard.Data;
using DeadlineGuard.Models.Enums;
using DeadlineGuard.Services;
using Microsoft.EntityFrameworkCore;

namespace DeadlineGuard.BackgroundJobs;

public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReminderBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
    private const int MaxRetries = 3;

    public ReminderBackgroundService(IServiceProvider serviceProvider, ILogger<ReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderBackgroundService started. Polling every {Interval} seconds.", _checkInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during reminder background processing cycle.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("ReminderBackgroundService is stopping.");
    }

    public async Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var priorityService = scope.ServiceProvider.GetRequiredService<IDeadlinePriorityService>();
        var now = DateTime.UtcNow;

        // Fetch pending enabled reminders that are due
        var dueReminders = await context.Reminders
            .Include(r => r.Deadline)
            .Include(r => r.User)
                .ThenInclude(u => u!.Preferences)
            .Where(r => r.IsEnabled &&
                        r.Status == ReminderStatus.Pending &&
                        r.ReminderTime <= now)
            .OrderBy(r => r.ReminderTime)
            .Take(50) // Process in bounded batches
            .ToListAsync(cancellationToken);

        if (!dueReminders.Any()) return;

        _logger.LogInformation("Processing {Count} due reminders.", dueReminders.Count);

        foreach (var reminder in dueReminders)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var deadline = reminder.Deadline;
            var user = reminder.User;

            if (deadline == null || user == null)
            {
                reminder.Status = ReminderStatus.Skipped;
                reminder.FailureReason = "Associated deadline or user record missing.";
                continue;
            }

            // If deadline is already completed or cancelled, skip
            if (deadline.Status == DeadlineStatus.Completed || deadline.Status == DeadlineStatus.Cancelled)
            {
                reminder.Status = ReminderStatus.Skipped;
                reminder.FailureReason = $"Deadline is already {deadline.Status}.";
                continue;
            }

            reminder.LastAttemptAt = now;

            // Idempotency check: verify notification hasn't already been created for this exact reminder occurrence
            bool notificationExists = await context.Notifications.AnyAsync(n =>
                n.UserId == reminder.UserId &&
                n.DeadlineId == reminder.DeadlineId &&
                n.CreatedAt >= reminder.ReminderTime.AddMinutes(-10) &&
                n.CreatedAt <= reminder.ReminderTime.AddMinutes(10) &&
                n.NotificationType == "Reminder", cancellationToken);

            var eval = priorityService.CalculatePriority(deadline, now);

            try
            {
                // In-App Notification
                if (!notificationExists)
                {
                    var notifTitle = $"Reminder: {deadline.Title}";
                    var notifMessage = $"{eval.Explanation} Due on {deadline.DueDateTime:MMM dd, yyyy HH:mm} UTC. {eval.SuggestedAction}";

                    context.Notifications.Add(new Models.Notification
                    {
                        UserId = reminder.UserId,
                        DeadlineId = reminder.DeadlineId,
                        Title = notifTitle,
                        Message = notifMessage,
                        NotificationType = "Reminder",
                        CreatedAt = now
                    });
                }

                // Email Notification if Email channel
                if (reminder.Channel == ReminderChannel.Email)
                {
                    bool emailEnabled = user.Preferences?.EmailNotificationsEnabled ?? false;
                    if (emailEnabled && !string.IsNullOrWhiteSpace(user.Email))
                    {
                        var emailResult = await emailService.SendReminderEmailAsync(
                            user.Email,
                            user.FullName,
                            deadline.Title,
                            deadline.DueDateTime,
                            eval.ConsequenceSummary,
                            eval.Explanation);

                        if (!emailResult.Succeeded && !emailResult.WasSkippedDueToConfig)
                        {
                            reminder.RetryCount++;
                            if (reminder.RetryCount >= MaxRetries)
                            {
                                reminder.Status = ReminderStatus.Failed;
                                reminder.FailureReason = $"Exceeded max retries. Last error: {emailResult.ErrorMessage}";
                            }
                            else
                            {
                                reminder.FailureReason = $"Email dispatch attempt failed: {emailResult.ErrorMessage}";
                            }
                            continue;
                        }
                    }
                }

                reminder.Status = ReminderStatus.Sent;
                reminder.SentAt = now;
                reminder.FailureReason = null;
            }
            catch (Exception ex)
            {
                reminder.RetryCount++;
                reminder.LastAttemptAt = now;
                reminder.FailureReason = ex.Message;

                if (reminder.RetryCount >= MaxRetries)
                {
                    reminder.Status = ReminderStatus.Failed;
                }

                _logger.LogError(ex, "Error processing reminder {ReminderId} for deadline {DeadlineId}", reminder.Id, reminder.DeadlineId);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
