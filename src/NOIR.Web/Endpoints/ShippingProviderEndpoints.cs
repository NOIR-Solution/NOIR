using NOIR.Application.Features.Shipping.Commands.ConfigureShippingProvider;
using NOIR.Application.Features.Shipping.Commands.UpdateShippingProvider;
using NOIR.Application.Features.Shipping.Queries.GetActiveShippingProviders;
using NOIR.Application.Features.Shipping.Queries.GetShippingProviderById;
using NOIR.Application.Features.Shipping.Queries.GetShippingProviders;
using NOIR.Application.Features.Shipping.Queries.GetShippingProviderSchemas;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Shipping provider management endpoints.
/// </summary>
public static class ShippingProviderEndpoints
{
    public static void MapShippingProviderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/shipping-providers")
            .WithTags("Shipping Providers")
            .RequireFeature(ModuleNames.Ecommerce.Checkout)
            .RequireAuthorization();

        // Get all providers (admin)
        group.MapGet("/", async (IMessageBus bus) =>
        {
            var query = new GetShippingProvidersQuery();
            var result = await bus.InvokeAsync<Result<List<ShippingProviderDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersManage)
        .WithName("GetShippingProviders")
        .WithSummary("Get all shipping providers")
        .WithDescription("Returns all configured shipping providers for the tenant.")
        .Produces<List<ShippingProviderDto>>(StatusCodes.Status200OK);

        // Get active providers for checkout
        group.MapGet("/active", async (IMessageBus bus) =>
        {
            var query = new GetActiveShippingProvidersQuery();
            var result = await bus.InvokeAsync<Result<List<CheckoutShippingProviderDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("GetActiveShippingProviders")
        .WithSummary("Get active shipping providers for checkout")
        .WithDescription("Returns active providers available for shipping selection during checkout.")
        .Produces<List<CheckoutShippingProviderDto>>(StatusCodes.Status200OK);

        // Get shipping provider credential schemas
        group.MapGet("/schemas", async (IMessageBus bus) =>
        {
            var query = new GetShippingProviderSchemasQuery();
            var result = await bus.InvokeAsync<Result<ShippingProviderSchemasDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersManage)
        .WithName("GetShippingProviderSchemas")
        .WithSummary("Get shipping provider credential schemas")
        .WithDescription("Returns credential field definitions for all supported shipping providers.")
        .Produces<ShippingProviderSchemasDto>(StatusCodes.Status200OK);

        // Get provider by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetShippingProviderByIdQuery(id);
            var result = await bus.InvokeAsync<Result<ShippingProviderDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersManage)
        .WithName("GetShippingProviderById")
        .WithSummary("Get shipping provider by ID")
        .WithDescription("Returns provider configuration details (credentials are not exposed).")
        .Produces<ShippingProviderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Configure new provider
        group.MapPost("/", async (
            ConfigureShippingProviderRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ConfigureShippingProviderCommand(
                request.ProviderCode,
                request.DisplayName,
                request.Environment,
                request.Credentials,
                request.SupportedServices,
                request.SortOrder,
                request.IsActive,
                request.SupportsCod,
                request.SupportsInsurance,
                request.ApiBaseUrl,
                request.TrackingUrlTemplate)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ShippingProviderDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersManage)
        .WithName("ConfigureShippingProvider")
        .WithSummary("Configure a new shipping provider")
        .WithDescription("Sets up a new shipping provider with credentials (encrypted at rest).")
        .Produces<ShippingProviderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update provider
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateShippingProviderRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateShippingProviderCommand(
                id,
                request.DisplayName,
                request.Environment,
                request.Credentials,
                request.SupportedServices,
                request.SortOrder,
                request.IsActive,
                request.SupportsCod,
                request.SupportsInsurance,
                request.ApiBaseUrl,
                request.TrackingUrlTemplate,
                request.MinWeightGrams,
                request.MaxWeightGrams,
                request.MinCodAmount,
                request.MaxCodAmount)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ShippingProviderDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersManage)
        .WithName("UpdateShippingProvider")
        .WithSummary("Update shipping provider")
        .WithDescription("Updates provider configuration. Pass null for fields to keep unchanged.")
        .Produces<ShippingProviderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Activate provider
        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateShippingProviderCommand(id, IsActive: true)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ShippingProviderDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersManage)
        .WithName("ActivateShippingProvider")
        .WithSummary("Activate shipping provider")
        .WithDescription("Enables a shipping provider for use in checkout.")
        .Produces<ShippingProviderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Deactivate provider
        group.MapPost("/{id:guid}/deactivate", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateShippingProviderCommand(id, IsActive: false)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ShippingProviderDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersManage)
        .WithName("DeactivateShippingProvider")
        .WithSummary("Deactivate shipping provider")
        .WithDescription("Disables a shipping provider from checkout.")
        .Produces<ShippingProviderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
