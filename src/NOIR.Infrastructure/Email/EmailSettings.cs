namespace NOIR.Infrastructure.Email;

/// <summary>
/// Email configuration settings.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// Default sender email address.
    /// </summary>
    public string DefaultFromEmail { get; set; } = "noreply@noir.local";

    /// <summary>
    /// Default sender name.
    /// </summary>
    public string DefaultFromName { get; set; } = "NOIR";

    /// <summary>
    /// SMTP server host.
    /// </summary>
    public string SmtpHost { get; set; } = "localhost";

    /// <summary>
    /// SMTP server port.
    /// </summary>
    public int SmtpPort { get; set; } = 25;

    /// <summary>
    /// SMTP username (optional).
    /// </summary>
    public string? SmtpUser { get; set; }

    /// <summary>
    /// SMTP password (optional).
    /// </summary>
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// Enable SSL/TLS for SMTP connection.
    /// </summary>
    public bool EnableSsl { get; set; } = false;

    /// <summary>
    /// Path to email templates folder.
    /// </summary>
    public string TemplatesPath { get; set; } = "EmailTemplates";
}
