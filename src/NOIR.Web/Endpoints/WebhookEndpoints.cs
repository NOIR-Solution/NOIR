using NOIR.Application.Features.Webhooks.Commands.CreateWebhookSubscription;
using NOIR.Application.Features.Webhooks.Commands.UpdateWebhookSubscription;
using NOIR.Application.Features.Webhooks.Commands.DeleteWebhookSubscription;
using NOIR.Application.Features.Webhooks.Commands.ActivateWebhookSubscription;
using NOIR.Application.Features.Webhooks.Commands.DeactivateWebhookSubscription;
using NOIR.Application.Features.Webhooks.Commands.TestWebhookSubscription;
using NOIR.Application.Features.Webhooks.Commands.RotateWebhookSecret;
using NOIR.Application.Features.Webhooks.DTOs;
using NOIR.Application.Features.Webhooks.Queries.GetWebhookSubscriptions;
using NOIR.Application.Features.Webhooks.Queries.GetWebhookSubscriptionById;
using NOIR.Application.Features.Webhooks.Queries.GetWebhookDeliveryLogs;
using NOIR.Application.Features.Webhooks.Queries.GetWebhookEventTypes;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Webhook API endpoints.
/// Provides CRUD operations, lifecycle management, testing, and delivery log access for webhook subscriptions.
/// </summary>
public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/webhooks")
            .WithTags("Webhooks")
            .RequireFeature(ModuleNames.Integrations.Webhooks)
            .RequireAuthorization();

        // Create webhook subscription
        group.MapPost("/", async (
            CreateWebhookSubscriptionCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<WebhookSubscriptionDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.WebhooksManage)
        .WithName("CreateWebhookSubscription")
        .WithSummary("Create a new webhook subscription")
        .WithDescription("Creates a new webhook subscription with URL, event patterns, and delivery configuration.")
        .Produces<WebhookSubscriptionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Get all webhook subscriptions (paginated)
        group.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            [FromQuery] WebhookSubscriptionStatus? status,
            IMessageBus bus) =>
        {
            var query = new GetWebhookSubscriptionsQuery(
                page ?? 1,
                pageSize ?? 20,
                search,
                status);
            var result = await bus.InvokeAsync<Result<PagedResult<WebhookSubscriptionSummaryDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.WebhooksRead)
        .WithName("GetWebhookSubscriptions")
        .WithSummary("Get paginated list of webhook subscriptions")
        .WithDescription("Returns webhook subscriptions with optional filtering by search term and status.")
        .Produces<PagedResult<WebhookSubscriptionSummaryDto>>(StatusCodes.Status200OK);

        // Get webhook subscription by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetWebhookSubscriptionByIdQuery(id);
            var result = await bus.InvokeAsync<Result<WebhookSubscriptionDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.WebhooksRead)
        .WithName("GetWebhookSubscriptionById")
        .WithSummary("Get webhook subscription by ID")
        .WithDescription("Returns full webhook subscription details including configuration and delivery statistics.")
        .Produces<WebhookSubscriptionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Update webhook subscription
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateWebhookSubscriptionCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { Id = id, UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<WebhookSubscriptionDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.WebhooksManage)
        .WithName("UpdateWebhookSubscription")
        .WithSummary("Update a webhook subscription")
        .WithDescription("Updates webhook subscription details, URL, event patterns, and delivery configuration.")
        .Produces<WebhookSubscriptionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete webhook subscription (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteWebhookSubscriptionCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.WebhooksManage)
        .WithName("DeleteWebhookSubscription")
        .WithSummary("Delete a webhook subscription")
        .WithDescription("Soft deletes a webhook subscription.")
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Activate webhook subscription
        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ActivateWebhookSubscriptionCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<WebhookSubscriptionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.WebhooksManage)
        .WithName("ActivateWebhookSubscription")
        .WithSummary("Activate a webhook subscription")
        .WithDescription("Activates a webhook subscription, enabling delivery of events to the configured URL.")
        .Produces<WebhookSubscriptionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Deactivate webhook subscription
        group.MapPost("/{id:guid}/deactivate", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeactivateWebhookSubscriptionCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<WebhookSubscriptionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.WebhooksManage)
        .WithName("DeactivateWebhookSubscription")
        .WithSummary("Deactivate a webhook subscription")
        .WithDescription("Deactivates a webhook subscription, pausing delivery of events.")
        .Produces<WebhookSubscriptionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Test webhook subscription
        group.MapPost("/{id:guid}/test", async (Guid id, IMessageBus bus) =>
        {
            var command = new TestWebhookSubscriptionCommand(id);
            var result = await bus.InvokeAsync<Result<WebhookDeliveryLogDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.WebhooksTest)
        .WithName("TestWebhookSubscription")
        .WithSummary("Send a test event to a webhook subscription")
        .WithDescription("Sends a test ping event to the webhook URL and returns the delivery result.")
        .Produces<WebhookDeliveryLogDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Rotate webhook secret
        group.MapPost("/{id:guid}/rotate-secret", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RotateWebhookSecretCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<WebhookSecretDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.WebhooksManage)
        .WithName("RotateWebhookSecret")
        .WithSummary("Rotate the signing secret for a webhook subscription")
        .WithDescription("Generates a new signing secret for the webhook subscription. The previous secret is immediately invalidated.")
        .Produces<WebhookSecretDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get webhook delivery logs
        group.MapGet("/{id:guid}/deliveries", async (
            Guid id,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] WebhookDeliveryStatus? status,
            IMessageBus bus) =>
        {
            var query = new GetWebhookDeliveryLogsQuery(
                id,
                page ?? 1,
                pageSize ?? 20,
                status);
            var result = await bus.InvokeAsync<Result<PagedResult<WebhookDeliveryLogDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.WebhooksRead)
        .WithName("GetWebhookDeliveryLogs")
        .WithSummary("Get delivery logs for a webhook subscription")
        .WithDescription("Returns paginated delivery logs with optional filtering by delivery status.")
        .Produces<PagedResult<WebhookDeliveryLogDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get available webhook event types
        group.MapGet("/event-types", async (IMessageBus bus) =>
        {
            var query = new GetWebhookEventTypesQuery();
            var result = await bus.InvokeAsync<Result<List<WebhookEventTypeDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.WebhooksRead)
        .WithName("GetWebhookEventTypes")
        .WithSummary("Get available webhook event types")
        .WithDescription("Returns all available event types that can be subscribed to for webhook delivery.")
        .Produces<List<WebhookEventTypeDto>>(StatusCodes.Status200OK);
    }
}
