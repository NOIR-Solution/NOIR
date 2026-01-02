namespace NOIR.Application.Features.Auth.Commands.Register;

/// <summary>
/// Command to register a new user.
/// Handler returns Result of AuthResponse with typed errors.
/// </summary>
/// <param name="Email">User's email address.</param>
/// <param name="Password">User's password.</param>
/// <param name="FirstName">User's first name (optional).</param>
/// <param name="LastName">User's last name (optional).</param>
/// <param name="UseCookies">If true, sets HttpOnly cookies with tokens for browser-based auth.</param>
public sealed record RegisterCommand(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    bool UseCookies = false);
