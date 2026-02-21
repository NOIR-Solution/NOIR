using NOIR.Application.Features.FeatureManagement.Commands.SetModuleAvailability;
using NOIR.Application.Features.FeatureManagement.Commands.ToggleModule;
using NOIR.Application.Features.FeatureManagement.DTOs;
using NOIR.Application.Features.FeatureManagement.Queries.GetCurrentTenantFeatures;
using NOIR.Application.Features.FeatureManagement.Queries.GetModuleCatalog;
using NOIR.Application.Features.FeatureManagement.Queries.GetTenantFeatureStates;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Feature management API endpoints.
/// Provides operations for viewing and managing module/feature availability and toggling.
/// </summary>
public static class FeatureManagementEndpoints
{
    public static void MapFeatureManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/features")
            .WithTags("Feature Management")
            .RequireAuthorization();

        // Get current tenant features (any authenticated user)
        group.MapGet("/current-tenant", async (IMessageBus bus) =>
        {
            var query = new GetCurrentTenantFeaturesQuery();
            var result = await bus.InvokeAsync<Result<IReadOnlyDictionary<string, EffectiveFeatureState>>>(query);
            return result.ToHttpResult();
        })
        .WithName("GetCurrentTenantFeatures")
        .WithSummary("Get current tenant feature states")
        .WithDescription("Returns all effective feature states for the current tenant. Available to any authenticated user.")
        .Produces<IReadOnlyDictionary<string, EffectiveFeatureState>>(StatusCodes.Status200OK);

        // Get module catalog (tenant settings read permission)
        group.MapGet("/catalog", async (IMessageBus bus) =>
        {
            var query = new GetModuleCatalogQuery();
            var result = await bus.InvokeAsync<Result<ModuleCatalogDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantSettingsRead)
        .WithName("GetModuleCatalog")
        .WithSummary("Get module catalog")
        .WithDescription("Returns the full module catalog with all code-defined modules and features.")
        .Produces<ModuleCatalogDto>(StatusCodes.Status200OK);

        // Get features for a specific tenant (platform admin)
        group.MapGet("/tenant/{tenantId}", async (string tenantId, IMessageBus bus) =>
        {
            var query = new GetTenantFeatureStatesQuery(tenantId);
            var result = await bus.InvokeAsync<Result<ModuleCatalogDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.FeaturesRead)
        .WithName("GetTenantFeatureStates")
        .WithSummary("Get feature states for a specific tenant")
        .WithDescription("Returns module catalog with tenant-specific availability and enablement states. Platform admin only.")
        .Produces<ModuleCatalogDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Set module availability for a tenant (platform admin)
        group.MapPut("/tenant/{tenantId}/availability", async (
            string tenantId,
            SetAvailabilityRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new SetModuleAvailabilityCommand(tenantId, request.FeatureName, request.IsAvailable)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<TenantFeatureStateDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.FeaturesUpdate)
        .WithName("SetModuleAvailability")
        .WithSummary("Set module availability for a tenant")
        .WithDescription("Sets whether a module is available to a specific tenant. Platform admin only. Core modules cannot be modified.")
        .Produces<TenantFeatureStateDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        // Toggle module for current tenant (tenant admin)
        group.MapPut("/toggle", async (
            ToggleModuleRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ToggleModuleCommand(request.FeatureName, request.IsEnabled)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<TenantFeatureStateDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantSettingsUpdate)
        .WithName("ToggleModule")
        .WithSummary("Toggle module for current tenant")
        .WithDescription("Enables or disables a module for the current tenant. Requires tenant settings update permission. Core modules cannot be toggled.")
        .Produces<TenantFeatureStateDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);
    }
}

public sealed record SetAvailabilityRequest(string FeatureName, bool IsAvailable);
public sealed record ToggleModuleRequest(string FeatureName, bool IsEnabled);
