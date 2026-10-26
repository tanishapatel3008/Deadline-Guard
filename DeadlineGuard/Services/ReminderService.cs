using DeadlineGuard.Data;
using DeadlineGuard.Models;
using DeadlineGuard.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeadlineGuard.Services;

public interface IReminderService
{
    Task<Reminder?> AddReminderAsync(int deadlineId, string userId, DateTime reminderTimeUtc, ReminderChannel channel);
    Task<bool> DeleteReminderAsync(int reminderId, string userId);
    Task<bool> ToggleReminderAsync(int reminderId, string userId);
    Task<List<Reminder>> GetRemindersForDeadlineAsync(int deadlineId, string userId);
}

public class ReminderService : IReminderService
{
    private readonly ApplicationDbContext _context;

    public ReminderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Reminder?> AddReminderAsync(int deadlineId, string userId, DateTime reminderTimeUtc, ReminderChannel channel)
    {
        var deadline = await _context.Deadlines.FirstOrDefaultAsync(d => d.Id == deadlineId && d.UserId == userId);
        if (deadline == null) return null;

        // Prevent duplicate reminder for the same time & channel
        var existing = await _context.Reminders.FirstOrDefaultAsync(r =>
            r.DeadlineId == deadlineId &&
            r.ReminderTime == reminderTimeUtc &&
            r.Channel == channel);

        if (existing != null)
        {
            existing.IsEnabled = true;
            existing.Status = ReminderStatus.Pending;
            await _context.SaveChangesAsync();
            return existing;
        }

        var reminder = new Reminder
        {
            DeadlineId = deadlineId,
            UserId = userId,
            ReminderTime = reminderTimeUtc,
            Channel = channel,
            IsEnabled = true,
            Status = ReminderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reminders.Add(reminder);
        await _context.SaveChangesAsync();
        return reminder;
    }

    public async Task<bool> DeleteReminderAsync(int reminderId, string userId)
    {
        var reminder = await _context.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId && r.UserId == userId);
        if (reminder == null) return false;

        _context.Reminders.Remove(reminder);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleReminderAsync(int reminderId, string userId)
    {
        var reminder = await _context.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId && r.UserId == userId);
        if (reminder == null) return false;

        reminder.IsEnabled = !reminder.IsEnabled;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Reminder>> GetRemindersForDeadlineAsync(int deadlineId, string userId)
    {
        return await _context.Reminders
            .Where(r => r.DeadlineId == deadlineId && r.UserId == userId)
            .OrderBy(r => r.ReminderTime)
            .ToListAsync();
    }
}
