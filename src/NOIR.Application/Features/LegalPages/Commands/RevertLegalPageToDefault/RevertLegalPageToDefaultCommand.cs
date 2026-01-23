namespace NOIR.Application.Features.LegalPages.Commands.RevertLegalPageToDefault;

/// <summary>
/// Command to revert a tenant's customized legal page to the platform default.
/// This deletes the tenant's custom version, making the platform page visible again.
/// </summary>
public sealed record RevertLegalPageToDefaultCommand(Guid Id);
