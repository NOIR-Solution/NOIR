namespace NOIR.Application.Features.Roles.DTOs;

/// <summary>
/// Role data transfer object.
/// </summary>
public sealed record RoleDto(
    string Id,
    string Name,
    string? NormalizedName,
    int UserCount,
    IReadOnlyList<string> Permissions);

/// <summary>
/// Simplified role for listings.
/// </summary>
public sealed record RoleListDto(
    string Id,
    string Name,
    int UserCount);

/// <summary>
/// Role with assigned users.
/// </summary>
public sealed record RoleWithUsersDto(
    string Id,
    string Name,
    IReadOnlyList<RoleUserDto> Users);

/// <summary>
/// User info within a role context.
/// </summary>
public sealed record RoleUserDto(
    string Id,
    string Email,
    string? DisplayName);
