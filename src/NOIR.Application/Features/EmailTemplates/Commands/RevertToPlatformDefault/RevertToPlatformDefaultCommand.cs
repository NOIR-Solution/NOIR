namespace NOIR.Application.Features.EmailTemplates.Commands.RevertToPlatformDefault;

/// <summary>
/// Command to revert a tenant's customized email template to the platform default.
/// This deletes the tenant's custom version, making the platform template visible again.
/// </summary>
public sealed record RevertToPlatformDefaultCommand(Guid Id);
