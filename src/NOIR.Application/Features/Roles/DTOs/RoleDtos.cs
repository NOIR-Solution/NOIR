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
