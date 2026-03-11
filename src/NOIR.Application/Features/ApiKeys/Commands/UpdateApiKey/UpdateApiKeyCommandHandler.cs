using NOIR.Application.Features.ApiKeys.DTOs;
using NOIR.Application.Features.ApiKeys.Specifications;

namespace NOIR.Application.Features.ApiKeys.Commands.UpdateApiKey;

/// <summary>
/// Handler for updating an API key's metadata and permissions.
/// </summary>
public class UpdateApiKeyCommandHandler
{
    private readonly IRepository<Domain.Entities.ApiKey, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IRoleIdentityService _roleIdentityService;

    public UpdateApiKeyCommandHandler(
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

    public async Task<Result<ApiKeyDto>> Handle(
        UpdateApiKeyCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new ApiKeyByIdForUpdateSpec(command.Id);
        var key = await _repository.FirstOrDefaultAsync(spec, cancellationToken);
        if (key is null)
        {
            return Result.Failure<ApiKeyDto>(Error.NotFound("ApiKey", command.Id));
        }

        // Only the key owner can update (admins use revoke, not update)
        if (key.UserId != _currentUser.UserId)
        {
            return Result.Failure<ApiKeyDto>(Error.Forbidden("You can only update your own API keys."));
        }

        if (key.IsRevoked)
        {
            return Result.Failure<ApiKeyDto>(
                Error.Validation("ApiKey", "Cannot update a revoked API key.", "NOIR-APIKEY-003"));
        }

        // Resolve user's effective permissions from roles
        var userId = _currentUser.UserId!;
        var roles = await _userIdentityService.GetRolesAsync(userId, cancellationToken);
        var userPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleName in roles)
        {
            var role = await _roleIdentityService.FindByNameAsync(roleName, cancellationToken);
            if (role is not null)
            {
                var rolePermissions = await _roleIdentityService.GetPermissionsAsync(role.Id, cancellationToken);
                userPermissions.UnionWith(rolePermissions);
            }
        }

        var invalidPermissions = command.Permissions
            .Where(p => !userPermissions.Contains(p))
            .ToList();
        if (invalidPermissions.Count > 0)
        {
            return Result.Failure<ApiKeyDto>(
                Error.Validation("Permissions", $"You cannot assign permissions you don't have: {string.Join(", ", invalidPermissions)}", "NOIR-APIKEY-002"));
        }

        key.Update(command.Name, command.Description, command.Permissions);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ApiKeyMapper.ToDto(key));
    }
}
