namespace NOIR.Application.Features.Auth.Commands.Register;

/// <summary>
/// Command to register a new user.
/// Handler returns Result of AuthResponse with typed errors.
/// </summary>
public sealed record RegisterCommand(
    string Email,
    string Password,
    string? FirstName,
    string? LastName);
