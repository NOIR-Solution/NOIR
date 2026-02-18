using NOIR.Application.Features.Reviews.Commands.AddAdminResponse;
using NOIR.Application.Features.Reviews.Commands.ApproveReview;
using NOIR.Application.Features.Reviews.Commands.BulkApproveReviews;
using NOIR.Application.Features.Reviews.Commands.BulkRejectReviews;
using NOIR.Application.Features.Reviews.Commands.CreateReview;
using NOIR.Application.Features.Reviews.Commands.RejectReview;
using NOIR.Application.Features.Reviews.Commands.VoteReview;
using NOIR.Application.Features.Reviews.DTOs;
using NOIR.Application.Features.Reviews.Queries.GetProductReviews;
using NOIR.Application.Features.Reviews.Queries.GetReviewById;
using NOIR.Application.Features.Reviews.Queries.GetReviews;
using NOIR.Application.Features.Reviews.Queries.GetReviewStats;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Review API endpoints.
/// Provides CRUD operations for product reviews and ratings.
/// </summary>
public static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reviews")
            .WithTags("Reviews")
            .RequireAuthorization();

        var productReviewGroup = app.MapGroup("/api/products/{productId:guid}/reviews")
            .WithTags("Reviews")
            .RequireAuthorization();

        // GET /api/reviews - Admin moderation queue
        group.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] ReviewStatus? status,
            [FromQuery] Guid? productId,
            [FromQuery] int? rating,
            [FromQuery] string? search,
            IMessageBus bus) =>
        {
            var query = new GetReviewsQuery(
                page ?? 1,
                pageSize ?? 20,
                status,
                productId,
                rating,
                search);
            var result = await bus.InvokeAsync<Result<PagedResult<ReviewDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ReviewsRead)
        .WithName("GetReviews")
        .WithSummary("Get paginated list of reviews (admin)")
        .WithDescription("Returns reviews with optional filtering by status, product, rating, and search.")
        .Produces<PagedResult<ReviewDto>>(StatusCodes.Status200OK);

        // GET /api/reviews/{id} - Get review by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetReviewByIdQuery(id);
            var result = await bus.InvokeAsync<Result<ReviewDetailDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ReviewsRead)
        .WithName("GetReviewById")
        .WithSummary("Get review by ID")
        .WithDescription("Returns full review details including media.")
        .Produces<ReviewDetailDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // POST /api/reviews/{id}/approve - Approve review (admin)
        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ApproveReviewCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ReviewDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ReviewsManage)
        .WithName("ApproveReview")
        .WithSummary("Approve a review")
        .WithDescription("Approves a review for public display and recalculates product rating.")
        .Produces<ReviewDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // POST /api/reviews/{id}/reject - Reject review (admin)
        group.MapPost("/{id:guid}/reject", async (
            Guid id,
            [FromBody] RejectReviewRequest? request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RejectReviewCommand(id, request?.Reason) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ReviewDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ReviewsManage)
        .WithName("RejectReview")
        .WithSummary("Reject a review")
        .WithDescription("Rejects a review with an optional reason.")
        .Produces<ReviewDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // POST /api/reviews/{id}/respond - Add admin response (admin)
        group.MapPost("/{id:guid}/respond", async (
            Guid id,
            [FromBody] AdminResponseRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AddAdminResponseCommand(id, request.Response) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ReviewDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ReviewsManage)
        .WithName("AddAdminResponse")
        .WithSummary("Add admin response to a review")
        .WithDescription("Adds an admin response to a review that is visible to customers.")
        .Produces<ReviewDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // POST /api/reviews/bulk-approve - Bulk approve reviews (admin)
        group.MapPost("/bulk-approve", async (
            [FromBody] BulkApproveReviewsCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<int>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ReviewsManage)
        .WithName("BulkApproveReviews")
        .WithSummary("Bulk approve reviews")
        .WithDescription("Approves multiple reviews at once.")
        .Produces<int>(StatusCodes.Status200OK);

        // POST /api/reviews/bulk-reject - Bulk reject reviews (admin)
        group.MapPost("/bulk-reject", async (
            [FromBody] BulkRejectReviewsCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<int>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ReviewsManage)
        .WithName("BulkRejectReviews")
        .WithSummary("Bulk reject reviews")
        .WithDescription("Rejects multiple reviews at once.")
        .Produces<int>(StatusCodes.Status200OK);

        // POST /api/reviews/{id}/vote - Vote on review helpfulness
        group.MapPost("/{id:guid}/vote", async (
            Guid id,
            [FromBody] VoteReviewRequest request,
            IMessageBus bus) =>
        {
            var command = new VoteReviewCommand(id, request.IsHelpful);
            var result = await bus.InvokeAsync<Result>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithName("VoteReview")
        .WithSummary("Vote on review helpfulness")
        .WithDescription("Increments the helpful or not-helpful vote count for a review.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // GET /api/products/{productId}/reviews - Get product reviews (public)
        productReviewGroup.MapGet("/", async (
            Guid productId,
            [FromQuery] string? sort,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMessageBus bus) =>
        {
            var query = new GetProductReviewsQuery(productId, sort, page ?? 1, pageSize ?? 20);
            var result = await bus.InvokeAsync<Result<PagedResult<ReviewDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithName("GetProductReviews")
        .WithSummary("Get reviews for a product")
        .WithDescription("Returns approved reviews for a product with sorting options: newest, highest, lowest, mostHelpful.")
        .Produces<PagedResult<ReviewDto>>(StatusCodes.Status200OK);

        // GET /api/products/{productId}/reviews/stats - Get review statistics
        productReviewGroup.MapGet("/stats", async (
            Guid productId,
            IMessageBus bus) =>
        {
            var query = new GetReviewStatsQuery(productId);
            var result = await bus.InvokeAsync<Result<ReviewStatsDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithName("GetReviewStats")
        .WithSummary("Get review statistics for a product")
        .WithDescription("Returns average rating, total reviews, and rating distribution.")
        .Produces<ReviewStatsDto>(StatusCodes.Status200OK);

        // POST /api/products/{productId}/reviews - Create review
        productReviewGroup.MapPost("/", async (
            Guid productId,
            [FromBody] CreateReviewRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateReviewCommand(
                productId,
                request.Rating,
                request.Title,
                request.Content,
                request.OrderId,
                request.MediaUrls)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ReviewDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithName("CreateReview")
        .WithSummary("Create a product review")
        .WithDescription("Creates a new review for a product. Users can only review a product once.")
        .Produces<ReviewDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// Request DTO for creating a review.
/// </summary>
public sealed record CreateReviewRequest(
    int Rating,
    string? Title,
    string Content,
    Guid? OrderId = null,
    List<string>? MediaUrls = null);

/// <summary>
/// Request DTO for rejecting a review.
/// </summary>
public sealed record RejectReviewRequest(string? Reason);

/// <summary>
/// Request DTO for adding an admin response.
/// </summary>
public sealed record AdminResponseRequest(string Response);

/// <summary>
/// Request DTO for voting on a review.
/// </summary>
public sealed record VoteReviewRequest(bool IsHelpful);
