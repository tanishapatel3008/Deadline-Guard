using System.Security.Claims;
using DeadlineGuard.Models.Enums;
using DeadlineGuard.Services;
using DeadlineGuard.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeadlineGuard.Controllers;

[Authorize]
public class DeadlinesController : Controller
{
    private readonly IDeadlineService _deadlineService;
    private readonly IDeadlinePriorityService _priorityService;
    private readonly IDeadlineConflictService _conflictService;

    public DeadlinesController(
        IDeadlineService deadlineService,
        IDeadlinePriorityService priorityService,
        IDeadlineConflictService conflictService)
    {
        _deadlineService = deadlineService;
        _priorityService = priorityService;
        _conflictService = conflictService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] DeadlineListViewModel filter)
    {
        var model = await _deadlineService.GetFilteredDeadlinesAsync(CurrentUserId, filter);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Overdue()
    {
        var filter = new DeadlineListViewModel
        {
            DateFilter = "overdue",
            SortBy = "priority"
        };
        var model = await _deadlineService.GetFilteredDeadlinesAsync(CurrentUserId, filter);
        ViewData["IsOverdueView"] = true;
        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Completed()
    {
        var filter = new DeadlineListViewModel
        {
            StatusFilter = DeadlineStatus.Completed,
            SortBy = "duedate"
        };
        var model = await _deadlineService.GetFilteredDeadlinesAsync(CurrentUserId, filter);
        ViewData["IsCompletedView"] = true;
        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var deadline = await _deadlineService.GetDeadlineByIdAsync(id, CurrentUserId);
        if (deadline == null)
        {
            TempData["ErrorMessage"] = "Deadline not found or you do not have permission to view it.";
            return RedirectToAction("Index");
        }

        var allActive = await _deadlineService.GetActiveDeadlinesAsync(CurrentUserId);
        var priority = _priorityService.CalculatePriority(deadline);
        var conflicts = _conflictService.GetConflictsForDeadline(deadline, allActive);

        var viewModel = new DeadlineDetailsViewModel
        {
            Deadline = deadline,
            Priority = priority,
            Conflicts = conflicts,
            Attachments = deadline.Attachments.OrderByDescending(a => a.UploadedAt).ToList(),
            Reminders = deadline.Reminders.OrderBy(r => r.ReminderTime).ToList(),
            History = deadline.Histories.OrderByDescending(h => h.ChangedAt).ToList()
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new DeadlineFormViewModel
        {
            DueDateTime = DateTime.UtcNow.AddDays(3),
            EstimatedEffortMinutes = 60,
            Category = DeadlineCategory.Bills,
            ImportanceLevel = ImportanceLevel.Medium,
            ConsequenceLevel = ConsequenceLevel.Moderate,
            ConsequenceType = ConsequenceType.FinancialPenalty,
            Reminder1Day = true,
            Reminder1Hour = true
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DeadlineFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var deadline = await _deadlineService.CreateDeadlineAsync(model, CurrentUserId);
        TempData["SuccessMessage"] = $"Deadline \"{deadline.Title}\" has been created and analyzed!";
        return RedirectToAction("Details", new { id = deadline.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _deadlineService.GetDeadlineForEditAsync(id, CurrentUserId);
        if (model == null)
        {
            TempData["ErrorMessage"] = "Deadline not found or unauthorized.";
            return RedirectToAction("Index");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DeadlineFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var success = await _deadlineService.UpdateDeadlineAsync(model, CurrentUserId);
        if (!success)
        {
            TempData["ErrorMessage"] = "Unable to update deadline. It may have been removed or you are unauthorized.";
            return RedirectToAction("Index");
        }

        TempData["SuccessMessage"] = "Deadline updated successfully.";
        return RedirectToAction("Details", new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? completionNotes)
    {
        var success = await _deadlineService.MarkCompletedAsync(id, CurrentUserId, completionNotes);
        if (success)
        {
            TempData["SuccessMessage"] = "Deadline marked as completed! Excellent work.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to mark deadline as completed.";
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveInProgress(int id)
    {
        var success = await _deadlineService.MoveInProgressAsync(id, CurrentUserId);
        if (success)
        {
            TempData["SuccessMessage"] = "Deadline marked as in progress.";
        }
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var success = await _deadlineService.CancelDeadlineAsync(id, CurrentUserId);
        if (success)
        {
            TempData["InfoMessage"] = "Deadline has been cancelled.";
        }
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(int id)
    {
        var success = await _deadlineService.ReopenDeadlineAsync(id, CurrentUserId);
        if (success)
        {
            TempData["SuccessMessage"] = "Deadline re-opened as pending.";
        }
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _deadlineService.DeleteDeadlineAsync(id, CurrentUserId);
        if (success)
        {
            TempData["SuccessMessage"] = "Deadline and associated files were deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to delete deadline.";
        }

        return RedirectToAction("Index");
    }
}
