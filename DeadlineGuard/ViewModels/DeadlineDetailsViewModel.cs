using DeadlineGuard.Models;
using DeadlineGuard.Services;

namespace DeadlineGuard.ViewModels;

public class DeadlineDetailsViewModel
{
    public Deadline Deadline { get; set; } = null!;
    public PriorityEvaluation Priority { get; set; } = null!;
    public List<DeadlineConflict> Conflicts { get; set; } = new();
    public List<DeadlineAttachment> Attachments { get; set; } = new();
    public List<Reminder> Reminders { get; set; } = new();
    public List<DeadlineHistory> History { get; set; } = new();
}
