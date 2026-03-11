using NOIR.Application.Features.ApiKeys.DTOs;
using NOIR.Application.Features.ApiKeys.Specifications;

namespace NOIR.Application.Features.ApiKeys.Commands.RotateApiKey;

/// <summary>
/// Handler for rotating an API key's secret.
/// </summary>
public class RotateApiKeyCommandHandler
{
    private readonly IRepository<Domain.Entities.ApiKey, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public RotateApiKeyCommandHandler(
        IRepository<Domain.Entities.ApiKey, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<ApiKeyRotatedDto>> Handle(
        RotateApiKeyCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new ApiKeyByIdForUpdateSpec(command.Id);
        var key = await _repository.FirstOrDefaultAsync(spec, cancellationToken);
        if (key is null)
        {
            return Result.Failure<ApiKeyRotatedDto>(Error.NotFound("ApiKey", command.Id));
        }

        if (key.UserId != _currentUser.UserId)
        {
            return Result.Failure<ApiKeyRotatedDto>(Error.Forbidden("You can only rotate your own API keys."));
        }

        if (key.IsRevoked)
        {
            return Result.Failure<ApiKeyRotatedDto>(
                Error.Validation("ApiKey", "Cannot rotate a revoked API key.", "NOIR-APIKEY-004"));
        }

        var newSecret = key.RotateSecret();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ApiKeyMapper.ToRotatedDto(key, newSecret));
    }
}
