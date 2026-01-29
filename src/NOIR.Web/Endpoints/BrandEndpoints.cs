using NOIR.Application.Features.Brands.Commands.CreateBrand;
using NOIR.Application.Features.Brands.Commands.DeleteBrand;
using NOIR.Application.Features.Brands.Commands.UpdateBrand;
using NOIR.Application.Features.Brands.DTOs;
using NOIR.Application.Features.Brands.Queries.GetBrandById;
using NOIR.Application.Features.Brands.Queries.GetBrands;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Brand API endpoints.
/// Provides CRUD operations for product brands.
/// </summary>
public static class BrandEndpoints
{
    public static void MapBrandEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/brands")
            .WithTags("Brands")
            .RequireAuthorization();

        // Get all brands (paged)
        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] bool? isFeatured,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            IMessageBus bus = null!) =>
        {
            var query = new GetBrandsQuery(search, isActive, isFeatured, pageNumber, pageSize);
            var result = await bus.InvokeAsync<Result<PagedResult<BrandListDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BrandsRead)
        .WithName("GetBrands")
        .WithSummary("Get list of brands")
        .WithDescription("Returns paged list of brands with optional filtering.")
        .Produces<PagedResult<BrandListDto>>(StatusCodes.Status200OK);

        // Get brand by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetBrandByIdQuery(id);
            var result = await bus.InvokeAsync<Result<BrandDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BrandsRead)
        .WithName("GetBrandById")
        .WithSummary("Get brand by ID")
        .WithDescription("Returns brand details by ID.")
        .Produces<BrandDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Create brand
        group.MapPost("/", async (
            CreateBrandRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateBrandCommand(
                request.Name,
                request.Slug,
                request.LogoUrl,
                request.BannerUrl,
                request.Description,
                request.Website,
                request.MetaTitle,
                request.MetaDescription,
                request.IsFeatured)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<BrandDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BrandsCreate)
        .WithName("CreateBrand")
        .WithSummary("Create a new brand")
        .WithDescription("Creates a new product brand.")
        .Produces<BrandDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update brand
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateBrandRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateBrandCommand(
                id,
                request.Name,
                request.Slug,
                request.Description,
                request.Website,
                request.LogoUrl,
                request.BannerUrl,
                request.MetaTitle,
                request.MetaDescription,
                request.IsActive,
                request.IsFeatured,
                request.SortOrder)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<BrandDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BrandsUpdate)
        .WithName("UpdateBrand")
        .WithSummary("Update an existing brand")
        .WithDescription("Updates brand details.")
        .Produces<BrandDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Delete brand (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteBrandCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BrandsDelete)
        .WithName("DeleteBrand")
        .WithSummary("Soft-delete a brand")
        .WithDescription("Soft-deletes a brand. Will fail if it has associated products.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
    }
}
