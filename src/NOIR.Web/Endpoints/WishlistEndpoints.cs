using NOIR.Application.Features.Wishlists.Commands.AddToWishlist;
using NOIR.Application.Features.Wishlists.Commands.CreateWishlist;
using NOIR.Application.Features.Wishlists.Commands.DeleteWishlist;
using NOIR.Application.Features.Wishlists.Commands.MoveToCart;
using NOIR.Application.Features.Wishlists.Commands.RemoveFromWishlist;
using NOIR.Application.Features.Wishlists.Commands.ShareWishlist;
using NOIR.Application.Features.Wishlists.Commands.UpdateWishlist;
using NOIR.Application.Features.Wishlists.Commands.UpdateWishlistItemPriority;
using NOIR.Application.Features.Wishlists.DTOs;
using NOIR.Application.Features.Wishlists.Queries.GetSharedWishlist;
using NOIR.Application.Features.Wishlists.Queries.GetWishlistAnalytics;
using NOIR.Application.Features.Wishlists.Queries.GetWishlistById;
using NOIR.Application.Features.Wishlists.Queries.GetWishlists;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Wishlist API endpoints.
/// Supports multiple wishlists per user, item management, and sharing.
/// </summary>
public static class WishlistEndpoints
{
    public static void MapWishlistEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/wishlists")
            .WithTags("Wishlists")
            .RequireFeature(ModuleNames.Ecommerce.Wishlist)
            .RequireAuthorization();

        // Get user's wishlists
        group.MapGet("/", async (
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var query = new GetWishlistsQuery { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<List<WishlistDto>>>(query);
            return result.ToHttpResult();
        })
        .WithName("GetWishlists")
        .WithSummary("Get user's wishlists")
        .WithDescription("Returns all wishlists for the authenticated user.")
        .Produces<List<WishlistDto>>(StatusCodes.Status200OK);

        // Get wishlist by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var query = new GetWishlistByIdQuery(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<WishlistDetailDto>>(query);
            return result.ToHttpResult();
        })
        .WithName("GetWishlistById")
        .WithSummary("Get wishlist by ID")
        .WithDescription("Returns a wishlist with full item details.")
        .Produces<WishlistDetailDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get shared wishlist (public access)
        group.MapGet("/shared/{token}", async (
            string token,
            IMessageBus bus) =>
        {
            var query = new GetSharedWishlistQuery(token);
            var result = await bus.InvokeAsync<Result<WishlistDetailDto>>(query);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetSharedWishlist")
        .WithSummary("Get shared wishlist")
        .WithDescription("Returns a public wishlist by share token.")
        .Produces<WishlistDetailDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get wishlist analytics (admin)
        group.MapGet("/analytics", async (
            [FromQuery] int topCount,
            IMessageBus bus) =>
        {
            var query = new GetWishlistAnalyticsQuery(topCount > 0 ? topCount : 10);
            var result = await bus.InvokeAsync<Result<WishlistAnalyticsDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.WishlistsManage)
        .WithName("GetWishlistAnalytics")
        .WithSummary("Get wishlist analytics")
        .WithDescription("Returns wishlist analytics including top wishlisted products.")
        .Produces<WishlistAnalyticsDto>(StatusCodes.Status200OK);

        // Create wishlist
        group.MapPost("/", async (
            CreateWishlistCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<WishlistDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .WithName("CreateWishlist")
        .WithSummary("Create a new wishlist")
        .WithDescription("Creates a new wishlist for the authenticated user.")
        .Produces<WishlistDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Update wishlist
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateWishlistCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { Id = id, UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<WishlistDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .WithName("UpdateWishlist")
        .WithSummary("Update a wishlist")
        .WithDescription("Updates the name and visibility of a wishlist.")
        .Produces<WishlistDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete wishlist
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteWishlistCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<WishlistDto>>(command);
            return result.ToHttpResult();
        })
        .WithName("DeleteWishlist")
        .WithSummary("Delete a wishlist")
        .WithDescription("Soft deletes a wishlist. Cannot delete the default wishlist.")
        .Produces<WishlistDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Add item to wishlist
        group.MapPost("/items", async (
            AddToWishlistRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AddToWishlistCommand(
                request.WishlistId,
                request.ProductId,
                request.ProductVariantId,
                request.Note)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<WishlistDetailDto>>(command);
            return result.ToHttpResult();
        })
        .WithName("AddToWishlist")
        .WithSummary("Add item to wishlist")
        .WithDescription("Adds a product to a wishlist. Uses the default wishlist if no WishlistId is specified.")
        .Produces<WishlistDetailDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Remove item from wishlist
        group.MapDelete("/items/{itemId:guid}", async (
            Guid itemId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RemoveFromWishlistCommand(itemId) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<WishlistDetailDto>>(command);
            return result.ToHttpResult();
        })
        .WithName("RemoveFromWishlist")
        .WithSummary("Remove item from wishlist")
        .WithDescription("Removes an item from the wishlist.")
        .Produces<WishlistDetailDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Move item to cart
        group.MapPost("/items/{itemId:guid}/move-to-cart", async (
            Guid itemId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new MoveToCartCommand(itemId) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<WishlistDetailDto>>(command);
            return result.ToHttpResult();
        })
        .WithName("MoveWishlistItemToCart")
        .WithSummary("Move item to cart")
        .WithDescription("Moves a wishlist item to the shopping cart and removes it from the wishlist.")
        .Produces<WishlistDetailDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Share wishlist
        group.MapPost("/{id:guid}/share", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ShareWishlistCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<WishlistDto>>(command);
            return result.ToHttpResult();
        })
        .WithName("ShareWishlist")
        .WithSummary("Share a wishlist")
        .WithDescription("Generates a share token for the wishlist, making it publicly accessible.")
        .Produces<WishlistDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Update item priority
        group.MapPut("/items/{itemId:guid}/priority", async (
            Guid itemId,
            UpdateWishlistItemPriorityRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateWishlistItemPriorityCommand(itemId, request.Priority)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<WishlistDetailDto>>(command);
            return result.ToHttpResult();
        })
        .WithName("UpdateWishlistItemPriority")
        .WithSummary("Update item priority")
        .WithDescription("Updates the priority of a wishlist item.")
        .Produces<WishlistDetailDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
