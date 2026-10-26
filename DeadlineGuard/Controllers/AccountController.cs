using DeadlineGuard.Data;
using DeadlineGuard.Models;
using DeadlineGuard.Services;
using DeadlineGuard.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeadlineGuard.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "An account with this email address already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName.Trim(),
            CreatedAt = DateTime.UtcNow,
            TimeZoneId = "UTC",
            PreferredTheme = "light",
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Create default user preferences
            var preferences = new UserPreferences
            {
                UserId = user.Id,
                DefaultReminderMinutes = 1440,
                EmailNotificationsEnabled = false,
                InAppNotificationsEnabled = true,
                DailySummaryEnabled = true,
                DailySummaryTime = new TimeSpan(9, 0, 0),
                DefaultTimeZone = "UTC",
                AvailableDailyPrepMinutes = 240,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserPreferences.Add(preferences);

            // Add welcome notification
            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = "Welcome to DeadlineGuard!",
                Message = "Your account has been created. Start by adding your critical deadlines to track urgency, consequences, and avoid penalties.",
                NotificationType = "System",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["SuccessMessage"] = "Welcome! Your account has been created successfully.";
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account has been temporarily locked out due to multiple failed login attempts.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid credentials. Please verify your email and password.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["InfoMessage"] = "You have been logged out.";
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Do not reveal that user doesn't exist
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, protocol: Request.Scheme);

        await _emailService.SendEmailAsync(
            user.Email!,
            "DeadlineGuard Password Reset",
            $"Please reset your password by <a href='{callbackUrl}'>clicking here</a>.");

        return RedirectToAction("ForgotPasswordConfirmation");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string token, string email)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login");
        }

        var model = new ResetPasswordViewModel { Token = token, Email = email };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return RedirectToAction("ResetPasswordConfirmation");
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            return RedirectToAction("ResetPasswordConfirmation");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        var prefs = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == user.Id);

        var model = new UserProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            TimeZoneId = user.TimeZoneId,
            PreferredTheme = user.PreferredTheme,
            DefaultReminderMinutes = prefs?.DefaultReminderMinutes ?? 1440,
            EmailNotificationsEnabled = prefs?.EmailNotificationsEnabled ?? false,
            InAppNotificationsEnabled = prefs?.InAppNotificationsEnabled ?? true,
            DailySummaryEnabled = prefs?.DailySummaryEnabled ?? true,
            DailySummaryTime = prefs?.DailySummaryTime ?? new TimeSpan(9, 0, 0),
            AvailableDailyPrepMinutes = prefs?.AvailableDailyPrepMinutes ?? 240
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UserProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        if (!ModelState.IsValid) return View(model);

        user.FullName = model.FullName.Trim();
        user.TimeZoneId = model.TimeZoneId;
        user.PreferredTheme = model.PreferredTheme;
        await _userManager.UpdateAsync(user);

        // Update preferences
        var prefs = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (prefs == null)
        {
            prefs = new UserPreferences { UserId = user.Id };
            _context.UserPreferences.Add(prefs);
        }

        prefs.DefaultReminderMinutes = model.DefaultReminderMinutes;
        prefs.EmailNotificationsEnabled = model.EmailNotificationsEnabled;
        prefs.InAppNotificationsEnabled = model.InAppNotificationsEnabled;
        prefs.DailySummaryEnabled = model.DailySummaryEnabled;
        prefs.DailySummaryTime = model.DailySummaryTime;
        prefs.DefaultTimeZone = model.TimeZoneId;
        prefs.AvailableDailyPrepMinutes = model.AvailableDailyPrepMinutes;
        prefs.UpdatedAt = DateTime.UtcNow;

        // Change password if provided
        if (!string.IsNullOrEmpty(model.NewPassword))
        {
            if (string.IsNullOrEmpty(model.CurrentPassword))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is required to set a new password.");
                return View(model);
            }

            var changeResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!changeResult.Succeeded)
            {
                foreach (var err in changeResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return View(model);
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Profile and settings updated successfully.";
        return RedirectToAction("Profile");
    }
}
