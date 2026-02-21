using NOIR.Application.Features.Tenants.Commands.CreateTenant;
using NOIR.Application.Features.Tenants.Commands.DeleteTenant;
using NOIR.Application.Features.Tenants.Commands.ProvisionTenant;
using NOIR.Application.Features.Tenants.Commands.ResetTenantAdminPassword;
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
            .RequireFeature(ModuleNames.Platform.Tenants)
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
        group.MapPost("/", async (
            CreateTenantCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<TenantDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantsCreate)
        .WithName("CreateTenant")
        .WithSummary("Create a new tenant")
        .Produces<TenantDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Provision tenant (create with admin user)
        group.MapPost("/provision", async (
            ProvisionTenantCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ProvisionTenantResult>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantsCreate)
        .WithName("ProvisionTenant")
        .WithSummary("Provision a new tenant with admin user")
        .WithDescription("Creates a new tenant and optionally creates an admin user for that tenant. " +
                        "This is the recommended way to create tenants as it handles all setup in one operation.")
        .Produces<ProvisionTenantResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update tenant
        group.MapPut("/{tenantId:guid}", async (
            Guid tenantId,
            UpdateTenantRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var cmd = UpdateTenantCommand.FromRequest(tenantId, request);
            var auditableCommand = cmd with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<TenantDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantsUpdate)
        .WithName("UpdateTenant")
        .WithSummary("Update tenant details")
        .Produces<TenantDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete tenant (soft delete)
        group.MapDelete("/{tenantId:guid}", async (
            Guid tenantId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteTenantCommand(tenantId) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantsDelete)
        .WithName("DeleteTenant")
        .WithSummary("Delete a tenant (soft delete)")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Reset tenant admin password
        group.MapPost("/{tenantId}/reset-admin-password", async (
            string tenantId,
            ResetTenantAdminPasswordRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ResetTenantAdminPasswordCommand(tenantId, request.NewPassword) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ResetTenantAdminPasswordResult>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantsUpdate)
        .WithName("ResetTenantAdminPassword")
        .WithSummary("Reset the password of a tenant's admin user")
        .WithDescription("Allows platform administrators to reset the password for a tenant's admin user. " +
                        "This is useful when the tenant admin forgets their password.")
        .Produces<ResetTenantAdminPasswordResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}

/// <summary>
/// Request body for resetting tenant admin password.
/// </summary>
public record ResetTenantAdminPasswordRequest(string NewPassword);
