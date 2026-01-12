using NOIR.Application.Features.Tenants.Commands.CreateTenant;
using NOIR.Application.Features.Tenants.Commands.DeleteTenant;
using NOIR.Application.Features.Tenants.Commands.UpdateTenant;
using NOIR.Application.Features.Tenants.Queries.GetTenantById;
using NOIR.Application.Features.Tenants.Queries.GetTenants;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Tenant management API endpoints.
/// System admin only - for managing tenants in the multi-tenant system.
/// </summary>
public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants")
            .WithTags("Tenants")
            .RequireAuthorization();

        // Get all tenants (paginated)
        group.MapGet("/", async (
            [AsParameters] GetTenantsQuery query,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<PaginatedList<TenantListDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantsRead)
        .WithName("GetTenants")
        .WithSummary("Get paginated list of tenants")
        .Produces<PaginatedList<TenantListDto>>(StatusCodes.Status200OK);

        // Get tenant by ID
        group.MapGet("/{tenantId:guid}", async (Guid tenantId, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<TenantDto>>(new GetTenantByIdQuery(tenantId));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantsRead)
        .WithName("GetTenantById")
        .WithSummary("Get tenant by ID with full details")
        .Produces<TenantDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Create tenant
        group.MapPost("/", async (CreateTenantCommand command, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<TenantDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantsCreate)
        .WithName("CreateTenant")
        .WithSummary("Create a new tenant")
        .Produces<TenantDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update tenant
        group.MapPut("/{tenantId:guid}", async (
            Guid tenantId,
            UpdateTenantCommand command,
            IMessageBus bus) =>
        {
            var cmd = command with { TenantId = tenantId };
            var result = await bus.InvokeAsync<Result<TenantDto>>(cmd);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantsUpdate)
        .WithName("UpdateTenant")
        .WithSummary("Update tenant details")
        .Produces<TenantDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete tenant (soft delete)
        group.MapDelete("/{tenantId:guid}", async (Guid tenantId, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<bool>>(new DeleteTenantCommand(tenantId));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantsDelete)
        .WithName("DeleteTenant")
        .WithSummary("Delete a tenant (soft delete)")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
