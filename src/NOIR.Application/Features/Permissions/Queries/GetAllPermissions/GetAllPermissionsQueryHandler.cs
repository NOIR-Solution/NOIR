namespace NOIR.Application.Features.Permissions.Queries.GetAllPermissions;

/// <summary>
/// Wolverine handler for getting all available permissions.
/// Returns permissions with display metadata and tenant scope information.
/// </summary>
public class GetAllPermissionsQueryHandler
{
    public Task<Result<IReadOnlyList<PermissionDto>>> Handle(GetAllPermissionsQuery query, CancellationToken cancellationToken)
    {
        var permissions = PermissionDtoFactory.GetAllPermissions();
        return Task.FromResult(Result.Success(permissions));
    }
}
