using NOIR.Application.Features.ApiKeys.DTOs;
using NOIR.Application.Features.ApiKeys.Specifications;

namespace NOIR.Application.Features.ApiKeys.Queries.GetTenantApiKeys;

/// <summary>
/// Query to get all API keys in the tenant (admin view).
/// </summary>
public sealed record GetTenantApiKeysQuery;

/// <summary>
/// Handler for GetTenantApiKeysQuery.
/// </summary>
public class GetTenantApiKeysQueryHandler
{
    private readonly IRepository<Domain.Entities.ApiKey, Guid> _repository;
    private readonly IUserIdentityService _userIdentityService;

    public GetTenantApiKeysQueryHandler(
        IRepository<Domain.Entities.ApiKey, Guid> repository,
        IUserIdentityService userIdentityService)
    {
        _repository = repository;
        _userIdentityService = userIdentityService;
    }

    public async Task<Result<List<ApiKeyDto>>> Handle(
        GetTenantApiKeysQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new ApiKeysByTenantSpec();
        var keys = await _repository.ListAsync(spec, cancellationToken);

        // Batch resolve user display names
        var userIds = keys.Select(k => k.UserId).Distinct().ToList();
        var userNames = new Dictionary<string, string>();
        foreach (var userId in userIds)
        {
            var user = await _userIdentityService.FindByIdAsync(userId, cancellationToken);
            if (user is not null)
            {
                userNames[userId] = user.DisplayName ?? user.Email;
            }
        }

        var dtos = keys.Select(k => ApiKeyMapper.ToDto(k, userNames.GetValueOrDefault(k.UserId))).ToList();
        return Result.Success(dtos);
    }
}
