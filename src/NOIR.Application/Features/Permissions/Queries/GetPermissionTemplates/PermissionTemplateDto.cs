namespace NOIR.Application.Features.Permissions.Queries.GetPermissionTemplates;

/// <summary>
/// DTO for permission template.
/// </summary>
public sealed record PermissionTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    string? TenantId,
    bool IsSystem,
    string? IconName,
    string? Color,
    int SortOrder,
    IReadOnlyList<string> Permissions);
