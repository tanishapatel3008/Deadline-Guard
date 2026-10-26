using DeadlineGuard.Data;
using DeadlineGuard.Models;
using DeadlineGuard.Models.Enums;
using DeadlineGuard.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DeadlineGuard.Services;

public interface IDeadlineService
{
    Task<Deadline> CreateDeadlineAsync(DeadlineFormViewModel model, string userId);
    Task<Deadline?> GetDeadlineByIdAsync(int id, string userId);
    Task<DeadlineFormViewModel?> GetDeadlineForEditAsync(int id, string userId);
    Task<bool> UpdateDeadlineAsync(DeadlineFormViewModel model, string userId);
    Task<bool> DeleteDeadlineAsync(int id, string userId);
    Task<bool> MarkCompletedAsync(int id, string userId, string? completionNotes);
    Task<bool> MoveInProgressAsync(int id, string userId);
    Task<bool> CancelDeadlineAsync(int id, string userId);
    Task<bool> ReopenDeadlineAsync(int id, string userId);
    Task<DeadlineListViewModel> GetFilteredDeadlinesAsync(string userId, DeadlineListViewModel filter);
    Task<List<Deadline>> GetActiveDeadlinesAsync(string userId);
    Task<DashboardViewModel> GetDashboardDataAsync(string userId);
    Task<List<DeadlineItemViewModel>> GetCalendarDeadlinesAsync(string userId, DateTime startUtc, DateTime endUtc);
    DateTime CalculateNextOccurrence(DateTime currentDue, RecurrenceRule rule, int interval);
}

public class DeadlineService : IDeadlineService
{
    private readonly ApplicationDbContext _context;
    private readonly IDeadlinePriorityService _priorityService;
    private readonly IDeadlineConflictService _conflictService;
    private readonly ILogger<DeadlineService> _logger;

    public DeadlineService(
        ApplicationDbContext context,
        IDeadlinePriorityService priorityService,
        IDeadlineConflictService conflictService,
        ILogger<DeadlineService> logger)
    {
        _context = context;
        _priorityService = priorityService;
        _conflictService = conflictService;
        _logger = logger;
    }

    public async Task<Deadline> CreateDeadlineAsync(DeadlineFormViewModel model, string userId)
    {
        var deadline = new Deadline
        {
            UserId = userId,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            Category = model.Category,
            DueDateTime = model.DueDateTime.ToUniversalTime(),
            EstimatedEffortMinutes = model.EstimatedEffortMinutes,
            ImportanceLevel = model.ImportanceLevel,
            ConsequenceLevel = model.ConsequenceLevel,
            ConsequenceType = model.ConsequenceType,
            PotentialPenaltyAmount = model.PotentialPenaltyAmount,
            CurrencyCode = string.IsNullOrWhiteSpace(model.CurrencyCode) ? "USD" : model.CurrencyCode.ToUpper().Trim(),
            Status = DeadlineStatus.Pending,
            IsRecurring = model.IsRecurring,
            RecurrenceRule = model.IsRecurring ? model.RecurrenceRule : RecurrenceRule.None,
            RecurrenceInterval = model.IsRecurring ? Math.Max(1, model.RecurrenceInterval) : 1,
            RecurrenceEndDate = model.IsRecurring && model.RecurrenceEndDate.HasValue ? model.RecurrenceEndDate.Value.ToUniversalTime() : null,
            CustomPriorityWeight = model.CustomPriorityWeight,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Deadlines.Add(deadline);
        await _context.SaveChangesAsync();

        // 1. Record history
        _context.DeadlineHistories.Add(new DeadlineHistory
        {
            DeadlineId = deadline.Id,
            UserId = userId,
            Action = "Created",
            NewValue = deadline.Title,
            ChangedAt = DateTime.UtcNow,
            Notes = $"Deadline scheduled for {deadline.DueDateTime:yyyy-MM-dd HH:mm} UTC."
        });

        // 2. Add requested reminder presets
        var reminderChannel = model.ReminderEmail ? ReminderChannel.Email : ReminderChannel.InApp;
        var due = deadline.DueDateTime;

        if (model.Reminder7Days && due.AddDays(-7) > DateTime.UtcNow)
        {
            _context.Reminders.Add(new Reminder
            {
                DeadlineId = deadline.Id,
                UserId = userId,
                ReminderTime = due.AddDays(-7),
                Channel = reminderChannel,
                Status = ReminderStatus.Pending
            });
        }

        if (model.Reminder3Days && due.AddDays(-3) > DateTime.UtcNow)
        {
            _context.Reminders.Add(new Reminder
            {
                DeadlineId = deadline.Id,
                UserId = userId,
                ReminderTime = due.AddDays(-3),
                Channel = reminderChannel,
                Status = ReminderStatus.Pending
            });
        }

        if (model.Reminder1Day && due.AddDays(-1) > DateTime.UtcNow)
        {
            _context.Reminders.Add(new Reminder
            {
                DeadlineId = deadline.Id,
                UserId = userId,
                ReminderTime = due.AddDays(-1),
                Channel = reminderChannel,
                Status = ReminderStatus.Pending
            });
        }

        if (model.Reminder1Hour && due.AddHours(-1) > DateTime.UtcNow)
        {
            _context.Reminders.Add(new Reminder
            {
                DeadlineId = deadline.Id,
                UserId = userId,
                ReminderTime = due.AddHours(-1),
                Channel = reminderChannel,
                Status = ReminderStatus.Pending
            });
        }

        await _context.SaveChangesAsync();
        return deadline;
    }

    public async Task<Deadline?> GetDeadlineByIdAsync(int id, string userId)
    {
        return await _context.Deadlines
            .Include(d => d.Attachments)
            .Include(d => d.Reminders)
            .Include(d => d.Histories.OrderByDescending(h => h.ChangedAt))
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
    }

    public async Task<DeadlineFormViewModel?> GetDeadlineForEditAsync(int id, string userId)
    {
        var d = await _context.Deadlines.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (d == null) return null;

        return new DeadlineFormViewModel
        {
            Id = d.Id,
            Title = d.Title,
            Description = d.Description,
            Category = d.Category,
            DueDateTime = d.DueDateTime,
            EstimatedEffortMinutes = d.EstimatedEffortMinutes,
            ImportanceLevel = d.ImportanceLevel,
            ConsequenceLevel = d.ConsequenceLevel,
            ConsequenceType = d.ConsequenceType,
            PotentialPenaltyAmount = d.PotentialPenaltyAmount,
            CurrencyCode = d.CurrencyCode,
            IsRecurring = d.IsRecurring,
            RecurrenceRule = d.RecurrenceRule,
            RecurrenceInterval = d.RecurrenceInterval,
            RecurrenceEndDate = d.RecurrenceEndDate,
            CustomPriorityWeight = d.CustomPriorityWeight
        };
    }

    public async Task<bool> UpdateDeadlineAsync(DeadlineFormViewModel model, string userId)
    {
        var deadline = await _context.Deadlines.FirstOrDefaultAsync(d => d.Id == model.Id && d.UserId == userId);
        if (deadline == null) return false;

        var changes = new List<string>();

        if (deadline.Title != model.Title.Trim())
        {
            changes.Add($"Title changed from '{deadline.Title}' to '{model.Title.Trim()}'");
            deadline.Title = model.Title.Trim();
        }

        var newDueUtc = model.DueDateTime.ToUniversalTime();
        if (deadline.DueDateTime != newDueUtc)
        {
            changes.Add($"Due date changed from {deadline.DueDateTime:yyyy-MM-dd HH:mm} UTC to {newDueUtc:yyyy-MM-dd HH:mm} UTC");
            deadline.DueDateTime = newDueUtc;
        }

        if (deadline.ConsequenceLevel != model.ConsequenceLevel)
        {
            changes.Add($"Consequence severity updated to {model.ConsequenceLevel}");
            deadline.ConsequenceLevel = model.ConsequenceLevel;
        }

        if (deadline.ImportanceLevel != model.ImportanceLevel)
        {
            changes.Add($"Importance level updated to {model.ImportanceLevel}");
            deadline.ImportanceLevel = model.ImportanceLevel;
        }

        if (deadline.EstimatedEffortMinutes != model.EstimatedEffortMinutes)
        {
            changes.Add($"Effort estimate changed to {model.EstimatedEffortMinutes} minutes");
            deadline.EstimatedEffortMinutes = model.EstimatedEffortMinutes;
        }

        deadline.Description = model.Description?.Trim();
        deadline.Category = model.Category;
        deadline.ConsequenceType = model.ConsequenceType;
        deadline.PotentialPenaltyAmount = model.PotentialPenaltyAmount;
        deadline.CurrencyCode = string.IsNullOrWhiteSpace(model.CurrencyCode) ? "USD" : model.CurrencyCode.ToUpper().Trim();
        deadline.IsRecurring = model.IsRecurring;
        deadline.RecurrenceRule = model.IsRecurring ? model.RecurrenceRule : RecurrenceRule.None;
        deadline.RecurrenceInterval = model.IsRecurring ? Math.Max(1, model.RecurrenceInterval) : 1;
        deadline.RecurrenceEndDate = model.IsRecurring && model.RecurrenceEndDate.HasValue ? model.RecurrenceEndDate.Value.ToUniversalTime() : null;
        deadline.CustomPriorityWeight = model.CustomPriorityWeight;
        deadline.UpdatedAt = DateTime.UtcNow;

        if (changes.Any())
        {
            _context.DeadlineHistories.Add(new DeadlineHistory
            {
                DeadlineId = deadline.Id,
                UserId = userId,
                Action = "Updated",
                NewValue = string.Join("; ", changes),
                ChangedAt = DateTime.UtcNow,
                Notes = "User modified deadline settings."
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDeadlineAsync(int id, string userId)
    {
        var deadline = await _context.Deadlines
            .Include(d => d.Attachments)
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

        if (deadline == null) return false;

        // Clean up attachment physical files
        foreach (var attachment in deadline.Attachments)
        {
            if (File.Exists(attachment.StoragePath))
            {
                try { File.Delete(attachment.StoragePath); } catch { /* ignore disk cleanup log */ }
            }
        }

        _context.Deadlines.Remove(deadline);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkCompletedAsync(int id, string userId, string? completionNotes)
    {
        var deadline = await _context.Deadlines.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        if (deadline == null) return false;

        var now = DateTime.UtcNow;
        deadline.Status = DeadlineStatus.Completed;
        deadline.LastCompletedAt = now;
        deadline.CompletionNotes = completionNotes?.Trim();
        deadline.UpdatedAt = now;

        _context.DeadlineHistories.Add(new DeadlineHistory
        {
            DeadlineId = deadline.Id,
            UserId = userId,
            Action = "Completed",
            NewValue = "Completed",
            ChangedAt = now,
            Notes = completionNotes ?? "Marked as completed."
        });

        // Recurring Deadlines: generate the next occurrence if recurring
        if (deadline.IsRecurring && deadline.RecurrenceRule != RecurrenceRule.None)
        {
            var nextDue = CalculateNextOccurrence(deadline.DueDateTime, deadline.RecurrenceRule, deadline.RecurrenceInterval);

            // Verify recurrence end date
            if (!deadline.RecurrenceEndDate.HasValue || nextDue <= deadline.RecurrenceEndDate.Value)
            {
                var nextOccurrence = new Deadline
                {
                    UserId = userId,
                    Title = deadline.Title,
                    Description = deadline.Description,
                    Category = deadline.Category,
                    DueDateTime = nextDue,
                    EstimatedEffortMinutes = deadline.EstimatedEffortMinutes,
                    ImportanceLevel = deadline.ImportanceLevel,
                    ConsequenceLevel = deadline.ConsequenceLevel,
                    ConsequenceType = deadline.ConsequenceType,
                    PotentialPenaltyAmount = deadline.PotentialPenaltyAmount,
                    CurrencyCode = deadline.CurrencyCode,
                    Status = DeadlineStatus.Pending,
                    IsRecurring = true,
                    RecurrenceRule = deadline.RecurrenceRule,
                    RecurrenceInterval = deadline.RecurrenceInterval,
                    RecurrenceEndDate = deadline.RecurrenceEndDate,
                    CustomPriorityWeight = deadline.CustomPriorityWeight,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Deadlines.Add(nextOccurrence);
                await _context.SaveChangesAsync();

                _context.DeadlineHistories.Add(new DeadlineHistory
                {
                    DeadlineId = nextOccurrence.Id,
                    UserId = userId,
                    Action = "RecurrenceGenerated",
                    NewValue = nextOccurrence.Title,
                    ChangedAt = now,
                    Notes = $"Scheduled automatically following completion of previous cycle (due {nextDue:yyyy-MM-dd HH:mm} UTC)."
                });

                // Attach 1-day reminder by default to next occurrence
                if (nextDue.AddDays(-1) > now)
                {
                    _context.Reminders.Add(new Reminder
                    {
                        DeadlineId = nextOccurrence.Id,
                        UserId = userId,
                        ReminderTime = nextDue.AddDays(-1),
                        Channel = ReminderChannel.InApp,
                        Status = ReminderStatus.Pending
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MoveInProgressAsync(int id, string userId)
    {
        var deadline = await _context.Deadlines.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        if (deadline == null) return false;

        deadline.Status = DeadlineStatus.InProgress;
        deadline.UpdatedAt = DateTime.UtcNow;

        _context.DeadlineHistories.Add(new DeadlineHistory
        {
            DeadlineId = deadline.Id,
            UserId = userId,
            Action = "StatusChanged",
            NewValue = "InProgress",
            ChangedAt = DateTime.UtcNow,
            Notes = "Task moved into progress."
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelDeadlineAsync(int id, string userId)
    {
        var deadline = await _context.Deadlines.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        if (deadline == null) return false;

        deadline.Status = DeadlineStatus.Cancelled;
        deadline.UpdatedAt = DateTime.UtcNow;

        _context.DeadlineHistories.Add(new DeadlineHistory
        {
            DeadlineId = deadline.Id,
            UserId = userId,
            Action = "StatusChanged",
            NewValue = "Cancelled",
            ChangedAt = DateTime.UtcNow,
            Notes = "Deadline cancelled by user."
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReopenDeadlineAsync(int id, string userId)
    {
        var deadline = await _context.Deadlines.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        if (deadline == null) return false;

        deadline.Status = DeadlineStatus.Pending;
        deadline.UpdatedAt = DateTime.UtcNow;

        _context.DeadlineHistories.Add(new DeadlineHistory
        {
            DeadlineId = deadline.Id,
            UserId = userId,
            Action = "StatusChanged",
            NewValue = "Pending",
            ChangedAt = DateTime.UtcNow,
            Notes = "Re-opened deadline."
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Deadline>> GetActiveDeadlinesAsync(string userId)
    {
        return await _context.Deadlines
            .Where(d => d.UserId == userId && d.Status != DeadlineStatus.Completed && d.Status != DeadlineStatus.Cancelled)
            .OrderBy(d => d.DueDateTime)
            .ToListAsync();
    }

    public async Task<DeadlineListViewModel> GetFilteredDeadlinesAsync(string userId, DeadlineListViewModel filter)
    {
        var now = DateTime.UtcNow;
        var query = _context.Deadlines.Where(d => d.UserId == userId);

        // Search
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(d => d.Title.ToLower().Contains(term) || (d.Description != null && d.Description.ToLower().Contains(term)));
        }

        // Category filter
        if (filter.CategoryFilter.HasValue)
        {
            query = query.Where(d => d.Category == filter.CategoryFilter.Value);
        }

        // Status filter
        if (filter.StatusFilter.HasValue)
        {
            query = query.Where(d => d.Status == filter.StatusFilter.Value);
        }

        // Date filter
        switch (filter.DateFilter?.ToLower())
        {
            case "today":
                var endOfToday = now.Date.AddDays(1);
                query = query.Where(d => d.DueDateTime >= now.Date && d.DueDateTime < endOfToday);
                break;
            case "next7":
                var next7Days = now.AddDays(7);
                query = query.Where(d => d.DueDateTime >= now && d.DueDateTime <= next7Days);
                break;
            case "overdue":
                query = query.Where(d => d.DueDateTime < now && d.Status != DeadlineStatus.Completed && d.Status != DeadlineStatus.Cancelled);
                break;
            case "month":
                var endOfMonth = now.AddMonths(1);
                query = query.Where(d => d.DueDateTime >= now && d.DueDateTime <= endOfMonth);
                break;
        }

        var allMatching = await query.ToListAsync();

        // Evaluate priority for each deadline
        var evaluated = allMatching.Select(d => new DeadlineItemViewModel
        {
            Deadline = d,
            Priority = _priorityService.CalculatePriority(d, now)
        }).ToList();

        // Priority filter
        if (filter.PriorityFilter.HasValue)
        {
            evaluated = evaluated.Where(e => e.Priority.Classification == filter.PriorityFilter.Value).ToList();
        }

        // Sort
        evaluated = filter.SortBy?.ToLower() switch
        {
            "duedate" => evaluated.OrderBy(e => e.Deadline.DueDateTime).ToList(),
            "consequence" => evaluated.OrderByDescending(e => e.Priority.ConsequenceScore).ThenBy(e => e.Deadline.DueDateTime).ToList(),
            "effort" => evaluated.OrderByDescending(e => e.Deadline.EstimatedEffortMinutes).ThenBy(e => e.Deadline.DueDateTime).ToList(),
            _ => evaluated.OrderByDescending(e => e.Priority.Score).ThenBy(e => e.Deadline.DueDateTime).ToList()
        };

        filter.TotalItems = evaluated.Count;
        filter.CurrentPage = Math.Max(1, filter.CurrentPage);
        filter.PageSize = Math.Max(5, filter.PageSize);

        filter.Deadlines = evaluated
            .Skip((filter.CurrentPage - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return filter;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var allUserDeadlines = await _context.Deadlines
            .Where(d => d.UserId == userId)
            .ToListAsync();

        var userPrefs = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        int prepLimit = userPrefs?.AvailableDailyPrepMinutes ?? 240;

        var activeDeadlines = allUserDeadlines
            .Where(d => d.Status != DeadlineStatus.Completed && d.Status != DeadlineStatus.Cancelled)
            .ToList();

        var rankedActive = _priorityService.RankDeadlines(activeDeadlines, now)
            .Select(x => new DeadlineItemViewModel
            {
                Deadline = x.Deadline,
                Priority = x.Evaluation
            })
            .ToList();

        var todayEnd = now.Date.AddDays(1);
        var next7 = now.AddDays(7);

        var model = new DashboardViewModel
        {
            ActiveDeadlinesCount = activeDeadlines.Count,
            DueTodayCount = activeDeadlines.Count(d => d.DueDateTime >= now.Date && d.DueDateTime < todayEnd),
            DueNext7DaysCount = activeDeadlines.Count(d => d.DueDateTime >= now && d.DueDateTime <= next7),
            OverdueCount = activeDeadlines.Count(d => d.DueDateTime < now),
            CompletedCount = allUserDeadlines.Count(d => d.Status == DeadlineStatus.Completed),
            CriticalHighCount = rankedActive.Count(r => r.Priority.Classification == PriorityClassification.Critical || r.Priority.Classification == PriorityClassification.High),
            TodayActionPlan = rankedActive.Take(6).ToList(),
            ActiveConflicts = _conflictService.DetectConflicts(activeDeadlines, prepLimit, now),
            UpcomingRecurring = activeDeadlines.Where(d => d.IsRecurring).Take(5).ToList(),
            RecentActivity = await _context.DeadlineHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.ChangedAt)
                .Take(7)
                .ToListAsync(),
            UnreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.ReadAt == null)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync(),
            UserTimeZone = userPrefs?.DefaultTimeZone ?? "UTC"
        };

        // Category distribution
        var categoryGroups = activeDeadlines.GroupBy(d => d.Category.ToString()).ToList();
        model.CategoryLabels = categoryGroups.Select(g => g.Key).ToList();
        model.CategoryCounts = categoryGroups.Select(g => g.Count()).ToList();

        // Priority distribution
        model.PriorityCounts = new List<int>
        {
            rankedActive.Count(r => r.Priority.Classification == PriorityClassification.Critical),
            rankedActive.Count(r => r.Priority.Classification == PriorityClassification.High),
            rankedActive.Count(r => r.Priority.Classification == PriorityClassification.Medium),
            rankedActive.Count(r => r.Priority.Classification == PriorityClassification.Low)
        };

        // Completion trend (last 6 months)
        var sixMonthsAgo = now.AddMonths(-5);
        var completedDeadlines = allUserDeadlines
            .Where(d => d.Status == DeadlineStatus.Completed && d.LastCompletedAt >= new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1))
            .ToList();

        for (int i = 5; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            model.CompletionTrendLabels.Add(monthDate.ToString("MMM yyyy"));
            var count = completedDeadlines.Count(d => d.LastCompletedAt.HasValue &&
                                                     d.LastCompletedAt.Value.Year == monthDate.Year &&
                                                     d.LastCompletedAt.Value.Month == monthDate.Month);
            model.CompletionTrendCounts.Add(count);
        }

        return model;
    }

    public async Task<List<DeadlineItemViewModel>> GetCalendarDeadlinesAsync(string userId, DateTime startUtc, DateTime endUtc)
    {
        var now = DateTime.UtcNow;
        var deadlines = await _context.Deadlines
            .Where(d => d.UserId == userId && d.DueDateTime >= startUtc && d.DueDateTime <= endUtc)
            .OrderBy(d => d.DueDateTime)
            .ToListAsync();

        return deadlines.Select(d => new DeadlineItemViewModel
        {
            Deadline = d,
            Priority = _priorityService.CalculatePriority(d, now)
        }).ToList();
    }

    public DateTime CalculateNextOccurrence(DateTime currentDue, RecurrenceRule rule, int interval)
    {
        interval = Math.Max(1, interval);

        return rule switch
        {
            RecurrenceRule.Daily => currentDue.AddDays(interval),
            RecurrenceRule.Weekly => currentDue.AddDays(7 * interval),
            RecurrenceRule.Monthly => AddMonthsSafe(currentDue, interval),
            RecurrenceRule.Yearly => AddYearsSafe(currentDue, interval),
            RecurrenceRule.CustomDays => currentDue.AddDays(interval),
            _ => currentDue.AddDays(interval)
        };
    }

    private static DateTime AddMonthsSafe(DateTime date, int months)
    {
        int targetYear = date.Year + ((date.Month - 1 + months) / 12);
        int targetMonth = ((date.Month - 1 + months) % 12) + 1;
        int maxDaysInTargetMonth = DateTime.DaysInMonth(targetYear, targetMonth);
        int targetDay = Math.Min(date.Day, maxDaysInTargetMonth);

        return new DateTime(targetYear, targetMonth, targetDay, date.Hour, date.Minute, date.Second, date.Kind);
    }

    private static DateTime AddYearsSafe(DateTime date, int years)
    {
        int targetYear = date.Year + years;
        int targetMonth = date.Month;
        int maxDaysInTargetMonth = DateTime.DaysInMonth(targetYear, targetMonth);
        int targetDay = Math.Min(date.Day, maxDaysInTargetMonth);

        return new DateTime(targetYear, targetMonth, targetDay, date.Hour, date.Minute, date.Second, date.Kind);
    }
}
