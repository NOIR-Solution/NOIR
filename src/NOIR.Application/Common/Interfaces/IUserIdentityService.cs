namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Abstracts ASP.NET Core Identity operations for user management.
/// This interface allows handlers in the Application layer to perform identity operations
/// without directly depending on UserManager and SignInManager types.
/// Implementations in Infrastructure layer provide the actual identity logic.
/// </summary>
public interface IUserIdentityService
{
    #region User Lookup

    /// <summary>
    /// Finds a user by their unique identifier.
    /// </summary>
    Task<UserIdentityDto?> FindByIdAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Finds a user by their email address.
    /// </summary>
    Task<UserIdentityDto?> FindByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Gets a queryable for paginated user listing.
    /// Returns a projection to UserIdentityDto.
    /// Note: This returns a projected queryable - use GetUsersPaginatedAsync for EF-compatible pagination.
    /// </summary>
    IQueryable<UserIdentityDto> GetUsersQueryable();

    /// <summary>
    /// Gets paginated users with optional search filter.
    /// Handles EF Core translation properly by doing projection after ordering.
    /// </summary>
    Task<(IReadOnlyList<UserIdentityDto> Users, int TotalCount)> GetUsersPaginatedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default);

    #endregion

    #region Authentication

    /// <summary>
    /// Validates user credentials and handles lockout logic.
    /// </summary>
    /// <returns>A result indicating success, lockout, or invalid credentials.</returns>
    Task<PasswordSignInResult> CheckPasswordSignInAsync(
        string userId,
        string password,
        bool lockoutOnFailure = true,
        CancellationToken ct = default);

    /// <summary>
    /// Normalizes an email address for consistent comparison.
    /// </summary>
    string NormalizeEmail(string email);

    #endregion

    #region User CRUD

    /// <summary>
    /// Creates a new user with the specified password.
    /// </summary>
    Task<IdentityOperationResult> CreateUserAsync(
        CreateUserDto user,
        string password,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing user's profile information.
    /// </summary>
    Task<IdentityOperationResult> UpdateUserAsync(
        string userId,
        UpdateUserDto updates,
        CancellationToken ct = default);

    /// <summary>
    /// Soft deletes a user (marks as deleted).
    /// </summary>
    Task<IdentityOperationResult> SoftDeleteUserAsync(
        string userId,
        string deletedBy,
        CancellationToken ct = default);

    /// <summary>
    /// Resets a user's password without requiring the old password.
    /// Used for password reset flow after OTP verification.
    /// </summary>
    Task<IdentityOperationResult> ResetPasswordAsync(
        string userId,
        string newPassword,
        CancellationToken ct = default);

    /// <summary>
    /// Changes user password after verifying current password.
    /// Updates PasswordLastChangedAt timestamp.
    /// </summary>
    Task<IdentityOperationResult> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default);

    #endregion

    #region Role Management

    /// <summary>
    /// Gets all roles assigned to a user.
    /// </summary>
    Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Adds a user to multiple roles.
    /// </summary>
    Task<IdentityOperationResult> AddToRolesAsync(
        string userId,
        IEnumerable<string> roleNames,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a user from multiple roles.
    /// </summary>
    Task<IdentityOperationResult> RemoveFromRolesAsync(
        string userId,
        IEnumerable<string> roleNames,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a user is in a specific role.
    /// </summary>
    Task<bool> IsInRoleAsync(string userId, string roleName, CancellationToken ct = default);

    /// <summary>
    /// Assigns roles to a user, replacing existing roles if specified.
    /// </summary>
    Task<IdentityOperationResult> AssignRolesAsync(
        string userId,
        IEnumerable<string> roleNames,
        bool replaceExisting = false,
        CancellationToken ct = default);

    #endregion
}

#region DTOs for Identity Operations

/// <summary>
/// DTO representing user identity information.
/// Decouples Application layer from ApplicationUser entity.
/// </summary>
public record UserIdentityDto(
    string Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string FullName,
    string? TenantId,
    bool IsActive,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// DTO for creating a new user.
/// </summary>
public record CreateUserDto(
    string Email,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? TenantId);

/// <summary>
/// DTO for updating user information.
/// </summary>
public record UpdateUserDto(
    string? FirstName = null,
    string? LastName = null,
    string? DisplayName = null,
    bool? IsActive = null);

/// <summary>
/// Result of a password sign-in attempt.
/// </summary>
public record PasswordSignInResult(
    bool Succeeded,
    bool IsLockedOut,
    bool IsNotAllowed,
    bool RequiresTwoFactor);

/// <summary>
/// Result of an identity operation.
/// </summary>
public record IdentityOperationResult(
    bool Succeeded,
    string? UserId = null,
    IReadOnlyList<string>? Errors = null)
{
    public static IdentityOperationResult Success(string? userId = null) => new(true, userId);
    public static IdentityOperationResult Failure(params string[] errors) => new(false, null, errors);
}

#endregion
