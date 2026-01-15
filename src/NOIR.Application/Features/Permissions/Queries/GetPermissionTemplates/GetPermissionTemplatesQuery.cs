namespace NOIR.Application.Features.Permissions.Queries.GetPermissionTemplates;

/// <summary>
/// Query to get all permission templates.
/// Optionally filter by tenant.
/// </summary>
public sealed record GetPermissionTemplatesQuery(Guid? TenantId = null);
