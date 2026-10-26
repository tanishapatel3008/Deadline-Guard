using System.ComponentModel.DataAnnotations;
using DeadlineGuard.Models.Enums;

namespace DeadlineGuard.Models;

public class Reminder
{
    public int Id { get; set; }

    public int DeadlineId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public DateTime ReminderTime { get; set; }

    public ReminderChannel Channel { get; set; } = ReminderChannel.InApp;

    public bool IsEnabled { get; set; } = true;

    public ReminderStatus Status { get; set; } = ReminderStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastAttemptAt { get; set; }

    public DateTime? SentAt { get; set; }

    [MaxLength(500)]
    public string? FailureReason { get; set; }

    public int RetryCount { get; set; } = 0;

    // Navigation properties
    public virtual Deadline? Deadline { get; set; }
    public virtual ApplicationUser? User { get; set; }
}
