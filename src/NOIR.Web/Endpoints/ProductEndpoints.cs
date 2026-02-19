using NOIR.Application.Features.ProductAttributes.Commands.BulkUpdateProductAttributes;
using NOIR.Application.Features.ProductAttributes.Commands.SetProductAttributeValue;
using NOIR.Application.Features.ProductAttributes.DTOs;
using NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributeAssignments;
using NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributeFormSchema;
using NOIR.Application.Features.Products.Commands.AddProductImage;
using NOIR.Application.Features.Products.Specifications;
using NOIR.Domain.Entities.Product;
using NOIR.Application.Features.Products.Commands.AddProductOption;
using NOIR.Application.Features.Products.Commands.AddProductOptionValue;
using NOIR.Application.Features.Products.Commands.AddProductVariant;
using NOIR.Application.Features.Products.Commands.ArchiveProduct;
using NOIR.Application.Features.Products.Commands.BulkArchiveProducts;
using NOIR.Application.Features.Products.Commands.BulkDeleteProducts;
using NOIR.Application.Features.Products.Commands.BulkImportProducts;
using NOIR.Application.Features.Products.Queries.ExportProducts;
using NOIR.Application.Features.Products.Commands.BulkPublishProducts;
using NOIR.Application.Features.Products.Commands.CreateProduct;
using NOIR.Application.Features.Products.Commands.DeleteProduct;
using NOIR.Application.Features.Products.Commands.DuplicateProduct;
using NOIR.Application.Features.Products.Commands.DeleteProductImage;
using NOIR.Application.Features.Products.Commands.DeleteProductOption;
using NOIR.Application.Features.Products.Commands.DeleteProductOptionValue;
using NOIR.Application.Features.Products.Commands.DeleteProductVariant;
using NOIR.Application.Features.Products.Commands.PublishProduct;
using NOIR.Application.Features.Products.Commands.ReorderProductImages;
using NOIR.Application.Features.Products.Commands.SetPrimaryProductImage;
using NOIR.Application.Features.Products.Commands.UpdateProduct;
using NOIR.Application.Features.Products.Commands.UpdateProductImage;
using NOIR.Application.Features.Products.Commands.UpdateProductOption;
using NOIR.Application.Features.Products.Commands.UpdateProductOptionValue;
using NOIR.Application.Features.Products.Commands.UpdateProductVariant;
using NOIR.Application.Features.Products.Commands.UploadProductImage;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Queries.GetProductById;
using NOIR.Application.Features.Products.Queries.GetProducts;
using NOIR.Application.Features.Products.Queries.GetProductStats;
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
            [FromQuery] bool? lowStockOnly,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? attributeFilters,
            IMessageBus bus) =>
        {
            // Parse attribute filters from JSON string
            // Format: {"attributeCode": ["value1", "value2"], ...}
            Dictionary<string, List<string>>? parsedFilters = null;
            if (!string.IsNullOrEmpty(attributeFilters))
            {
                try
                {
                    parsedFilters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(attributeFilters);
                }
                catch
                {
                    // Ignore invalid JSON, treat as no filter
                }
            }

            var query = new GetProductsQuery(
                search,
                status,
                categoryId,
                brand,
                minPrice,
                maxPrice,
                inStockOnly,
                lowStockOnly,
                page ?? 1,
                pageSize ?? 20,
                parsedFilters);
            var result = await bus.InvokeAsync<Result<ProductPagedResult>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsRead)
        .WithName("GetProducts")
        .WithSummary("Get paginated list of products")
        .WithDescription("Returns products with optional filtering by search, status, category, brand, price range, stock availability, low stock items, and attribute values. The attributeFilters parameter accepts a JSON string like {\"color\": [\"Red\", \"Blue\"]}.")
        .Produces<ProductPagedResult>(StatusCodes.Status200OK);

        // Get product stats (dashboard)
        group.MapGet("/stats", async (IMessageBus bus) =>
        {
            var query = new GetProductStatsQuery();
            var result = await bus.InvokeAsync<Result<ProductStatsDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsRead)
        .WithName("GetProductStats")
        .WithSummary("Get product statistics")
        .WithDescription("Returns global product counts by status for dashboard display.")
        .Produces<ProductStatsDto>(StatusCodes.Status200OK);

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
                v.Name, v.Sku, v.Price, v.CompareAtPrice, v.CostPrice,
                v.StockQuantity, v.Options, v.SortOrder)).ToList();

            // Map request images to command DTOs
            var images = request.Images?.Select(i => new CreateProductImageDto(
                i.Url, i.AltText, i.SortOrder, i.IsPrimary)).ToList();

            var command = new CreateProductCommand(
                request.Name,
                request.Slug,
                request.ShortDescription,
                request.Description,
                request.DescriptionHtml,
                request.BasePrice,
                request.Currency,
                request.CategoryId,
                request.BrandId,
                request.Brand,
                request.Sku,
                request.Barcode,
                request.TrackInventory,
                request.MetaTitle,
                request.MetaDescription,
                request.SortOrder,
                request.Weight,
                request.WeightUnit,
                request.Length,
                request.Width,
                request.Height,
                request.DimensionUnit,
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
                request.ShortDescription,
                request.Description,
                request.DescriptionHtml,
                request.BasePrice,
                request.Currency,
                request.CategoryId,
                request.BrandId,
                request.Brand,
                request.Sku,
                request.Barcode,
                request.TrackInventory,
                request.MetaTitle,
                request.MetaDescription,
                request.SortOrder,
                request.Weight,
                request.WeightUnit,
                request.Length,
                request.Width,
                request.Height,
                request.DimensionUnit)
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

        // Duplicate product
        group.MapPost("/{id:guid}/duplicate", async (
            Guid id,
            DuplicateProductRequest? request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DuplicateProductCommand(
                id,
                request?.CopyVariants ?? false,
                request?.CopyImages ?? false,
                request?.CopyOptions ?? false)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsCreate)
        .WithName("DuplicateProduct")
        .WithSummary("Duplicate a product")
        .WithDescription("Creates a copy of a product as a new draft. Optionally copies variants, images, and options.")
        .Produces<ProductDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Bulk import products
        group.MapPost("/import", async (
            BulkImportProductsCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var commandWithUser = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<BulkImportResultDto>>(commandWithUser);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsCreate)
        .WithName("BulkImportProducts")
        .WithSummary("Bulk import products")
        .WithDescription("Import multiple products from parsed CSV data. Supports variants, images, and attributes. Maximum 1000 products per request.")
        .Produces<BulkImportResultDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Export products
        group.MapGet("/export", async (
            [FromQuery] string? categoryId,
            [FromQuery] string? status,
            [FromQuery] bool includeAttributes,
            [FromQuery] bool includeImages,
            IMessageBus bus) =>
        {
            var query = new ExportProductsQuery(categoryId, status, includeAttributes, includeImages);
            var result = await bus.InvokeAsync<Result<ExportProductsResultDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsRead)
        .WithName("ExportProducts")
        .WithSummary("Export products")
        .WithDescription("Export products as flat rows for CSV. Each variant becomes a separate row. Includes images as pipe-separated URLs and dynamic attribute columns.")
        .Produces<ExportProductsResultDto>(StatusCodes.Status200OK);

        // Bulk publish products
        group.MapPost("/bulk-publish", async (
            BulkPublishProductsCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var commandWithUser = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<BulkOperationResultDto>>(commandWithUser);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsPublish)
        .WithName("BulkPublishProducts")
        .WithSummary("Bulk publish products")
        .WithDescription("Publishes multiple draft products in a single operation.")
        .Produces<BulkOperationResultDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Bulk archive products
        group.MapPost("/bulk-archive", async (
            BulkArchiveProductsCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var commandWithUser = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<BulkOperationResultDto>>(commandWithUser);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("BulkArchiveProducts")
        .WithSummary("Bulk archive products")
        .WithDescription("Archives multiple active products in a single operation.")
        .Produces<BulkOperationResultDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Bulk delete products
        group.MapPost("/bulk-delete", async (
            BulkDeleteProductsCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var commandWithUser = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<BulkOperationResultDto>>(commandWithUser);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsDelete)
        .WithName("BulkDeleteProducts")
        .WithSummary("Bulk delete products")
        .WithDescription("Soft-deletes multiple products in a single operation.")
        .Produces<BulkOperationResultDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Delete product (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteProductCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsDelete)
        .WithName("DeleteProduct")
        .WithSummary("Soft-delete a product")
        .WithDescription("Soft-deletes a product. It can be restored later.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ===== Variant Management Endpoints =====

        // Add variant
        group.MapPost("/{productId:guid}/variants", async (
            Guid productId,
            AddProductVariantRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AddProductVariantCommand(
                productId,
                request.Name,
                request.Price,
                request.Sku,
                request.CompareAtPrice,
                request.CostPrice,
                request.StockQuantity,
                request.Options,
                request.SortOrder)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductVariantDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("AddProductVariant")
        .WithSummary("Add a variant to a product")
        .WithDescription("Adds a new variant with pricing and stock information.")
        .Produces<ProductVariantDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Update variant
        group.MapPut("/{productId:guid}/variants/{variantId:guid}", async (
            Guid productId,
            Guid variantId,
            UpdateProductVariantRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateProductVariantCommand(
                productId,
                variantId,
                request.Name,
                request.Price,
                request.Sku,
                request.CompareAtPrice,
                request.CostPrice,
                request.StockQuantity,
                request.Options,
                request.SortOrder)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("UpdateProductVariant")
        .WithSummary("Update a product variant")
        .WithDescription("Updates variant details including pricing, stock, and options. Returns full product with updated variant.")
        .Produces<ProductDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete variant
        group.MapDelete("/{productId:guid}/variants/{variantId:guid}", async (
            Guid productId,
            Guid variantId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteProductVariantCommand(productId, variantId)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("DeleteProductVariant")
        .WithSummary("Delete a product variant")
        .WithDescription("Removes a variant from the product.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get stock history for variant
        group.MapGet("/{productId:guid}/variants/{variantId:guid}/stock-history", async (
            Guid productId,
            Guid variantId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMessageBus bus) =>
        {
            var query = new Application.Features.Inventory.Queries.GetStockHistory.GetStockHistoryQuery(
                productId,
                variantId,
                page ?? 1,
                pageSize ?? 20);
            var result = await bus.InvokeAsync<Result<Application.Features.Products.Queries.GetProducts.PagedResult<Application.Features.Inventory.DTOs.InventoryMovementDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsRead)
        .WithName("GetProductVariantStockHistory")
        .WithSummary("Get stock movement history for a product variant")
        .WithDescription("Returns paginated stock movement history including reservations, releases, adjustments, and other inventory changes.")
        .Produces<Application.Features.Products.Queries.GetProducts.PagedResult<Application.Features.Inventory.DTOs.InventoryMovementDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ===== Image Management Endpoints =====

        // Add image (by URL)
        group.MapPost("/{productId:guid}/images", async (
            Guid productId,
            AddProductImageRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AddProductImageCommand(
                productId,
                request.Url,
                request.AltText,
                request.SortOrder,
                request.IsPrimary)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductImageDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("AddProductImage")
        .WithSummary("Add an image to a product")
        .WithDescription("Adds a new image to the product gallery.")
        .Produces<ProductImageDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Upload image (file upload with processing)
        group.MapPost("/{productId:guid}/images/upload", async (
            Guid productId,
            IFormFile file,
            [FromQuery] string? altText,
            [FromQuery] bool isPrimary,
            [FromServices] ICurrentUser currentUser,
            [FromServices] UploadProductImageCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            await using var stream = file.OpenReadStream();
            var command = new UploadProductImageCommand(
                productId,
                file.FileName,
                stream,
                file.ContentType,
                file.Length,
                altText,
                isPrimary)
            {
                UserId = currentUser.UserId
            };
            return (await handler.Handle(command, cancellationToken)).ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("UploadProductImage")
        .WithSummary("Upload an image to a product")
        .WithDescription("Uploads and processes an image (resize, optimize). Max 10MB. Supports JPEG, PNG, GIF, WebP, AVIF.")
        .RequireRateLimiting("fixed")
        .DisableAntiforgery()
        .Produces<ProductImageUploadResultDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Reorder images (bulk update sortOrder)
        group.MapPut("/{productId:guid}/images/reorder", async (
            Guid productId,
            ReorderProductImagesRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ReorderProductImagesCommand(
                productId,
                request.Items.Select(i => new ImageSortOrderItem(i.ImageId, i.SortOrder)).ToList())
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("ReorderProductImages")
        .WithSummary("Reorder product images")
        .WithDescription("Updates the sort order of multiple images in a single request. Returns the updated product.")
        .Produces<ProductDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Update image
        group.MapPut("/{productId:guid}/images/{imageId:guid}", async (
            Guid productId,
            Guid imageId,
            UpdateProductImageRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateProductImageCommand(
                productId,
                imageId,
                request.Url,
                request.AltText,
                request.SortOrder)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("UpdateProductImage")
        .WithSummary("Update a product image")
        .WithDescription("Updates image URL, alt text, and sort order. Returns full product with updated image.")
        .Produces<ProductDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete image
        group.MapDelete("/{productId:guid}/images/{imageId:guid}", async (
            Guid productId,
            Guid imageId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteProductImageCommand(productId, imageId)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("DeleteProductImage")
        .WithSummary("Delete a product image")
        .WithDescription("Removes an image from the product gallery.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Set primary image
        group.MapPost("/{productId:guid}/images/{imageId:guid}/set-primary", async (
            Guid productId,
            Guid imageId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new SetPrimaryProductImageCommand(productId, imageId)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("SetPrimaryProductImage")
        .WithSummary("Set an image as primary")
        .WithDescription("Sets the specified image as the primary product image.")
        .Produces<ProductDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ===== Option Management Endpoints =====

        // Add option
        group.MapPost("/{productId:guid}/options", async (
            Guid productId,
            AddProductOptionRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AddProductOptionCommand(
                productId,
                request.Name,
                request.DisplayName,
                request.SortOrder,
                request.Values)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductOptionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("AddProductOption")
        .WithSummary("Add an option to a product")
        .WithDescription("Adds a new option type (e.g., Color, Size) with optional values.")
        .Produces<ProductOptionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Update option
        group.MapPut("/{productId:guid}/options/{optionId:guid}", async (
            Guid productId,
            Guid optionId,
            UpdateProductOptionRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateProductOptionCommand(
                productId,
                optionId,
                request.Name,
                request.DisplayName,
                request.SortOrder)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductOptionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("UpdateProductOption")
        .WithSummary("Update a product option")
        .WithDescription("Updates option name, display name, or sort order.")
        .Produces<ProductOptionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete option
        group.MapDelete("/{productId:guid}/options/{optionId:guid}", async (
            Guid productId,
            Guid optionId,
            [FromServices] ICurrentUser currentUser,
            [FromServices] IRepository<Product, Guid> productRepository,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            // Fetch option name for audit log display
            var spec = new ProductByIdForOptionUpdateSpec(productId);
            var product = await productRepository.FirstOrDefaultAsync(spec, ct);
            var optionName = product?.Options.FirstOrDefault(o => o.Id == optionId)?.DisplayName;

            var command = new DeleteProductOptionCommand(productId, optionId, optionName)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("DeleteProductOption")
        .WithSummary("Delete a product option")
        .WithDescription("Removes an option and all its values from the product.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ===== Option Value Management Endpoints =====

        // Add option value
        group.MapPost("/{productId:guid}/options/{optionId:guid}/values", async (
            Guid productId,
            Guid optionId,
            AddProductOptionValueRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AddProductOptionValueCommand(
                productId,
                optionId,
                request.Value,
                request.DisplayValue,
                request.ColorCode,
                request.SwatchUrl,
                request.SortOrder)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductOptionValueDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("AddProductOptionValue")
        .WithSummary("Add a value to a product option")
        .WithDescription("Adds a new value (e.g., Red, Large) to an existing option.")
        .Produces<ProductOptionValueDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Update option value
        group.MapPut("/{productId:guid}/options/{optionId:guid}/values/{valueId:guid}", async (
            Guid productId,
            Guid optionId,
            Guid valueId,
            UpdateProductOptionValueRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateProductOptionValueCommand(
                productId,
                optionId,
                valueId,
                request.Value,
                request.DisplayValue,
                request.ColorCode,
                request.SwatchUrl,
                request.SortOrder)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductOptionValueDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("UpdateProductOptionValue")
        .WithSummary("Update a product option value")
        .WithDescription("Updates value details including color code and swatch URL.")
        .Produces<ProductOptionValueDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete option value
        group.MapDelete("/{productId:guid}/options/{optionId:guid}/values/{valueId:guid}", async (
            Guid productId,
            Guid optionId,
            Guid valueId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteProductOptionValueCommand(productId, optionId, valueId)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("DeleteProductOptionValue")
        .WithSummary("Delete a product option value")
        .WithDescription("Removes a value from the product option.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ===== Product Attribute Endpoints =====

        // Get product's attribute form schema (for dynamic form rendering)
        group.MapGet("/{productId:guid}/attributes/form-schema", async (
            Guid productId,
            [FromQuery] Guid? variantId,
            IMessageBus bus) =>
        {
            var query = new GetProductAttributeFormSchemaQuery(productId, variantId);
            var result = await bus.InvokeAsync<Result<ProductAttributeFormSchemaDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsRead)
        .WithName("GetProductAttributeFormSchema")
        .WithSummary("Get attribute form schema for a product")
        .WithDescription("Returns all applicable attributes for the product (based on category) with current values. Used for dynamic form generation.")
        .Produces<ProductAttributeFormSchemaDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get product's attribute values
        group.MapGet("/{productId:guid}/attributes", async (
            Guid productId,
            [FromQuery] Guid? variantId,
            IMessageBus bus) =>
        {
            var query = new GetProductAttributeAssignmentsQuery(productId, variantId);
            var result = await bus.InvokeAsync<Result<IReadOnlyCollection<ProductAttributeAssignmentDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsRead)
        .WithName("GetProductAttributeAssignments")
        .WithSummary("Get a product's attribute values")
        .WithDescription("Returns all attribute values assigned to the product or a specific variant.")
        .Produces<IReadOnlyCollection<ProductAttributeAssignmentDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Set a single attribute value
        group.MapPut("/{productId:guid}/attributes/{attributeId:guid}", async (
            Guid productId,
            Guid attributeId,
            SetProductAttributeValueEndpointRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new SetProductAttributeValueCommand(
                productId,
                attributeId,
                request.VariantId,
                request.Value)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductAttributeAssignmentDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("SetProductAttributeValue")
        .WithSummary("Set a product's attribute value")
        .WithDescription("Sets the value for a single attribute on the product or a specific variant.")
        .Produces<ProductAttributeAssignmentDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Bulk update multiple attribute values
        group.MapPut("/{productId:guid}/attributes", async (
            Guid productId,
            BulkUpdateProductAttributesEndpointRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var values = request.Values?.Select(v => new AttributeValueItem(v.AttributeId, v.Value)).ToList()
                         ?? new List<AttributeValueItem>();

            var command = new BulkUpdateProductAttributesCommand(
                productId,
                request.VariantId,
                values)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<IReadOnlyCollection<ProductAttributeAssignmentDto>>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsUpdate)
        .WithName("BulkUpdateProductAttributes")
        .WithSummary("Bulk update product attribute values")
        .WithDescription("Updates multiple attribute values for the product or a specific variant in a single request.")
        .Produces<IReadOnlyCollection<ProductAttributeAssignmentDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}

// ===== Request Records for Product Attribute Endpoints =====

/// <summary>
/// Request to set a single attribute value for a product.
/// </summary>
public sealed record SetProductAttributeValueEndpointRequest(
    Guid? VariantId,
    object? Value);

/// <summary>
/// Request to bulk update multiple attribute values for a product.
/// </summary>
public sealed record BulkUpdateProductAttributesEndpointRequest(
    Guid? VariantId,
    List<AttributeValueEndpointItem>? Values);

/// <summary>
/// Individual attribute value item for bulk update.
/// </summary>
public sealed record AttributeValueEndpointItem(
    Guid AttributeId,
    object? Value);
