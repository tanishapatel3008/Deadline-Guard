using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace DeadlineGuard.Services;

public interface IEmailService
{
    Task<EmailSendResult> SendEmailAsync(string toEmail, string subject, string htmlBody);
    Task<EmailSendResult> SendReminderEmailAsync(string toEmail, string userName, string deadlineTitle, DateTime dueDateTime, string consequenceSummary, string explanation);
}

public class EmailSendResult
{
    public bool Succeeded { get; set; }
    public bool WasSkippedDueToConfig { get; set; }
    public string? ErrorMessage { get; set; }

    public static EmailSendResult Success() => new() { Succeeded = true };
    public static EmailSendResult Skipped(string reason) => new() { Succeeded = false, WasSkippedDueToConfig = true, ErrorMessage = reason };
    public static EmailSendResult Failed(string error) => new() { Succeeded = false, ErrorMessage = error };
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        if (!_settings.IsConfigured || string.IsNullOrWhiteSpace(_settings.Server))
        {
            _logger.LogInformation("Email notification to {Recipient} skipped because SMTP is not configured in appsettings/secrets.", toEmail);
            return EmailSendResult.Skipped("SMTP is not configured in application settings.");
        }

        try
        {
            using var client = new SmtpClient(_settings.Server, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
            }

            var mail = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
            _logger.LogInformation("Email successfully dispatched to {Recipient} with subject '{Subject}'.", toEmail, subject);
            return EmailSendResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}: {Message}", toEmail, ex.Message);
            return EmailSendResult.Failed(ex.Message);
        }
    }

    public async Task<EmailSendResult> SendReminderEmailAsync(
        string toEmail,
        string userName,
        string deadlineTitle,
        DateTime dueDateTime,
        string consequenceSummary,
        string explanation)
    {
        var subject = $"DeadlineGuard Reminder: \"{deadlineTitle}\" is approaching";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 24px; }}
        .card {{ background: #ffffff; max-width: 600px; margin: 0 auto; border-radius: 8px; border: 1px solid #e2e8f0; padding: 32px; }}
        .header {{ border-bottom: 2px solid #6366f1; padding-bottom: 16px; margin-bottom: 24px; }}
        .title {{ color: #0f172a; font-size: 20px; font-weight: 700; margin: 0; }}
        .badge {{ display: inline-block; padding: 4px 10px; background-color: #fee2e2; color: #991b1b; font-weight: 600; font-size: 12px; border-radius: 4px; }}
        .info-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        .info-table td {{ padding: 8px 0; border-bottom: 1px solid #f1f5f9; }}
        .label {{ font-weight: 600; color: #64748b; width: 140px; }}
        .footer {{ font-size: 12px; color: #94a3b8; text-align: center; margin-top: 24px; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='header'>
            <h2 class='title'>🛡️ DeadlineGuard Alert</h2>
        </div>
        <p>Hello <strong>{WebUtility.HtmlEncode(userName)}</strong>,</p>
        <p>This is an automated reminder regarding your upcoming deadline:</p>
        
        <div style='background: #f8fafc; border-left: 4px solid #6366f1; padding: 16px; margin: 16px 0;'>
            <h3 style='margin: 0 0 8px 0; color: #0f172a;'>{WebUtility.HtmlEncode(deadlineTitle)}</h3>
            <p style='margin: 0; color: #475569;'>{WebUtility.HtmlEncode(explanation)}</p>
        </div>

        <table class='info-table'>
            <tr>
                <td class='label'>Due Date (UTC):</td>
                <td><strong>{dueDateTime:MMM dd, yyyy HH:mm} UTC</strong></td>
            </tr>
            <tr>
                <td class='label'>Consequence:</td>
                <td>{WebUtility.HtmlEncode(consequenceSummary)}</td>
            </tr>
        </table>

        <p>Stay ahead of your obligations and prevent costly penalties.</p>
        <div class='footer'>
            Sent by DeadlineGuard &bull; Manage your preferences in your account settings.
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, body);
    }
}
