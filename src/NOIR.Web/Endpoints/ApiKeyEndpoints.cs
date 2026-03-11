using Microsoft.Extensions.Caching.Memory;
using NOIR.Application.Features.ApiKeys.Commands.CreateApiKey;
using NOIR.Application.Features.ApiKeys.Commands.UpdateApiKey;
using NOIR.Application.Features.ApiKeys.Commands.RotateApiKey;
using NOIR.Application.Features.ApiKeys.Commands.RevokeApiKey;
using NOIR.Application.Features.ApiKeys.DTOs;
using NOIR.Application.Features.ApiKeys.Queries.GetMyApiKeys;
using NOIR.Application.Features.ApiKeys.Queries.GetTenantApiKeys;
using NOIR.Web.Authentication;

namespace NOIR.Web.Endpoints;

/// <summary>
/// API Key management endpoints.
/// User self-service: /api/auth/me/api-keys (profile tab)
/// Admin management: /api/admin/api-keys (tenant admin view)
/// </summary>
public static class ApiKeyEndpoints
{
    public static void MapApiKeyEndpoints(this IEndpointRouteBuilder app)
    {
        // --- User Self-Service (Profile Tab) ---
        var userGroup = app.MapGroup("/api/auth/me/api-keys")
            .WithTags("API Keys")
            .RequireAuthorization();

        // List my API keys
        userGroup.MapGet("/", async (IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<List<ApiKeyDto>>>(new GetMyApiKeysQuery());
            return result.ToHttpResult();
        })
        .WithName("GetMyApiKeys")
        .WithSummary("List my API keys")
        .WithDescription("Returns all API keys owned by the current user.")
        .Produces<List<ApiKeyDto>>(StatusCodes.Status200OK);

        // Create API key
        userGroup.MapPost("/", async (
            CreateApiKeyCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ApiKeyCreatedDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ApiKeysCreate)
        .WithName("CreateApiKey")
        .WithSummary("Create a new API key")
        .WithDescription("Creates a new API key with scoped permissions. The API secret is returned only once — store it securely.")
        .Produces<ApiKeyCreatedDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Update API key (name, description, permissions)
        userGroup.MapPut("/{id:guid}", async (
            Guid id,
            UpdateApiKeyCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { Id = id, UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ApiKeyDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .WithName("UpdateApiKey")
        .WithSummary("Update an API key")
        .WithDescription("Updates the name, description, and permissions of an API key. Only the key owner can update.")
        .Produces<ApiKeyDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Rotate API key secret
        userGroup.MapPost("/{id:guid}/rotate", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            [FromServices] IMemoryCache cache,
            IMessageBus bus) =>
        {
            var command = new RotateApiKeyCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ApiKeyRotatedDto>>(command);
            if (result.IsSuccess)
            {
                ApiKeyAuthenticationHandler.EvictCachedKey(cache, result.Value!.KeyIdentifier);
            }
            return result.ToHttpResult();
        })
        .WithName("RotateApiKeySecret")
        .WithSummary("Rotate an API key's secret")
        .WithDescription("Generates a new secret for the API key. The old secret is invalidated immediately. New secret is returned only once.")
        .Produces<ApiKeyRotatedDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Revoke API key (user self-service)
        userGroup.MapPost("/{id:guid}/revoke", async (
            Guid id,
            RevokeApiKeyCommand? command,
            [FromServices] ICurrentUser currentUser,
            [FromServices] IMemoryCache cache,
            IMessageBus bus) =>
        {
            var auditableCommand = new RevokeApiKeyCommand(id, command?.Reason) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ApiKeyDto>>(auditableCommand);
            if (result.IsSuccess)
            {
                ApiKeyAuthenticationHandler.EvictCachedKey(cache, result.Value!.KeyIdentifier);
            }
            return result.ToHttpResult();
        })
        .WithName("RevokeApiKey")
        .WithSummary("Revoke an API key")
        .WithDescription("Permanently revokes an API key. The key immediately stops working.")
        .Produces<ApiKeyDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // --- Admin Management ---
        var adminGroup = app.MapGroup("/api/admin/api-keys")
            .WithTags("API Keys")
            .RequireAuthorization(Permissions.ApiKeysRead);

        // List all API keys in tenant (admin view)
        adminGroup.MapGet("/", async (IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<List<ApiKeyDto>>>(new GetTenantApiKeysQuery());
            return result.ToHttpResult();
        })
        .WithName("GetTenantApiKeys")
        .WithSummary("List all API keys in tenant")
        .WithDescription("Returns all API keys across all users in the current tenant. Requires api-keys:read permission.")
        .Produces<List<ApiKeyDto>>(StatusCodes.Status200OK);

        // Admin revoke any API key in tenant
        adminGroup.MapPost("/{id:guid}/revoke", async (
            Guid id,
            RevokeApiKeyCommand? command,
            [FromServices] ICurrentUser currentUser,
            [FromServices] IMemoryCache cache,
            IMessageBus bus) =>
        {
            var auditableCommand = new RevokeApiKeyCommand(id, command?.Reason) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ApiKeyDto>>(auditableCommand);
            if (result.IsSuccess)
            {
                ApiKeyAuthenticationHandler.EvictCachedKey(cache, result.Value!.KeyIdentifier);
            }
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ApiKeysDelete)
        .WithName("AdminRevokeApiKey")
        .WithSummary("Admin: Revoke any API key")
        .WithDescription("Allows tenant admins to revoke any API key in the tenant. Requires api-keys:delete permission.")
        .Produces<ApiKeyDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }
}
