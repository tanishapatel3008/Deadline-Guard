using System.Security.Claims;
using DeadlineGuard.Models.Enums;
using DeadlineGuard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeadlineGuard.Controllers;

[Authorize]
public class RemindersController : Controller
{
    private readonly IReminderService _reminderService;

    public RemindersController(IReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int deadlineId, DateTime reminderTime, ReminderChannel channel)
    {
        if (reminderTime <= DateTime.UtcNow)
        {
            TempData["ErrorMessage"] = "Reminder time must be set in the future.";
            return RedirectToAction("Details", "Deadlines", new { id = deadlineId });
        }

        var reminder = await _reminderService.AddReminderAsync(deadlineId, CurrentUserId, reminderTime.ToUniversalTime(), channel);
        if (reminder != null)
        {
            TempData["SuccessMessage"] = "Reminder scheduled successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to add reminder.";
        }

        return RedirectToAction("Details", "Deadlines", new { id = deadlineId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, int deadlineId)
    {
        await _reminderService.ToggleReminderAsync(id, CurrentUserId);
        return RedirectToAction("Details", "Deadlines", new { id = deadlineId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int deadlineId)
    {
        await _reminderService.DeleteReminderAsync(id, CurrentUserId);
        TempData["SuccessMessage"] = "Reminder removed.";
        return RedirectToAction("Details", "Deadlines", new { id = deadlineId });
    }
}
