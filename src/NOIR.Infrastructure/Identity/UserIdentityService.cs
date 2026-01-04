namespace NOIR.Infrastructure.Identity;

/// <summary>
/// Implementation of IUserIdentityService that wraps ASP.NET Core Identity.
/// Provides user management operations for handlers in the Application layer.
/// </summary>
public class UserIdentityService : IUserIdentityService, IScopedService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IDateTime _dateTime;

    public UserIdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IDateTime dateTime)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dateTime = dateTime;
    }

    #region User Lookup

    public async Task<UserIdentityDto?> FindByIdAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? null : MapToDto(user);
    }

    public async Task<UserIdentityDto?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalizedEmail = _userManager.NormalizeEmail(email);
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        return user is null ? null : MapToDto(user);
    }

    public IQueryable<UserIdentityDto> GetUsersQueryable()
    {
        return _userManager.Users
            .Where(u => !u.IsDeleted)
            .Select(u => new UserIdentityDto(
                u.Id,
                u.Email!,
                u.FirstName,
                u.LastName,
                u.DisplayName,
                (u.FirstName ?? "") + " " + (u.LastName ?? ""),
                u.TenantId,
                u.IsActive,
                u.IsDeleted,
                u.CreatedAt,
                u.ModifiedAt));
    }

    public async Task<(IReadOnlyList<UserIdentityDto> Users, int TotalCount)> GetUsersPaginatedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _userManager.Users
            .Where(u => !u.IsDeleted);

        // Apply search filter on raw entity (before projection)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(searchLower)) ||
                (u.DisplayName != null && u.DisplayName.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync(ct);

        // Order, paginate, then project
        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserIdentityDto(
                u.Id,
                u.Email!,
                u.FirstName,
                u.LastName,
                u.DisplayName,
                (u.FirstName ?? "") + " " + (u.LastName ?? ""),
                u.TenantId,
                u.IsActive,
                u.IsDeleted,
                u.CreatedAt,
                u.ModifiedAt))
            .ToListAsync(ct);

        return (users, totalCount);
    }

    #endregion

    #region Authentication

    public async Task<PasswordSignInResult> CheckPasswordSignInAsync(
        string userId,
        string password,
        bool lockoutOnFailure = true,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new PasswordSignInResult(false, false, false, false);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
        return new PasswordSignInResult(
            result.Succeeded,
            result.IsLockedOut,
            result.IsNotAllowed,
            result.RequiresTwoFactor);
    }

    public string NormalizeEmail(string email)
    {
        return _userManager.NormalizeEmail(email) ?? email;
    }

    #endregion

    #region User CRUD

    public async Task<IdentityOperationResult> CreateUserAsync(
        CreateUserDto dto,
        string password,
        CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DisplayName = dto.DisplayName,
            TenantId = dto.TenantId,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(
                result.Errors.Select(e => e.Description).ToArray());
        }

        return IdentityOperationResult.Success(user.Id);
    }

    public async Task<IdentityOperationResult> UpdateUserAsync(
        string userId,
        UpdateUserDto updates,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityOperationResult.Failure("User not found.");
        }

        if (updates.FirstName is not null)
            user.FirstName = updates.FirstName;
        if (updates.LastName is not null)
            user.LastName = updates.LastName;
        if (updates.DisplayName is not null)
            user.DisplayName = updates.DisplayName;
        if (updates.IsActive.HasValue)
            user.IsActive = updates.IsActive.Value;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(
                result.Errors.Select(e => e.Description).ToArray());
        }

        return IdentityOperationResult.Success(user.Id);
    }

    public async Task<IdentityOperationResult> SoftDeleteUserAsync(
        string userId,
        string deletedBy,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityOperationResult.Failure("User not found.");
        }

        user.IsDeleted = true;
        user.DeletedAt = _dateTime.UtcNow;
        user.DeletedBy = deletedBy;
        user.IsActive = false;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(
                result.Errors.Select(e => e.Description).ToArray());
        }

        return IdentityOperationResult.Success(user.Id);
    }

    public async Task<IdentityOperationResult> ResetPasswordAsync(
        string userId,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityOperationResult.Failure("User not found.");
        }

        // Generate a password reset token and use it to reset
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(
                result.Errors.Select(e => e.Description).ToArray());
        }

        // Update the password last changed timestamp
        user.PasswordLastChangedAt = _dateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return IdentityOperationResult.Success(user.Id);
    }

    #endregion

    #region Role Management

    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return [];
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    public async Task<IdentityOperationResult> AddToRolesAsync(
        string userId,
        IEnumerable<string> roleNames,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityOperationResult.Failure("User not found.");
        }

        var result = await _userManager.AddToRolesAsync(user, roleNames);
        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(
                result.Errors.Select(e => e.Description).ToArray());
        }

        return IdentityOperationResult.Success(user.Id);
    }

    public async Task<IdentityOperationResult> RemoveFromRolesAsync(
        string userId,
        IEnumerable<string> roleNames,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityOperationResult.Failure("User not found.");
        }

        var result = await _userManager.RemoveFromRolesAsync(user, roleNames);
        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(
                result.Errors.Select(e => e.Description).ToArray());
        }

        return IdentityOperationResult.Success(user.Id);
    }

    public async Task<bool> IsInRoleAsync(string userId, string roleName, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        return await _userManager.IsInRoleAsync(user, roleName);
    }

    public async Task<IdentityOperationResult> AssignRolesAsync(
        string userId,
        IEnumerable<string> roleNames,
        bool replaceExisting = false,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityOperationResult.Failure("User not found.");
        }

        var newRoles = roleNames.ToList();

        if (replaceExisting)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(newRoles).ToList();
            var rolesToAdd = newRoles.Except(currentRoles).ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    return IdentityOperationResult.Failure(
                        removeResult.Errors.Select(e => e.Description).ToArray());
                }
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    return IdentityOperationResult.Failure(
                        addResult.Errors.Select(e => e.Description).ToArray());
                }
            }
        }
        else
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = newRoles.Except(currentRoles).ToList();

            if (rolesToAdd.Count > 0)
            {
                var result = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!result.Succeeded)
                {
                    return IdentityOperationResult.Failure(
                        result.Errors.Select(e => e.Description).ToArray());
                }
            }
        }

        return IdentityOperationResult.Success(user.Id);
    }

    #endregion

    #region Mapping

    private static UserIdentityDto MapToDto(ApplicationUser user)
    {
        return new UserIdentityDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.DisplayName,
            user.FullName,
            user.TenantId,
            user.IsActive,
            user.IsDeleted,
            user.CreatedAt,
            user.ModifiedAt);
    }

    #endregion
}
