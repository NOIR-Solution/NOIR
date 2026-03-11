using NOIR.Application.Features.ApiKeys.DTOs;
using NOIR.Application.Features.ApiKeys.Specifications;

namespace NOIR.Application.Features.ApiKeys.Queries.GetMyApiKeys;

/// <summary>
/// Query to get the current user's API keys (profile tab).
/// </summary>
public sealed record GetMyApiKeysQuery;

/// <summary>
/// Handler for GetMyApiKeysQuery.
/// </summary>
public class GetMyApiKeysQueryHandler
{
    private readonly IRepository<Domain.Entities.ApiKey, Guid> _repository;
    private readonly ICurrentUser _currentUser;

    public GetMyApiKeysQueryHandler(
        IRepository<Domain.Entities.ApiKey, Guid> repository,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<List<ApiKeyDto>>> Handle(
        GetMyApiKeysQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new ApiKeysByUserIdSpec(_currentUser.UserId!);
        var keys = await _repository.ListAsync(spec, cancellationToken);

        var dtos = keys.Select(k => ApiKeyMapper.ToDto(k)).ToList();
        return Result.Success(dtos);
    }
}
