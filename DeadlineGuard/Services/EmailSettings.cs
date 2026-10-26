namespace DeadlineGuard.Services;

public class EmailSettings
{
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string SenderName { get; set; } = "DeadlineGuard";
    public string SenderEmail { get; set; } = "no-reply@deadlineguard.com";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public bool IsConfigured { get; set; } = false;
}
