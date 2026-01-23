namespace NOIR.Application.Features.PlatformSettings.Commands.TestSmtpConnection;

/// <summary>
/// Handler for testing SMTP connection using the configured platform settings.
/// </summary>
public class TestSmtpConnectionCommandHandler
{
    private readonly ISmtpTestService _smtpTestService;

    public TestSmtpConnectionCommandHandler(ISmtpTestService smtpTestService)
    {
        _smtpTestService = smtpTestService;
    }

    public async Task<Result<bool>> Handle(
        TestSmtpConnectionCommand command,
        CancellationToken cancellationToken)
    {
        return await _smtpTestService.SendTestEmailAsync(command.RecipientEmail, cancellationToken);
    }
}
