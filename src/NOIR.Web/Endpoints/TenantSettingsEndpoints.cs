using NOIR.Application.Features.TenantSettings.Commands.UpdateBrandingSettings;
using NOIR.Application.Features.TenantSettings.Commands.UpdateContactSettings;
using NOIR.Application.Features.TenantSettings.Commands.UpdateRegionalSettings;
using NOIR.Application.Features.TenantSettings.Queries.GetBrandingSettings;
using NOIR.Application.Features.TenantSettings.Queries.GetContactSettings;
using NOIR.Application.Features.TenantSettings.Queries.GetRegionalSettings;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Tenant settings API endpoints.
/// Manages tenant-level configuration for branding, contact, and regional settings.
/// </summary>
public static class TenantSettingsEndpoints
{
    public static void MapTenantSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenant-settings")
            .WithTags("Tenant Settings")
            .RequireAuthorization();

        MapBrandingEndpoints(group);
        MapContactEndpoints(group);
        MapRegionalEndpoints(group);
    }

    private static void MapBrandingEndpoints(RouteGroupBuilder group)
    {
        // Get branding settings
        group.MapGet("/branding", async (IMessageBus bus) =>
        {
            var query = new GetBrandingSettingsQuery();
            var result = await bus.InvokeAsync<Result<BrandingSettingsDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantSettingsRead)
        .WithName("GetBrandingSettings")
        .WithSummary("Get tenant branding settings")
        .WithDescription("Returns the current tenant branding configuration including logo, colors, and dark mode preference.")
        .Produces<BrandingSettingsDto>(StatusCodes.Status200OK);

        // Update branding settings
        group.MapPut("/branding", async (
            UpdateBrandingSettingsRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateBrandingSettingsCommand(
                request.LogoUrl,
                request.FaviconUrl,
                request.PrimaryColor,
                request.SecondaryColor,
                request.DarkModeDefault)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<BrandingSettingsDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantSettingsUpdate)
        .WithName("UpdateBrandingSettings")
        .WithSummary("Update tenant branding settings")
        .WithDescription("Updates the tenant branding configuration. All fields are optional - unset fields will clear the value.")
        .Produces<BrandingSettingsDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }

    private static void MapContactEndpoints(RouteGroupBuilder group)
    {
        // Get contact settings
        group.MapGet("/contact", async (IMessageBus bus) =>
        {
            var query = new GetContactSettingsQuery();
            var result = await bus.InvokeAsync<Result<ContactSettingsDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantSettingsRead)
        .WithName("GetContactSettings")
        .WithSummary("Get tenant contact settings")
        .WithDescription("Returns the current tenant contact information (email, phone, address).")
        .Produces<ContactSettingsDto>(StatusCodes.Status200OK);

        // Update contact settings
        group.MapPut("/contact", async (
            UpdateContactSettingsRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateContactSettingsCommand(
                request.Email,
                request.Phone,
                request.Address)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ContactSettingsDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantSettingsUpdate)
        .WithName("UpdateContactSettings")
        .WithSummary("Update tenant contact settings")
        .WithDescription("Updates the tenant contact information. All fields are optional - unset fields will clear the value.")
        .Produces<ContactSettingsDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }

    private static void MapRegionalEndpoints(RouteGroupBuilder group)
    {
        // Get regional settings
        group.MapGet("/regional", async (IMessageBus bus) =>
        {
            var query = new GetRegionalSettingsQuery();
            var result = await bus.InvokeAsync<Result<RegionalSettingsDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantSettingsRead)
        .WithName("GetRegionalSettings")
        .WithSummary("Get tenant regional settings")
        .WithDescription("Returns the current tenant regional configuration (timezone, language, date format).")
        .Produces<RegionalSettingsDto>(StatusCodes.Status200OK);

        // Update regional settings
        group.MapPut("/regional", async (
            UpdateRegionalSettingsRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateRegionalSettingsCommand(
                request.Timezone,
                request.Language,
                request.DateFormat)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<RegionalSettingsDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantSettingsUpdate)
        .WithName("UpdateRegionalSettings")
        .WithSummary("Update tenant regional settings")
        .WithDescription("Updates the tenant regional configuration including timezone, language, and date format.")
        .Produces<RegionalSettingsDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }
}
