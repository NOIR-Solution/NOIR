using NOIR.Application.Features.ApiKeys.DTOs;
using NOIR.Application.Features.ApiKeys.Specifications;

namespace NOIR.Application.Features.ApiKeys.Commands.CreateApiKey;

/// <summary>
/// Handler for creating a new API key.
/// </summary>
public class CreateApiKeyCommandHandler
{
    private const int MaxKeysPerUser = 10;

    private readonly IRepository<Domain.Entities.ApiKey, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IRoleIdentityService _roleIdentityService;

    public CreateApiKeyCommandHandler(
        IRepository<Domain.Entities.ApiKey, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IUserIdentityService userIdentityService,
        IRoleIdentityService roleIdentityService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _userIdentityService = userIdentityService;
        _roleIdentityService = roleIdentityService;
    }

    public async Task<Result<ApiKeyCreatedDto>> Handle(
        CreateApiKeyCommand command,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!;

        // Enforce max keys per user
        var countSpec = new ActiveApiKeysCountByUserSpec(userId);
        var activeCount = await _repository.CountAsync(countSpec, cancellationToken);
        if (activeCount >= MaxKeysPerUser)
        {
            return Result.Failure<ApiKeyCreatedDto>(
                Error.Validation("ApiKey", $"Maximum number of active API keys ({MaxKeysPerUser}) reached.", "NOIR-APIKEY-001"));
        }

        // Resolve user's effective permissions from roles
        var userPermissions = await ResolveUserPermissionsAsync(userId, cancellationToken);
        var invalidPermissions = command.Permissions
            .Where(p => !userPermissions.Contains(p))
            .ToList();
        if (invalidPermissions.Count > 0)
        {
            return Result.Failure<ApiKeyCreatedDto>(
                Error.Validation("Permissions", $"You cannot assign permissions you don't have: {string.Join(", ", invalidPermissions)}", "NOIR-APIKEY-002"));
        }

        var (key, plaintextSecret) = Domain.Entities.ApiKey.Create(
            command.Name,
            userId,
            command.Permissions,
            command.Description,
            command.ExpiresAt,
            _currentUser.TenantId);

        await _repository.AddAsync(key, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ApiKeyMapper.ToCreatedDto(key, plaintextSecret));
    }

    private async Task<HashSet<string>> ResolveUserPermissionsAsync(string userId, CancellationToken ct)
    {
        var roles = await _userIdentityService.GetRolesAsync(userId, ct);
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleName in roles)
        {
            var role = await _roleIdentityService.FindByNameAsync(roleName, ct);
            if (role is not null)
            {
                var rolePermissions = await _roleIdentityService.GetPermissionsAsync(role.Id, ct);
                permissions.UnionWith(rolePermissions);
            }
        }
        return permissions;
    }
}
