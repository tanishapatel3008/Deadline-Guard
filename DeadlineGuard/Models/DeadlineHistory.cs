using System.ComponentModel.DataAnnotations;

namespace DeadlineGuard.Models;

public class DeadlineHistory
{
    public int Id { get; set; }

    public int DeadlineId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PreviousValue { get; set; }

    [MaxLength(500)]
    public string? NewValue { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Deadline? Deadline { get; set; }
    public virtual ApplicationUser? User { get; set; }
}
