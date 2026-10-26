using DeadlineGuard.Models;
using DeadlineGuard.Models.Enums;
using DeadlineGuard.Services;

namespace DeadlineGuard.ViewModels;

public class DeadlineItemViewModel
{
    public Deadline Deadline { get; set; } = null!;
    public PriorityEvaluation Priority { get; set; } = null!;
}

public class DeadlineListViewModel
{
    public List<DeadlineItemViewModel> Deadlines { get; set; } = new();

    public string? SearchTerm { get; set; }
    public DeadlineCategory? CategoryFilter { get; set; }
    public DeadlineStatus? StatusFilter { get; set; }
    public PriorityClassification? PriorityFilter { get; set; }
    public string DateFilter { get; set; } = "all"; // all, today, next7, overdue, month
    public string SortBy { get; set; } = "priority"; // priority, duedate, consequence, effort

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
