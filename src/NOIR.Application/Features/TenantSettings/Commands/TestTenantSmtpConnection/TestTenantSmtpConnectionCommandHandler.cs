namespace NOIR.Application.Features.TenantSettings.Commands.TestTenantSmtpConnection;

/// <summary>
/// Handler for testing tenant SMTP connection.
/// Uses the EmailService which already has the fallback chain (tenant → platform → appsettings).
/// </summary>
public class TestTenantSmtpConnectionCommandHandler
{
    private readonly ISmtpTestService _smtpTestService;

    public TestTenantSmtpConnectionCommandHandler(ISmtpTestService smtpTestService)
    {
        _smtpTestService = smtpTestService;
    }

    public async Task<Result<bool>> Handle(
        TestTenantSmtpConnectionCommand command,
        CancellationToken cancellationToken)
    {
        return await _smtpTestService.SendTestEmailAsync(command.RecipientEmail, cancellationToken);
    }
}
