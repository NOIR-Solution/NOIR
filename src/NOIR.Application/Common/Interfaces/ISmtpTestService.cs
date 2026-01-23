namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for testing SMTP connections.
/// </summary>
public interface ISmtpTestService
{
    /// <summary>
    /// Sends a test email using the configured platform SMTP settings.
    /// </summary>
    /// <param name="recipientEmail">The email address to send the test to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure with error details.</returns>
    Task<Result<bool>> SendTestEmailAsync(string recipientEmail, CancellationToken cancellationToken = default);
}
