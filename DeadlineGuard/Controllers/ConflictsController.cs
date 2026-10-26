using System.Security.Claims;
using DeadlineGuard.Data;
using DeadlineGuard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeadlineGuard.Controllers;

[Authorize]
public class ConflictsController : Controller
{
    private readonly IDeadlineConflictService _conflictService;
    private readonly IDeadlineService _deadlineService;
    private readonly ApplicationDbContext _context;

    public ConflictsController(
        IDeadlineConflictService conflictService,
        IDeadlineService deadlineService,
        ApplicationDbContext context)
    {
        _conflictService = conflictService;
        _deadlineService = deadlineService;
        _context = context;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var activeDeadlines = await _deadlineService.GetActiveDeadlinesAsync(CurrentUserId);
        var prefs = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == CurrentUserId);
        int dailyPrep = prefs?.AvailableDailyPrepMinutes ?? 240;

        var conflicts = _conflictService.DetectConflicts(activeDeadlines, dailyPrep);
        ViewData["DailyPrepMinutes"] = dailyPrep;

        return View(conflicts);
    }
}
