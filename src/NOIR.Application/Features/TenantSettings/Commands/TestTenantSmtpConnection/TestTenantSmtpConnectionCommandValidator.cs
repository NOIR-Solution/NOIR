namespace NOIR.Application.Features.TenantSettings.Commands.TestTenantSmtpConnection;

/// <summary>
/// Validator for TestTenantSmtpConnectionCommand.
/// </summary>
public sealed class TestTenantSmtpConnectionCommandValidator : AbstractValidator<TestTenantSmtpConnectionCommand>
{
    public TestTenantSmtpConnectionCommandValidator()
    {
        RuleFor(x => x.RecipientEmail)
            .NotEmpty().WithMessage("Recipient email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
    }
}
