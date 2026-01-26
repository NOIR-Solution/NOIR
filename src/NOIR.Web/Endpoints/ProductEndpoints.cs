using NOIR.Application.Features.Products.Commands.ArchiveProduct;
using NOIR.Application.Features.Products.Commands.CreateProduct;
using NOIR.Application.Features.Products.Commands.PublishProduct;
using NOIR.Application.Features.Products.Commands.UpdateProduct;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Queries.GetProductById;
using NOIR.Application.Features.Products.Queries.GetProducts;
using ProductPagedResult = NOIR.Application.Features.Products.Queries.GetProducts.PagedResult<NOIR.Application.Features.Products.DTOs.ProductListDto>;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Product API endpoints.
/// Provides CRUD operations for products.
/// </summary>
public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products")
            .RequireAuthorization();

        // Get all products (paginated)
        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] ProductStatus? status,
            [FromQuery] Guid? categoryId,
            [FromQuery] string? brand,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] bool? inStockOnly,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMessageBus bus) =>
        {
            var query = new GetProductsQuery(
                search,
                status,
                categoryId,
                brand,
                minPrice,
                maxPrice,
                inStockOnly,
                page ?? 1,
                pageSize ?? 20);
            var result = await bus.InvokeAsync<Result<ProductPagedResult>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsRead)
        .WithName("GetProducts")
        .WithSummary("Get paginated list of products")
        .WithDescription("Returns products with optional filtering by search, status, category, brand, price range, and stock availability.")
        .Produces<ProductPagedResult>(StatusCodes.Status200OK);

        // Get product by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetProductByIdQuery(Id: id);
            var result = await bus.InvokeAsync<Result<ProductDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsRead)
        .WithName("GetProductById")
        .WithSummary("Get product by ID")
        .WithDescription("Returns full product details including variants and images.")
        .Produces<ProductDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get product by slug
        group.MapGet("/by-slug/{slug}", async (string slug, IMessageBus bus) =>
        {
            var query = new GetProductByIdQuery(Slug: slug);
            var result = await bus.InvokeAsync<Result<ProductDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsRead)
        .WithName("GetProductBySlug")
        .WithSummary("Get product by slug")
        .WithDescription("Returns product by its URL-friendly slug.")
        .Produces<ProductDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Create product
        group.MapPost("/", async (
            CreateProductRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            // Map request variants to command DTOs
            var variants = request.Variants?.Select(v => new CreateProductVariantDto(
                v.Name, v.Sku, v.Price, v.CompareAtPrice,
                v.StockQuantity, v.Options, v.SortOrder)).ToList();

            // Map request images to command DTOs
            var images = request.Images?.Select(i => new CreateProductImageDto(
                i.Url, i.AltText, i.SortOrder, i.IsPrimary)).ToList();

            var command = new CreateProductCommand(
                request.Name,
                request.Slug,
                request.Description,
                request.DescriptionHtml,
                request.BasePrice,
                request.Currency,
                request.CategoryId,
                request.Brand,
                request.Sku,
                request.Barcode,
                request.Weight,
                request.TrackInventory,
                request.MetaTitle,
                request.MetaDescription,
                request.SortOrder,
                variants,
                images)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsCreate)
        .WithName("CreateProduct")
        .WithSummary("Create a new product")
        .WithDescription("Creates a new product in draft status with optional variants and images.")
        .Produces<ProductDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update product
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateProductRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateProductCommand(
                id,
                request.Name,
                request.Slug,
                request.Description,
                request.DescriptionHtml,
                request.BasePrice,
                request.Currency,
                request.CategoryId,
                request.Brand,
                request.Sku,
                request.Barcode,
                request.Weight,
                request.TrackInventory,
                request.MetaTitle,
                request.MetaDescription,
                request.SortOrder)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("UpdateProduct")
        .WithSummary("Update an existing product")
        .WithDescription("Updates product details. Use separate endpoints for variants and images.")
        .Produces<ProductDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Publish product
        group.MapPost("/{id:guid}/publish", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new PublishProductCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ProductDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsPublish)
        .WithName("PublishProduct")
        .WithSummary("Publish a product")
        .WithDescription("Publishes a draft product, making it active and visible.")
        .Produces<ProductDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Archive product
        group.MapPost("/{id:guid}/archive", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ArchiveProductCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ProductDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("ArchiveProduct")
        .WithSummary("Archive a product")
        .WithDescription("Archives a product, removing it from active listings but preserving data.")
        .Produces<ProductDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
