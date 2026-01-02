namespace NOIR.Application.Features.Auth.Queries.GetUserById;

/// <summary>
/// Query to get a user's profile by ID.
/// Used internally for before-state fetching in audit logging.
/// </summary>
/// <param name="UserId">The user ID to fetch.</param>
public sealed record GetUserByIdQuery(string UserId);

/// <summary>
/// User profile information for before-state comparison.
/// Contains only the mutable fields that can be updated.
/// </summary>
public sealed record UserProfileDto(
    string Id,
    string Email,
    string? FirstName,
    string? LastName,
    string FullName,
    IEnumerable<string> Roles,
    string? TenantId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);
