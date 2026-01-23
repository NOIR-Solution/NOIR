using NOIR.Application.Features.PlatformSettings.Commands.TestSmtpConnection;
using NOIR.Application.Features.PlatformSettings.Commands.UpdateSmtpSettings;
using NOIR.Application.Features.PlatformSettings.DTOs;
using NOIR.Application.Features.PlatformSettings.Queries.GetSmtpSettings;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Platform settings API endpoints.
/// Platform admin only - manages SMTP and other platform-level configuration.
/// </summary>
public static class PlatformSettingsEndpoints
{
    public static void MapPlatformSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/platform-settings")
            .WithTags("Platform Settings")
            .RequireAuthorization();

        MapSmtpEndpoints(group);
    }

    private static void MapSmtpEndpoints(RouteGroupBuilder group)
    {
        // Get SMTP settings
        group.MapGet("/smtp", async (IMessageBus bus) =>
        {
            var query = new GetSmtpSettingsQuery();
            var result = await bus.InvokeAsync<Result<SmtpSettingsDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PlatformSettingsRead)
        .WithName("GetSmtpSettings")
        .WithSummary("Get platform SMTP settings")
        .WithDescription("Returns the current platform SMTP configuration. Password is never returned, only a flag indicating if one is set.")
        .Produces<SmtpSettingsDto>(StatusCodes.Status200OK);

        // Update SMTP settings
        group.MapPut("/smtp", async (
            UpdateSmtpSettingsRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateSmtpSettingsCommand(
                request.Host,
                request.Port,
                request.Username,
                request.Password,
                request.FromEmail,
                request.FromName,
                request.UseSsl)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<SmtpSettingsDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PlatformSettingsManage)
        .WithName("UpdateSmtpSettings")
        .WithSummary("Update platform SMTP settings")
        .WithDescription("Updates the platform SMTP configuration. Set password to null to keep existing, empty string to clear.")
        .Produces<SmtpSettingsDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Test SMTP connection
        group.MapPost("/smtp/test", async (
            TestSmtpRequest request,
            IMessageBus bus) =>
        {
            var command = new TestSmtpConnectionCommand(request.RecipientEmail);
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PlatformSettingsManage)
        .WithName("TestSmtpConnection")
        .WithSummary("Test SMTP connection")
        .WithDescription("Sends a test email using the currently configured platform SMTP settings.")
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }
}

// Request DTOs for the endpoints
public sealed record UpdateSmtpSettingsRequest(
    string Host,
    int Port,
    string? Username,
    string? Password,
    string FromEmail,
    string FromName,
    bool UseSsl);

public sealed record TestSmtpRequest(string RecipientEmail);
