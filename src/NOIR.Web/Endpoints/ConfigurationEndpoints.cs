using NOIR.Application.Common.Interfaces;
using NOIR.Application.Common.Models;
using NOIR.Application.Features.Configuration.Commands.RestartApplication;
using NOIR.Application.Features.Configuration.Commands.UpdateConfiguration;
using NOIR.Domain.Common;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Endpoints for live configuration management.
/// Restricted to platform administrators only.
/// </summary>
public static class ConfigurationEndpoints
{
    public static void MapConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/config")
            .WithTags("Configuration")
            .RequireAuthorization(Permissions.System.ViewConfig);

        // Query endpoints
        group.MapGet("/sections", GetSections)
            .WithName("GetConfigurationSections")
            .WithSummary("Get all available configuration sections");

        group.MapGet("/sections/{sectionName}", GetSection)
            .WithName("GetConfigurationSection")
            .WithSummary("Get a specific configuration section");

        group.MapGet("/backups", GetBackups)
            .WithName("GetConfigurationBackups")
            .WithSummary("Get all configuration backups");

        group.MapGet("/restart/status", GetRestartStatus)
            .WithName("GetRestartStatus")
            .WithSummary("Get restart capability and cooldown status");

        // Command endpoints (require EditConfig permission)
        group.MapPut("/sections/{sectionName}", UpdateSection)
            .RequireAuthorization(Permissions.System.EditConfig)
            .WithName("UpdateConfigurationSection")
            .WithSummary("Update a configuration section");

        group.MapPost("/backups/{backupId}/rollback", RollbackBackup)
            .RequireAuthorization(Permissions.System.EditConfig)
            .WithName("RollbackConfiguration")
            .WithSummary("Rollback configuration to a backup");

        group.MapPost("/restart", RestartApplication)
            .RequireAuthorization(Permissions.System.RestartApp)
            .WithName("RestartApplication")
            .WithSummary("Restart the application");
    }

    // GET /api/admin/config/sections
    private static async Task<IResult> GetSections(
        [FromServices] IConfigurationManagementService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAvailableSectionsAsync(cancellationToken);
        return result.ToHttpResult();
    }

    // GET /api/admin/config/sections/{sectionName}
    private static async Task<IResult> GetSection(
        string sectionName,
        [FromServices] IConfigurationManagementService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetSectionAsync(sectionName, cancellationToken);
        return result.ToHttpResult();
    }

    // PUT /api/admin/config/sections/{sectionName}
    private static async Task<IResult> UpdateSection(
        string sectionName,
        [FromBody] UpdateConfigurationRequest request,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var command = new UpdateConfigurationCommand(sectionName, request.NewValueJson)
        {
            UserId = currentUser.UserId
        };

        var result = await bus.InvokeAsync<Result<ConfigurationBackupDto>>(command, cancellationToken);
        return result.ToHttpResult();
    }

    // GET /api/admin/config/backups
    private static async Task<IResult> GetBackups(
        [FromServices] IConfigurationManagementService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetBackupsAsync(cancellationToken);
        return result.ToHttpResult();
    }

    // POST /api/admin/config/backups/{backupId}/rollback
    private static async Task<IResult> RollbackBackup(
        string backupId,
        [FromServices] IConfigurationManagementService service,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await service.RollbackAsync(backupId, currentUser.UserId!, cancellationToken);
        return result.ToHttpResult();
    }

    // GET /api/admin/config/restart/status
    private static IResult GetRestartStatus(
        [FromServices] IApplicationRestartService restartService)
    {
        var env = restartService.DetectEnvironment();
        var canRestart = restartService.CanRestart();
        var isAllowed = restartService.IsRestartAllowed();
        var lastRestart = restartService.GetLastRestartTime();

        var status = new
        {
            Environment = env.ToString(),
            CanRestart = canRestart,
            IsAllowed = isAllowed,
            LastRestartTime = lastRestart,
            EnvironmentSupportsAutoRestart = canRestart
        };

        return Results.Ok(status);
    }

    // POST /api/admin/config/restart
    private static async Task<IResult> RestartApplication(
        [FromBody] RestartApplicationRequest request,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var command = new RestartApplicationCommand(request.Reason)
        {
            UserId = currentUser.UserId
        };

        var result = await bus.InvokeAsync<Result<RestartApplicationResult>>(command, cancellationToken);

        if (result.IsSuccess)
        {
            // Return 202 Accepted (request accepted, shutdown initiated)
            return Results.Accepted(null, result.Value);
        }

        return result.ToHttpResult();
    }
}

// Request DTOs
public sealed record UpdateConfigurationRequest(string NewValueJson);
public sealed record RestartApplicationRequest(string Reason);
