namespace NOIR.Application.Features.Users.DTOs;

/// <summary>
/// User data transfer object for admin management.
/// </summary>
public sealed record UserDto(
    string Id,
    string Email,
    string? UserName,
    string? DisplayName,
    string? FirstName,
    string? LastName,
    bool EmailConfirmed,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEnd,
    IReadOnlyList<string> Roles);

/// <summary>
/// Simplified user for listings.
/// </summary>
public sealed record UserListDto(
    string Id,
    string Email,
    string? DisplayName,
    bool IsLocked,
    bool IsSystemUser,
    IReadOnlyList<string> Roles);

/// <summary>
/// User's effective permissions.
/// </summary>
public sealed record UserPermissionsDto(
    string UserId,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
