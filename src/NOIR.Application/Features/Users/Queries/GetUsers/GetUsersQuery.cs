namespace NOIR.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Query to get paginated list of users.
/// </summary>
public sealed record GetUsersQuery(
    string? Search = null,
    string? Role = null,
    bool? IsLocked = null,
    int Page = 1,
    int PageSize = 20);
