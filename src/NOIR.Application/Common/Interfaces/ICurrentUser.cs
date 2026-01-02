namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Interface for accessing the current authenticated user.
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }
    string? Email { get; }
    string? TenantId { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
    bool IsInRole(string role);
}
