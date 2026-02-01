namespace NOIR.Application.Features.PlatformSettings.Commands.TestSmtpConnection;

/// <summary>
/// Validator for TestSmtpConnectionCommand.
/// </summary>
public sealed class TestSmtpConnectionCommandValidator : AbstractValidator<TestSmtpConnectionCommand>
{
    public TestSmtpConnectionCommandValidator()
    {
        RuleFor(x => x.RecipientEmail)
            .NotEmpty().WithMessage("Recipient email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
    }
}
