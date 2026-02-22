namespace NOIR.Application.Features.Roles.DTOs;

/// <summary>
/// Role data transfer object with full details.
/// </summary>
public sealed record RoleDto(
    string Id,
    string Name,
    string? NormalizedName,
    string? Description,
    string? ParentRoleId,
    string? ParentRoleName,
    Guid? TenantId,
    bool IsSystemRole,
    int SortOrder,
    string? IconName,
    string? Color,
    int UserCount,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> EffectivePermissions);

/// <summary>
/// Simplified role for listings.
/// </summary>
public sealed record RoleListDto(
    string Id,
    string Name,
    string? Description,
    string? ParentRoleId,
    bool IsSystemRole,
    int SortOrder,
    string? IconName,
    string? Color,
    int UserCount,
    int PermissionCount);

/// <summary>
/// Role hierarchy item showing parent-child relationships.
/// </summary>
public sealed record RoleHierarchyDto(
    string Id,
    string Name,
    string? Description,
    int Level,
    IReadOnlyList<RoleHierarchyDto> Children);
