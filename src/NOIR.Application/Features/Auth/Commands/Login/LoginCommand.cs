namespace NOIR.Application.Features.Auth.Commands.Login;

/// <summary>
/// Command to login a user.
/// Handler returns Result of AuthResponse with typed errors.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password);
