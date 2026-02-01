namespace NOIR.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Validator for LogoutCommand.
/// </summary>
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        // RefreshToken is optional - if not provided, token from cookie is used.
        // No validation rules required as both parameters have default values.
    }
}
