namespace NOIR.Application.Features.Auth.Commands.Login;

/// <summary>
/// Command to login a user.
/// Handler returns Result of AuthResponse with typed errors.
/// </summary>
/// <param name="Email">User's email address.</param>
/// <param name="Password">User's password.</param>
/// <param name="UseCookies">If true, sets HttpOnly cookies with tokens for browser-based auth.</param>
public sealed record LoginCommand(
    string Email,
    string Password,
    bool UseCookies = false);
