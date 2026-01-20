namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for sending welcome emails to newly created users.
/// </summary>
public interface IWelcomeEmailService
{
    /// <summary>
    /// Sends a welcome email to a newly created user with their temporary password.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="userName">User's display name</param>
    /// <param name="temporaryPassword">Temporary password assigned to the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation (does not throw on email failure)</returns>
    Task SendWelcomeEmailAsync(
        string email,
        string userName,
        string temporaryPassword,
        CancellationToken cancellationToken = default);
}
