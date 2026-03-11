using NOIR.Application.Features.ApiKeys.DTOs;
using NOIR.Application.Features.ApiKeys.Specifications;

namespace NOIR.Application.Features.ApiKeys.Commands.RevokeApiKey;

/// <summary>
/// Handler for revoking an API key.
/// Key owners can revoke their own keys. Admin access is enforced at the endpoint level.
/// </summary>
public class RevokeApiKeyCommandHandler
{
    private readonly IRepository<Domain.Entities.ApiKey, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public RevokeApiKeyCommandHandler(
        IRepository<Domain.Entities.ApiKey, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<ApiKeyDto>> Handle(
        RevokeApiKeyCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new ApiKeyByIdForUpdateSpec(command.Id);
        var key = await _repository.FirstOrDefaultAsync(spec, cancellationToken);
        if (key is null)
        {
            return Result.Failure<ApiKeyDto>(Error.NotFound("ApiKey", command.Id));
        }

        // Owner can revoke own keys; admins verified at endpoint level via RequirePermission
        var isOwner = key.UserId == _currentUser.UserId;
        if (!isOwner && !_currentUser.IsPlatformAdmin)
        {
            return Result.Failure<ApiKeyDto>(Error.Forbidden("You can only revoke your own API keys."));
        }

        if (key.IsRevoked)
        {
            return Result.Failure<ApiKeyDto>(
                Error.Validation("ApiKey", "This API key is already revoked.", "NOIR-APIKEY-005"));
        }

        key.Revoke(command.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ApiKeyMapper.ToDto(key));
    }
}
