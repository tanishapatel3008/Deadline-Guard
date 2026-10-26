using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeadlineGuard.Models.Enums;

namespace DeadlineGuard.Models;

public class Deadline
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DeadlineCategory Category { get; set; } = DeadlineCategory.Other;

    public DateTime DueDateTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Range(0, 100000)]
    public int EstimatedEffortMinutes { get; set; } = 30;

    public ImportanceLevel ImportanceLevel { get; set; } = ImportanceLevel.Medium;

    public ConsequenceLevel ConsequenceLevel { get; set; } = ConsequenceLevel.Moderate;

    public ConsequenceType ConsequenceType { get; set; } = ConsequenceType.OrdinaryInconvenience;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PotentialPenaltyAmount { get; set; }

    [MaxLength(5)]
    public string CurrencyCode { get; set; } = "USD";

    public DeadlineStatus Status { get; set; } = DeadlineStatus.Pending;

    public bool IsRecurring { get; set; } = false;

    public RecurrenceRule RecurrenceRule { get; set; } = RecurrenceRule.None;

    public int RecurrenceInterval { get; set; } = 1;

    public DateTime? RecurrenceEndDate { get; set; }

    public DateTime? LastCompletedAt { get; set; }

    [MaxLength(1000)]
    public string? CompletionNotes { get; set; }

    public double? CustomPriorityWeight { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<DeadlineAttachment> Attachments { get; set; } = new List<DeadlineAttachment>();
    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public virtual ICollection<DeadlineHistory> Histories { get; set; } = new List<DeadlineHistory>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
