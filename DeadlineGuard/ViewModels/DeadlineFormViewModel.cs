using System.ComponentModel.DataAnnotations;
using DeadlineGuard.Models.Enums;

namespace DeadlineGuard.ViewModels;

public class DeadlineFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    [Display(Name = "Deadline Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    [Required]
    public DeadlineCategory Category { get; set; } = DeadlineCategory.Bills;

    [Required(ErrorMessage = "Due date and time is required.")]
    [Display(Name = "Due Date & Time")]
    public DateTime DueDateTime { get; set; } = DateTime.UtcNow.AddDays(3);

    [Required]
    [Range(0, 100000, ErrorMessage = "Estimated effort cannot be negative.")]
    [Display(Name = "Estimated Effort (Minutes)")]
    public int EstimatedEffortMinutes { get; set; } = 60;

    [Required]
    [Display(Name = "Importance Level")]
    public ImportanceLevel ImportanceLevel { get; set; } = ImportanceLevel.Medium;

    [Required]
    [Display(Name = "Consequence Level")]
    public ConsequenceLevel ConsequenceLevel { get; set; } = ConsequenceLevel.Moderate;

    [Required]
    [Display(Name = "Consequence Type")]
    public ConsequenceType ConsequenceType { get; set; } = ConsequenceType.OrdinaryInconvenience;

    [Range(0, 1000000, ErrorMessage = "Penalty amount must be a positive number.")]
    [Display(Name = "Potential Penalty Amount")]
    public decimal? PotentialPenaltyAmount { get; set; }

    [StringLength(5)]
    [Display(Name = "Currency")]
    public string CurrencyCode { get; set; } = "USD";

    [Display(Name = "Recurring Deadline")]
    public bool IsRecurring { get; set; } = false;

    [Display(Name = "Recurrence Pattern")]
    public RecurrenceRule RecurrenceRule { get; set; } = RecurrenceRule.None;

    [Range(1, 365, ErrorMessage = "Recurrence interval must be at least 1.")]
    [Display(Name = "Every (Interval)")]
    public int RecurrenceInterval { get; set; } = 1;

    [Display(Name = "Recurrence End Date")]
    public DateTime? RecurrenceEndDate { get; set; }

    [Range(0, 100, ErrorMessage = "Custom priority weight must be between 0 and 100.")]
    [Display(Name = "Custom Priority Weight (Optional)")]
    public double? CustomPriorityWeight { get; set; }

    // Reminder presets selection
    [Display(Name = "7 Days Before")]
    public bool Reminder7Days { get; set; }

    [Display(Name = "3 Days Before")]
    public bool Reminder3Days { get; set; }

    [Display(Name = "1 Day Before")]
    public bool Reminder1Day { get; set; } = true;

    [Display(Name = "1 Hour Before")]
    public bool Reminder1Hour { get; set; } = true;

    [Display(Name = "Also Send Email Notification")]
    public bool ReminderEmail { get; set; } = false;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsRecurring)
        {
            if (RecurrenceRule == RecurrenceRule.None)
            {
                yield return new ValidationResult(
                    "Please select a recurrence rule if recurring is enabled.",
                    new[] { nameof(RecurrenceRule) });
            }

            if (RecurrenceEndDate.HasValue && RecurrenceEndDate.Value <= DueDateTime)
            {
                yield return new ValidationResult(
                    "Recurrence end date must be after the initial due date.",
                    new[] { nameof(RecurrenceEndDate) });
            }
        }

        if (ConsequenceType == ConsequenceType.FinancialPenalty &&
            (!PotentialPenaltyAmount.HasValue || PotentialPenaltyAmount.Value <= 0))
        {
            yield return new ValidationResult(
                "Please specify a potential penalty amount when financial penalty is selected.",
                new[] { nameof(PotentialPenaltyAmount) });
        }
    }
}
