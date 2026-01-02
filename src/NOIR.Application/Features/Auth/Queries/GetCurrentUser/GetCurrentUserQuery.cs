namespace NOIR.Application.Features.Auth.Queries.GetCurrentUser;

/// <summary>
/// Query to get the current authenticated user's profile.
/// Handler returns Result of CurrentUserDto with typed errors.
/// </summary>
public sealed record GetCurrentUserQuery;

/// <summary>
/// Current user profile information.
/// </summary>
public sealed record CurrentUserDto(
    string Id,
    string Email,
    string? FirstName,
    string? LastName,
    string FullName,
    IEnumerable<string> Roles,
    string? TenantId,
    bool IsActive,
    DateTimeOffset CreatedAt);
