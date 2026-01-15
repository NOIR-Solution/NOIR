namespace NOIR.Application.Features.Users.DTOs;

/// <summary>
/// Model for the welcome email template sent when admin creates a user.
/// </summary>
public record WelcomeEmailModel(
    string UserName,
    string Email,
    string TemporaryPassword,
    string LoginUrl,
    string ApplicationName);
