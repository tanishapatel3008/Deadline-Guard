using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace DeadlineGuard.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string TimeZoneId { get; set; } = "UTC";

    [MaxLength(20)]
    public string PreferredTheme { get; set; } = "light";

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Deadline> Deadlines { get; set; } = new List<Deadline>();
    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<DeadlineAttachment> Attachments { get; set; } = new List<DeadlineAttachment>();
    public virtual ICollection<DeadlineHistory> Histories { get; set; } = new List<DeadlineHistory>();
    public virtual UserPreferences? Preferences { get; set; }
}
