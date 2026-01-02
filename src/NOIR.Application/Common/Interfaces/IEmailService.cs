namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for sending emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email to a single recipient.
    /// </summary>
    Task<bool> SendAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email to multiple recipients.
    /// </summary>
    Task<bool> SendAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email using a Razor template.
    /// </summary>
    Task<bool> SendTemplateAsync<T>(string to, string subject, string templateName, T model, CancellationToken cancellationToken = default);
}
