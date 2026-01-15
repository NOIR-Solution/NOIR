using NOIR.Application.Features.Permissions.Queries.GetAllPermissions;
using NOIR.Application.Features.Permissions.Queries.GetPermissionTemplates;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Permission management API endpoints.
/// </summary>
public static class PermissionEndpoints
{
    public static void MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        // Get all permissions
        app.MapGet("/api/permissions", async (IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<IReadOnlyList<PermissionDto>>>(new GetAllPermissionsQuery());
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.RolesRead)
        .WithTags("Permissions")
        .WithName("GetAllPermissions")
        .WithSummary("Get all available permissions with metadata")
        .Produces<IReadOnlyList<PermissionDto>>(StatusCodes.Status200OK);

        // Get permission templates
        app.MapGet("/api/permission-templates", async (
            [AsParameters] GetPermissionTemplatesQuery query,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<IReadOnlyList<PermissionTemplateDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.RolesRead)
        .WithTags("Permissions")
        .WithName("GetPermissionTemplates")
        .WithSummary("Get all permission templates")
        .Produces<IReadOnlyList<PermissionTemplateDto>>(StatusCodes.Status200OK);
    }
}
