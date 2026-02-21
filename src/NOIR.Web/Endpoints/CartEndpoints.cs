using NOIR.Application.Features.Cart.Commands.AddToCart;
using NOIR.Application.Features.Cart.Commands.ClearCart;
using NOIR.Application.Features.Cart.Commands.MergeCart;
using NOIR.Application.Features.Cart.Commands.RemoveCartItem;
using NOIR.Application.Features.Cart.Commands.UpdateCartItem;
using NOIR.Application.Features.Cart.DTOs;
using NOIR.Application.Features.Cart.Queries.GetCart;
using NOIR.Application.Features.Cart.Queries.GetCartSummary;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Shopping Cart API endpoints.
/// Supports both authenticated users and guest sessions.
/// </summary>
public static class CartEndpoints
{
    private const string CartSessionIdCookieName = "noir_cart_session";

    public static void MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cart")
            .WithTags("Cart")
            .RequireFeature(ModuleNames.Ecommerce.Cart);

        // Get current cart
        group.MapGet("/", async (
            HttpContext httpContext,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var query = new GetCartQuery
            {
                UserId = currentUser.IsAuthenticated ? currentUser.UserId : null,
                SessionId = GetOrCreateSessionId(httpContext)
            };
            var result = await bus.InvokeAsync<Result<CartDto>>(query);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetCart")
        .WithSummary("Get current cart")
        .WithDescription("Returns the current user's or guest's shopping cart.")
        .Produces<CartDto>(StatusCodes.Status200OK);

        // Get cart summary (for mini-cart)
        group.MapGet("/summary", async (
            HttpContext httpContext,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var query = new GetCartSummaryQuery
            {
                UserId = currentUser.IsAuthenticated ? currentUser.UserId : null,
                SessionId = GetOrCreateSessionId(httpContext)
            };
            var result = await bus.InvokeAsync<Result<CartSummaryDto>>(query);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetCartSummary")
        .WithSummary("Get cart summary")
        .WithDescription("Returns cart summary for mini-cart display.")
        .Produces<CartSummaryDto>(StatusCodes.Status200OK);

        // Add item to cart
        group.MapPost("/items", async (
            AddToCartRequest request,
            HttpContext httpContext,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AddToCartCommand(
                request.ProductId,
                request.ProductVariantId,
                request.Quantity)
            {
                UserId = currentUser.IsAuthenticated ? currentUser.UserId : null,
                SessionId = GetOrCreateSessionId(httpContext)
            };
            var result = await bus.InvokeAsync<Result<CartDto>>(command);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("AddToCart")
        .WithSummary("Add item to cart")
        .WithDescription("Adds a product variant to the shopping cart. Creates a cart if none exists.")
        .Produces<CartDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Update cart item quantity
        group.MapPut("/{cartId:guid}/items/{itemId:guid}", async (
            Guid cartId,
            Guid itemId,
            UpdateCartItemRequest request,
            HttpContext httpContext,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateCartItemCommand(cartId, itemId, request.Quantity)
            {
                UserId = currentUser.IsAuthenticated ? currentUser.UserId : null,
                SessionId = GetOrCreateSessionId(httpContext)
            };
            var result = await bus.InvokeAsync<Result<CartDto>>(command);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("UpdateCartItem")
        .WithSummary("Update cart item quantity")
        .WithDescription("Updates the quantity of a cart item. Set quantity to 0 to remove.")
        .Produces<CartDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Remove item from cart
        group.MapDelete("/{cartId:guid}/items/{itemId:guid}", async (
            Guid cartId,
            Guid itemId,
            HttpContext httpContext,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RemoveCartItemCommand(cartId, itemId)
            {
                UserId = currentUser.IsAuthenticated ? currentUser.UserId : null,
                SessionId = GetOrCreateSessionId(httpContext)
            };
            var result = await bus.InvokeAsync<Result<CartDto>>(command);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("RemoveCartItem")
        .WithSummary("Remove item from cart")
        .WithDescription("Removes an item from the shopping cart.")
        .Produces<CartDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Clear cart
        group.MapDelete("/{cartId:guid}", async (
            Guid cartId,
            HttpContext httpContext,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ClearCartCommand(cartId)
            {
                UserId = currentUser.IsAuthenticated ? currentUser.UserId : null,
                SessionId = GetOrCreateSessionId(httpContext)
            };
            var result = await bus.InvokeAsync<Result<CartDto>>(command);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("ClearCart")
        .WithSummary("Clear cart")
        .WithDescription("Removes all items from the shopping cart.")
        .Produces<CartDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Merge guest cart on login (internal use)
        group.MapPost("/merge", async (
            HttpContext httpContext,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            if (!currentUser.IsAuthenticated)
            {
                return Results.Unauthorized();
            }

            var sessionId = httpContext.Request.Cookies[CartSessionIdCookieName];
            if (string.IsNullOrEmpty(sessionId))
            {
                return Results.Ok(new CartMergeResultDto
                {
                    TargetCartId = Guid.Empty,
                    MergedItemCount = 0,
                    TotalItemCount = 0,
                    NewSubtotal = 0
                });
            }

            var command = new MergeCartCommand(sessionId, currentUser.UserId!);
            var result = await bus.InvokeAsync<Result<CartMergeResultDto>>(command);

            // Clear the guest session cookie after merge
            if (result.IsSuccess)
            {
                httpContext.Response.Cookies.Delete(CartSessionIdCookieName);
            }

            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithName("MergeCart")
        .WithSummary("Merge guest cart on login")
        .WithDescription("Merges the guest cart into the authenticated user's cart.")
        .Produces<CartMergeResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Gets the cart session ID from cookie or creates a new one.
    /// </summary>
    private static string GetOrCreateSessionId(HttpContext httpContext)
    {
        var sessionId = httpContext.Request.Cookies[CartSessionIdCookieName];

        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString("N");
            httpContext.Response.Cookies.Append(CartSessionIdCookieName, sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                Path = "/"
            });
        }

        return sessionId;
    }
}
