using DeadlineGuard.Data;
using DeadlineGuard.Models;
using Microsoft.EntityFrameworkCore;

namespace DeadlineGuard.Services;

public interface INotificationService
{
    Task<Notification> CreateNotificationAsync(string userId, string title, string message, string type = "Reminder", int? deadlineId = null);
    Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 20, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(int notificationId, string userId);
    Task MarkAllAsReadAsync(string userId);
    Task DeleteNotificationAsync(int notificationId, string userId);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> CreateNotificationAsync(string userId, string title, string message, string type = "Reminder", int? deadlineId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            DeadlineId = deadlineId,
            Title = title,
            Message = message,
            NotificationType = type,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 20, bool unreadOnly = false)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => n.ReadAt == null);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .Include(n => n.Deadline)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && n.ReadAt == null);
    }

    public async Task MarkAsReadAsync(int notificationId, string userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification != null && notification.ReadAt == null)
        {
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .ToListAsync();

        foreach (var item in unread)
        {
            item.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteNotificationAsync(int notificationId, string userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }
}
