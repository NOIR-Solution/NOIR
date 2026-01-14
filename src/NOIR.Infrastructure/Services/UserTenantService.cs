namespace NOIR.Infrastructure.Services;

/// <summary>
/// Implementation of IUserTenantService for managing user-tenant memberships.
/// </summary>
public class UserTenantService : IUserTenantService, IScopedService
{
    private readonly ApplicationDbContext _context;
    private readonly IMultiTenantStore<Tenant> _tenantStore;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocalizationService _localizer;
    private readonly ILogger<UserTenantService> _logger;

    public UserTenantService(
        ApplicationDbContext context,
        IMultiTenantStore<Tenant> tenantStore,
        IUserIdentityService userIdentityService,
        IUnitOfWork unitOfWork,
        ILocalizationService localizer,
        ILogger<UserTenantService> logger)
    {
        _context = context;
        _tenantStore = tenantStore;
        _userIdentityService = userIdentityService;
        _unitOfWork = unitOfWork;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserTenantMembershipDto>> GetUserTenantsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var spec = new UserMembershipsByUserIdSpec(userId);
        var memberships = await SpecificationEvaluator
            .GetQuery(_context.UserTenantMemberships, spec)
            .ToListAsync(cancellationToken);

        return memberships.Select(m => new UserTenantMembershipDto(
            TenantId: m.TenantId,
            TenantName: m.Tenant?.Name ?? "Unknown",
            TenantSlug: m.Tenant?.Identifier ?? "",
            Role: m.Role,
            JoinedAt: m.JoinedAt,
            IsDefault: m.IsDefault)).ToList();
    }

    public async Task<IReadOnlyList<TenantUserMembershipDto>> GetTenantUsersAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var spec = new TenantMembershipsByTenantIdSpec(tenantId);
        var memberships = await SpecificationEvaluator
            .GetQuery(_context.UserTenantMemberships, spec)
            .ToListAsync(cancellationToken);

        var result = new List<TenantUserMembershipDto>();
        foreach (var m in memberships)
        {
            var user = await _userIdentityService.FindByIdAsync(m.UserId, cancellationToken);
            if (user != null)
            {
                result.Add(new TenantUserMembershipDto(
                    UserId: m.UserId,
                    Email: user.Email,
                    FullName: user.FullName,
                    AvatarUrl: user.AvatarUrl,
                    Role: m.Role,
                    JoinedAt: m.JoinedAt,
                    IsActive: user.IsActive));
            }
        }

        return result;
    }

    public async Task<PaginatedTenantUsersDto> GetTenantUsersPaginatedAsync(
        string tenantId,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new TenantMembershipsPaginatedSpec(tenantId, pageNumber, pageSize);
        var memberships = await SpecificationEvaluator
            .GetQuery(_context.UserTenantMemberships, spec)
            .ToListAsync(cancellationToken);

        var countSpec = new TenantMembershipsByTenantIdSpec(tenantId);
        var totalCount = await SpecificationEvaluator
            .GetQuery(_context.UserTenantMemberships, countSpec)
            .CountAsync(cancellationToken);

        var result = new List<TenantUserMembershipDto>();
        foreach (var m in memberships)
        {
            var user = await _userIdentityService.FindByIdAsync(m.UserId, cancellationToken);
            if (user != null)
            {
                // Apply search filter if provided
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var searchLower = searchTerm.ToLowerInvariant();
                    if (!user.Email.Contains(searchLower, StringComparison.OrdinalIgnoreCase) &&
                        !(user.FullName?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false))
                    {
                        continue;
                    }
                }

                result.Add(new TenantUserMembershipDto(
                    UserId: m.UserId,
                    Email: user.Email,
                    FullName: user.FullName,
                    AvatarUrl: user.AvatarUrl,
                    Role: m.Role,
                    JoinedAt: m.JoinedAt,
                    IsActive: user.IsActive));
            }
        }

        return new PaginatedTenantUsersDto(
            Items: result,
            TotalCount: totalCount,
            PageNumber: pageNumber,
            PageSize: pageSize);
    }

    public async Task<Result<UserTenantMembershipDto>> AddUserToTenantAsync(
        string userId,
        string tenantId,
        TenantRole role,
        CancellationToken cancellationToken = default)
    {
        // Check if user exists
        var user = await _userIdentityService.FindByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<UserTenantMembershipDto>(
                Error.NotFound(ErrorCodes.Auth.UserNotFound, _localizer["user.notFound"]));
        }

        // Check if tenant exists
        var tenant = await _tenantStore.GetAsync(tenantId);
        if (tenant == null)
        {
            return Result.Failure<UserTenantMembershipDto>(
                Error.NotFound(ErrorCodes.Tenant.NotFound, _localizer["tenant.notFound"]));
        }

        // Check if already a member
        var existingSpec = new UserMembershipByUserAndTenantSpec(userId, tenantId);
        var existing = await SpecificationEvaluator
            .GetQuery(_context.UserTenantMemberships, existingSpec)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            return Result.Failure<UserTenantMembershipDto>(
                Error.Conflict(ErrorCodes.Business.AlreadyExists, _localizer["tenant.membership.alreadyExists"]));
        }

        // Check if this is the user's first tenant (make it default)
        var userMembershipsSpec = new UserMembershipsByUserIdSpec(userId);
        var hasMemberships = await SpecificationEvaluator
            .GetQuery(_context.UserTenantMemberships, userMembershipsSpec)
            .AnyAsync(cancellationToken);
        var isDefault = !hasMemberships;

        var membership = UserTenantMembership.Create(userId, tenantId, role, isDefault);
        _context.UserTenantMemberships.Add(membership);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Added user {UserId} to tenant {TenantId} with role {Role}",
            userId, tenantId, role);

        return Result.Success(new UserTenantMembershipDto(
            TenantId: tenantId,
            TenantName: tenant.Name ?? "Unknown",
            TenantSlug: tenant.Identifier,
            Role: role,
            JoinedAt: membership.JoinedAt,
            IsDefault: isDefault));
    }

    public async Task<Result<bool>> RemoveUserFromTenantAsync(
        string userId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var spec = new UserMembershipByUserAndTenantSpec(userId, tenantId, asTracking: true);
        var membership = await SpecificationEvaluator
            .GetQuery(_context.UserTenantMemberships, spec)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership == null)
        {
            return Result.Failure<bool>(
                Error.NotFound(ErrorCodes.Tenant.MembershipNotFound, _localizer["tenant.membership.notFound"]));
        }

        // If removing default tenant, set another tenant as default
        if (membership.IsDefault)
        {
            var otherSpec = new UserMembershipsByUserIdSpec(userId, excludeTenantId: tenantId);
            var firstOther = await SpecificationEvaluator
                .GetQuery(_context.UserTenantMemberships, otherSpec)
                .FirstOrDefaultAsync(cancellationToken);

            if (firstOther != null)
            {
                // Need to track for update
                var trackingSpec = new UserMembershipByUserAndTenantSpec(userId, firstOther.TenantId, asTracking: true);
                var toUpdate = await SpecificationEvaluator
                    .GetQuery(_context.UserTenantMemberships, trackingSpec)
                    .FirstOrDefaultAsync(cancellationToken);
                toUpdate?.SetAsDefault();
            }
        }

        _context.UserTenantMemberships.Remove(membership);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Removed user {UserId} from tenant {TenantId}",
            userId, tenantId);

        return Result.Success(true);
    }

    public async Task<Result<UserTenantMembershipDto>> UpdateUserRoleInTenantAsync(
        string userId,
        string tenantId,
        TenantRole newRole,
        CancellationToken cancellationToken = default)
    {
        var spec = new UserMembershipByUserAndTenantSpec(userId, tenantId, asTracking: true);
        var membership = await SpecificationEvaluator
            .GetQuery(_context.UserTenantMemberships, spec)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership == null)
        {
            return Result.Failure<UserTenantMembershipDto>(
                Error.NotFound(ErrorCodes.Tenant.MembershipNotFound, _localizer["tenant.membership.notFound"]));
        }

        var tenant = await _tenantStore.GetAsync(tenantId);

        membership.UpdateRole(newRole);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated user {UserId} role in tenant {TenantId} to {Role}",
            userId, tenantId, newRole);

        return Result.Success(new UserTenantMembershipDto(
            TenantId: tenantId,
            TenantName: tenant?.Name ?? "Unknown",
            TenantSlug: tenant?.Identifier ?? "",
            Role: newRole,
            JoinedAt: membership.JoinedAt,
            IsDefault: membership.IsDefault));
    }

    public async Task<TenantRole?> GetUserRoleInTenantAsync(
        string userId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var spec = new UserMembershipByUserAndTenantSpec(userId, tenantId);
        var membership = await SpecificationEvaluator
            .GetQuery(_context.UserTenantMemberships, spec)
            .FirstOrDefaultAsync(cancellationToken);
        return membership?.Role;
    }

    public async Task<bool> IsUserInTenantAsync(
        string userId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var spec = new UserMembershipByUserAndTenantSpec(userId, tenantId);
        return await SpecificationEvaluator
            .GetQuery(_context.UserTenantMemberships, spec)
            .AnyAsync(cancellationToken);
    }

    public async Task<UserTenantMembershipDto?> GetMembershipAsync(
        string userId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var spec = new UserMembershipByUserAndTenantSpec(userId, tenantId);
        var membership = await SpecificationEvaluator
            .GetQuery(_context.UserTenantMemberships, spec)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership == null)
            return null;

        var tenant = await _tenantStore.GetAsync(tenantId);

        return new UserTenantMembershipDto(
            TenantId: tenantId,
            TenantName: tenant?.Name ?? "Unknown",
            TenantSlug: tenant?.Identifier ?? "",
            Role: membership.Role,
            JoinedAt: membership.JoinedAt,
            IsDefault: membership.IsDefault);
    }

    public async Task<int> GetTenantUserCountAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var spec = new TenantMembershipsByTenantIdSpec(tenantId);
        return await SpecificationEvaluator
            .GetQuery(_context.UserTenantMemberships, spec)
            .CountAsync(cancellationToken);
    }
}
