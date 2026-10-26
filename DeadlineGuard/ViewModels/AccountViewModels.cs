using System.ComponentModel.DataAnnotations;

namespace DeadlineGuard.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember Me")]
    public bool RememberMe { get; set; } = false;

    public string? ReturnUrl { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class UserProfileViewModel
{
    [Required]
    [MaxLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Display(Name = "Time Zone")]
    public string TimeZoneId { get; set; } = "UTC";

    [Display(Name = "Preferred Theme")]
    public string PreferredTheme { get; set; } = "light";

    // User preferences
    [Display(Name = "Default Reminder (Minutes Before)")]
    [Range(5, 43200, ErrorMessage = "Reminder must be between 5 minutes and 30 days.")]
    public int DefaultReminderMinutes { get; set; } = 1440;

    [Display(Name = "Enable Email Reminders")]
    public bool EmailNotificationsEnabled { get; set; }

    [Display(Name = "Enable In-App Notifications")]
    public bool InAppNotificationsEnabled { get; set; } = true;

    [Display(Name = "Enable Daily Summary Email")]
    public bool DailySummaryEnabled { get; set; } = true;

    [Display(Name = "Daily Summary Dispatch Time")]
    public TimeSpan DailySummaryTime { get; set; } = new TimeSpan(9, 0, 0);

    [Display(Name = "Daily Available Prep Time (Minutes)")]
    [Range(30, 1440, ErrorMessage = "Prep time must be between 30 and 1440 minutes.")]
    public int AvailableDailyPrepMinutes { get; set; } = 240;

    // Password change (optional)
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "New Password (Optional)")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match.")]
    public string? ConfirmNewPassword { get; set; }
}
