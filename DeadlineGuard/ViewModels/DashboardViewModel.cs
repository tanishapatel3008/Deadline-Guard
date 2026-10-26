using DeadlineGuard.Models;
using DeadlineGuard.Services;

namespace DeadlineGuard.ViewModels;

public class DashboardViewModel
{
    public int ActiveDeadlinesCount { get; set; }
    public int DueTodayCount { get; set; }
    public int DueNext7DaysCount { get; set; }
    public int OverdueCount { get; set; }
    public int CompletedCount { get; set; }
    public int CriticalHighCount { get; set; }

    public List<DeadlineItemViewModel> TodayActionPlan { get; set; } = new();
    public List<DeadlineConflict> ActiveConflicts { get; set; } = new();
    public List<Deadline> UpcomingRecurring { get; set; } = new();
    public List<DeadlineHistory> RecentActivity { get; set; } = new();
    public List<Notification> UnreadNotifications { get; set; } = new();

    // Chart Data
    public List<string> CategoryLabels { get; set; } = new();
    public List<int> CategoryCounts { get; set; } = new();

    public List<string> PriorityLabels { get; set; } = new() { "Critical", "High", "Medium", "Low" };
    public List<int> PriorityCounts { get; set; } = new() { 0, 0, 0, 0 };

    public List<string> CompletionTrendLabels { get; set; } = new();
    public List<int> CompletionTrendCounts { get; set; } = new();

    // User time zone for display
    public string UserTimeZone { get; set; } = "UTC";
}
