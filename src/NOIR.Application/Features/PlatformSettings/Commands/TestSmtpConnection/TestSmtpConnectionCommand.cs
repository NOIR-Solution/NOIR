namespace NOIR.Application.Features.PlatformSettings.Commands.TestSmtpConnection;

/// <summary>
/// Command to test SMTP connection by sending a test email.
/// </summary>
public sealed record TestSmtpConnectionCommand(string RecipientEmail);
