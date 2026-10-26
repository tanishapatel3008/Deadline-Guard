using System.Security.Claims;
using DeadlineGuard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeadlineGuard.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDeadlineService _deadlineService;
    private readonly IDeadlinePriorityService _priorityService;

    public DashboardController(IDeadlineService deadlineService, IDeadlinePriorityService priorityService)
    {
        _deadlineService = deadlineService;
        _priorityService = priorityService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var model = await _deadlineService.GetDashboardDataAsync(CurrentUserId);
        return View(model);
    }

    public IActionResult Calendar()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> CalendarEvents(DateTime? start, DateTime? end)
    {
        var startDate = start ?? DateTime.UtcNow.AddMonths(-1);
        var endDate = end ?? DateTime.UtcNow.AddMonths(2);

        var items = await _deadlineService.GetCalendarDeadlinesAsync(CurrentUserId, startDate, endDate);

        var events = items.Select(i => new
        {
            id = i.Deadline.Id,
            title = i.Deadline.Title,
            start = i.Deadline.DueDateTime.ToString("o"),
            allDay = false,
            className = i.Priority.Classification switch
            {
                Models.Enums.PriorityClassification.Critical => "event-critical",
                Models.Enums.PriorityClassification.High => "event-high",
                Models.Enums.PriorityClassification.Medium => "event-medium",
                _ => "event-low"
            },
            url = Url.Action("Details", "Deadlines", new { id = i.Deadline.Id }),
            extendedProps = new
            {
                category = i.Deadline.Category.ToString(),
                priority = i.Priority.Classification.ToString(),
                score = i.Priority.Score,
                effort = i.Deadline.EstimatedEffortMinutes,
                isOverdue = i.Priority.IsOverdue
            }
        });

        return Json(events);
    }
}
