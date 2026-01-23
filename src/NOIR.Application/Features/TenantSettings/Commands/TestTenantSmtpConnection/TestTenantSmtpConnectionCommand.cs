namespace NOIR.Application.Features.TenantSettings.Commands.TestTenantSmtpConnection;

/// <summary>
/// Command to test the tenant SMTP connection by sending a test email.
/// Uses the tenant's SMTP settings if configured, otherwise platform defaults.
/// </summary>
public sealed record TestTenantSmtpConnectionCommand(string RecipientEmail);
