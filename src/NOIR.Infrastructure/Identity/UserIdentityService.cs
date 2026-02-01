namespace NOIR.Infrastructure.Identity;

/// <summary>
/// Implementation of IUserIdentityService that wraps ASP.NET Core Identity.
/// Provides user management operations for handlers in the Application layer.
/// </summary>
public class UserIdentityService : IUserIdentityService, IScopedService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IMultiTenantStore<Tenant> _tenantStore;
    private readonly IDateTime _dateTime;

    public UserIdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IMultiTenantStore<Tenant> tenantStore,
        IDateTime dateTime)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tenantStore = tenantStore;
        _dateTime = dateTime;
    }

    #region User Lookup

    public async Task<UserIdentityDto?> FindByIdAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? null : MapToDto(user);
    }

    public async Task<UserIdentityDto?> FindByEmailAsync(string email, string? tenantId, CancellationToken ct = default)
    {
        var normalizedEmail = _userManager.NormalizeEmail(email);
        // Email uniqueness is scoped to tenant
        // IgnoreQueryFilters to bypass any global query filters since we explicitly filter by TenantId
        var user = await _userManager.Users
            .IgnoreQueryFilters()
            .Where(u => u.NormalizedEmail == normalizedEmail && u.TenantId == tenantId && !u.IsDeleted)
            .FirstOrDefaultAsync(ct);
        return user is null ? null : MapToDto(user);
    }

    public async Task<IReadOnlyList<UserTenantInfo>> FindTenantsByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalizedEmail = _userManager.NormalizeEmail(email);

        // Find all users (across all tenants) with this email
        // IgnoreQueryFilters to bypass tenant query filter
        var users = await _userManager.Users
            .IgnoreQueryFilters()
            .Where(u => u.NormalizedEmail == normalizedEmail && !u.IsDeleted && u.IsActive)
            .Select(u => new { u.Id, u.TenantId })
            .ToListAsync(ct);

        if (users.Count == 0)
        {
            return [];
        }

        // Get all tenants to map tenant IDs to names
        var allTenants = await _tenantStore.GetAllAsync();
        var tenantLookup = allTenants.ToDictionary(t => t.Id, t => t);

        var result = new List<UserTenantInfo>();
        foreach (var user in users)
        {
            string tenantName;
            string? tenantIdentifier;

            if (user.TenantId is null)
            {
                // Platform admin user - no tenant
                tenantName = "Platform";
                tenantIdentifier = null;
            }
            else if (tenantLookup.TryGetValue(user.TenantId, out var tenant))
            {
                tenantName = tenant.Name ?? tenant.Identifier;
                tenantIdentifier = tenant.Identifier;
            }
            else
            {
                // Tenant not found (orphaned user)
                continue;
            }

            result.Add(new UserTenantInfo(
                user.Id,
                user.TenantId,
                tenantName,
                tenantIdentifier));
        }

        return result;
    }

    public IQueryable<UserIdentityDto> GetUsersQueryable()
    {
        return _userManager.Users
            .Where(u => !u.IsDeleted)
            .Select(u => new UserIdentityDto(
                u.Id,
                u.Email!,
                u.TenantId,
                u.FirstName,
                u.LastName,
                u.DisplayName,
                (u.FirstName ?? "") + " " + (u.LastName ?? ""),
                u.PhoneNumber,
                u.AvatarUrl,
                u.IsActive,
                u.IsDeleted,
                u.IsSystemUser,
                u.CreatedAt,
                u.ModifiedAt));
    }

    public async Task<(IReadOnlyList<UserIdentityDto> Users, int TotalCount)> GetUsersPaginatedAsync(
        string? tenantId,
        string? search,
        int page,
        int pageSize,
        string? role = null,
        bool? isLocked = null,
        CancellationToken ct = default)
    {
        var query = _userManager.Users
            .Where(u => !u.IsDeleted && u.TenantId == tenantId);

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

        // Apply lockout filter at database level (IsActive = !isLocked)
        if (isLocked.HasValue)
        {
            var shouldBeActive = !isLocked.Value;
            query = query.Where(u => u.IsActive == shouldBeActive);
        }

        // Apply role filter at database level
        if (!string.IsNullOrWhiteSpace(role))
        {
            // Get user IDs in the specified role (efficient single query)
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            var userIdsInRole = usersInRole.Select(u => u.Id).ToHashSet();
            query = query.Where(u => userIdsInRole.Contains(u.Id));
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
                u.TenantId,
                u.FirstName,
                u.LastName,
                u.DisplayName,
                (u.FirstName ?? "") + " " + (u.LastName ?? ""),
                u.PhoneNumber,
                u.AvatarUrl,
                u.IsActive,
                u.IsDeleted,
                u.IsSystemUser,
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
        // Validate required inputs
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return IdentityOperationResult.Failure("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return IdentityOperationResult.Failure("Password is required.");
        }

        // Check per-tenant email uniqueness (email can exist in different tenants, but not in the same tenant)
        var existingUser = await FindByEmailAsync(dto.Email, dto.TenantId, ct);
        if (existingUser is not null)
        {
            return IdentityOperationResult.Failure("Email is already in use in this tenant.");
        }

        // Generate tenant-scoped username to allow same email across tenants
        // USERNAME FORMAT:
        // - Platform users (TenantId = null): email (e.g., "admin@noir.local")
        // - Tenant users: email#tenantId (e.g., "user@example.com#550e8400-e29b-41d4-a716-446655440000")
        // This allows the same email to exist in multiple tenants while maintaining username uniqueness globally.
        // ASP.NET Core Identity requires unique usernames, but we want per-tenant email uniqueness.
        var userName = string.IsNullOrWhiteSpace(dto.TenantId)
            ? dto.Email
            : $"{dto.Email}#{dto.TenantId}";

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DisplayName = dto.DisplayName,
            TenantId = dto.TenantId,
            IsActive = true,
            EmailConfirmed = true  // Auto-confirm email for admin-created users
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Select(e => e.Description)
                .ToArray();

            return IdentityOperationResult.Failure(errors);
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
            user.FirstName = string.IsNullOrWhiteSpace(updates.FirstName) ? null : updates.FirstName;
        if (updates.LastName is not null)
            user.LastName = string.IsNullOrWhiteSpace(updates.LastName) ? null : updates.LastName;
        if (updates.DisplayName is not null)
            user.DisplayName = string.IsNullOrWhiteSpace(updates.DisplayName) ? null : updates.DisplayName;
        if (updates.PhoneNumber is not null)
            user.PhoneNumber = updates.PhoneNumber;
        if (updates.AvatarUrl is not null)
            user.AvatarUrl = updates.AvatarUrl;
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

    public async Task<IdentityOperationResult> SetUserLockoutAsync(
        string userId,
        bool locked,
        string? lockedBy = null,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityOperationResult.Failure("User not found.");
        }

        user.IsActive = !locked;
        
        if (locked)
        {
            // Set lockout end to far future to prevent login
            user.LockoutEnd = DateTimeOffset.MaxValue;
            user.LockedAt = _dateTime.UtcNow;
            user.LockedBy = lockedBy;
        }
        else
        {
            // Clear lockout to allow login
            user.LockoutEnd = null;
            user.LockedAt = null;
            user.LockedBy = null;
        }

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

    public async Task<IdentityOperationResult> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityOperationResult.Failure("User not found.");
        }

        // Use ChangePasswordAsync which verifies the current password
        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

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

    public async Task<IdentityOperationResult> UpdateEmailAsync(
        string userId,
        string newEmail,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityOperationResult.Failure("User not found.");
        }

        // Check per-tenant email uniqueness (email can exist in different tenants, but not in the same tenant)
        var existingUser = await FindByEmailAsync(newEmail, user.TenantId, ct);
        if (existingUser is not null && existingUser.Id != userId)
        {
            return IdentityOperationResult.Failure("Email is already in use in this tenant.");
        }

        // Generate tenant-scoped username (consistent with CreateUserAsync)
        // USERNAME FORMAT:
        // - Platform users (TenantId = null): email (e.g., "admin@noir.local")
        // - Tenant users: email#tenantId (e.g., "user@example.com#550e8400-e29b-41d4-a716-446655440000")
        var newUserName = user.TenantId is null
            ? newEmail
            : $"{newEmail}#{user.TenantId}";

        // Update email and username
        user.Email = newEmail;
        user.NormalizedEmail = _userManager.NormalizeEmail(newEmail);
        user.UserName = newUserName;
        user.NormalizedUserName = _userManager.NormalizeName(newUserName);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(
                result.Errors.Select(e => e.Description).ToArray());
        }

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

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetRolesForUsersAsync(
        IEnumerable<string> userIds,
        CancellationToken ct = default)
    {
        var userIdList = userIds.ToList();
        if (userIdList.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<string>>();
        }

        // Get all users in a single query
        var users = await _userManager.Users
            .Where(u => userIdList.Contains(u.Id))
            .ToListAsync(ct);

        // Build result dictionary - need to call GetRolesAsync for each user
        // This is optimized by batching the user lookup but still requires role lookups
        var result = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result[user.Id] = roles.ToList();
        }

        return result;
    }

    public async Task<IReadOnlyList<UserIdentityDto>> GetUsersInRoleAsync(
        string? tenantId,
        string roleName,
        CancellationToken ct = default)
    {
        // Get users in role efficiently using UserManager
        var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

        // Filter by tenant and map to DTO
        return usersInRole
            .Where(u => !u.IsDeleted && u.TenantId == tenantId)
            .Select(MapToDto)
            .ToList();
    }

    #endregion

    #region Mapping

    private static UserIdentityDto MapToDto(ApplicationUser user)
    {
        return new UserIdentityDto(
            user.Id,
            user.Email!,
            user.TenantId,
            user.FirstName,
            user.LastName,
            user.DisplayName,
            user.FullName,
            user.PhoneNumber,
            user.AvatarUrl,
            user.IsActive,
            user.IsDeleted,
            user.IsSystemUser,
            user.CreatedAt,
            user.ModifiedAt);
    }

    #endregion
}
