using System.ComponentModel.DataAnnotations;

namespace DeadlineGuard.Models;

public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public int? DeadlineId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string NotificationType { get; set; } = "Reminder";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual Deadline? Deadline { get; set; }
}
