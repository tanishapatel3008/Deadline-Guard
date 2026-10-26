using System.ComponentModel.DataAnnotations;

namespace DeadlineGuard.Models;

public class DeadlineAttachment
{
    public int Id { get; set; }

    public int DeadlineId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    // Navigation properties
    public virtual Deadline? Deadline { get; set; }
    public virtual ApplicationUser? User { get; set; }
}
