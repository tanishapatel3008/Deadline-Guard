using System.ComponentModel.DataAnnotations;

namespace DeadlineGuard.Models;

public class UserPreferences
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public int DefaultReminderMinutes { get; set; } = 1440; // 24 hours

    public bool EmailNotificationsEnabled { get; set; } = false;

    public bool InAppNotificationsEnabled { get; set; } = true;

    public bool DailySummaryEnabled { get; set; } = true;

    public TimeSpan DailySummaryTime { get; set; } = new TimeSpan(9, 0, 0);

    [MaxLength(100)]
    public string DefaultTimeZone { get; set; } = "UTC";

    public int AvailableDailyPrepMinutes { get; set; } = 240; // 4 hours daily for preparation

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual ApplicationUser? User { get; set; }
}
