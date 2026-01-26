using NOIR.Application.Features.Products.Commands.CreateProductCategory;
using NOIR.Application.Features.Products.Commands.DeleteProductCategory;
using NOIR.Application.Features.Products.Commands.UpdateProductCategory;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Queries.GetProductCategories;
using NOIR.Application.Features.Products.Queries.GetProductCategoryById;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Product Category API endpoints.
/// Provides CRUD operations for product categories.
/// </summary>
public static class ProductCategoryEndpoints
{
    public static void MapProductCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/categories")
            .WithTags("Product Categories")
            .RequireAuthorization();

        // Get all categories
        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] bool? topLevelOnly,
            [FromQuery] bool? includeChildren,
            IMessageBus bus) =>
        {
            var query = new GetProductCategoriesQuery(search, topLevelOnly ?? false, includeChildren ?? false);
            var result = await bus.InvokeAsync<Result<List<ProductCategoryListDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductCategoriesRead)
        .WithName("GetProductCategories")
        .WithSummary("Get list of product categories")
        .WithDescription("Returns all product categories with optional filtering.")
        .Produces<List<ProductCategoryListDto>>(StatusCodes.Status200OK);

        // Get category by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetProductCategoryByIdQuery(id);
            var result = await bus.InvokeAsync<Result<ProductCategoryDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductCategoriesRead)
        .WithName("GetProductCategoryById")
        .WithSummary("Get product category by ID")
        .WithDescription("Returns product category details including children.")
        .Produces<ProductCategoryDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Create category
        group.MapPost("/", async (
            CreateProductCategoryRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateProductCategoryCommand(
                request.Name,
                request.Slug,
                request.Description,
                request.MetaTitle,
                request.MetaDescription,
                request.ImageUrl,
                request.SortOrder,
                request.ParentId)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductCategoryDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductCategoriesCreate)
        .WithName("CreateProductCategory")
        .WithSummary("Create a new product category")
        .WithDescription("Creates a new product category. Can be nested under a parent category.")
        .Produces<ProductCategoryDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update category
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateProductCategoryRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateProductCategoryCommand(
                id,
                request.Name,
                request.Slug,
                request.Description,
                request.MetaTitle,
                request.MetaDescription,
                request.ImageUrl,
                request.SortOrder,
                request.ParentId)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductCategoryDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductCategoriesUpdate)
        .WithName("UpdateProductCategory")
        .WithSummary("Update an existing product category")
        .WithDescription("Updates category details and parent relationship.")
        .Produces<ProductCategoryDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Delete category (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteProductCategoryCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductCategoriesDelete)
        .WithName("DeleteProductCategory")
        .WithSummary("Soft-delete a product category")
        .WithDescription("Soft-deletes a category. Will fail if it has child categories or products.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
    }
}
