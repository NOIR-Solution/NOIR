using NOIR.Application.Features.ProductAttributes.Commands.AddProductAttributeValue;
using NOIR.Application.Features.ProductAttributes.Commands.CreateProductAttribute;
using NOIR.Application.Features.ProductAttributes.Commands.DeleteProductAttribute;
using NOIR.Application.Features.ProductAttributes.Commands.RemoveProductAttributeValue;
using NOIR.Application.Features.ProductAttributes.Commands.UpdateProductAttribute;
using NOIR.Application.Features.ProductAttributes.Commands.UpdateProductAttributeValue;
using NOIR.Application.Features.ProductAttributes.DTOs;
using NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributeById;
using NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributes;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Product Attribute API endpoints.
/// Provides CRUD operations for product attributes and their values.
/// </summary>
public static class ProductAttributeEndpoints
{
    public static void MapProductAttributeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/product-attributes")
            .WithTags("Product Attributes")
            .RequireFeature(ModuleNames.Ecommerce.Attributes)
            .RequireAuthorization();

        // Get all attributes (paged)
        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] bool? isFilterable,
            [FromQuery] bool? isVariantAttribute,
            [FromQuery] string? type,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            IMessageBus bus = null!) =>
        {
            var query = new GetProductAttributesQuery(search, isActive, isFilterable, isVariantAttribute, type, pageNumber, pageSize);
            var result = await bus.InvokeAsync<Result<PagedResult<ProductAttributeListDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AttributesRead)
        .WithName("GetProductAttributes")
        .WithSummary("Get list of product attributes")
        .WithDescription("Returns paged list of product attributes with optional filtering.")
        .Produces<PagedResult<ProductAttributeListDto>>(StatusCodes.Status200OK);

        // Get attribute by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetProductAttributeByIdQuery(id);
            var result = await bus.InvokeAsync<Result<ProductAttributeDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AttributesRead)
        .WithName("GetProductAttributeById")
        .WithSummary("Get product attribute by ID")
        .WithDescription("Returns product attribute details including values by ID.")
        .Produces<ProductAttributeDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Create attribute
        group.MapPost("/", async (
            CreateProductAttributeRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateProductAttributeCommand(
                request.Code,
                request.Name,
                request.Type,
                request.IsFilterable,
                request.IsSearchable,
                request.IsRequired,
                request.IsVariantAttribute,
                request.ShowInProductCard,
                request.ShowInSpecifications,
                request.IsGlobal,
                request.Unit,
                request.ValidationRegex,
                request.MinValue,
                request.MaxValue,
                request.MaxLength,
                request.DefaultValue,
                request.Placeholder,
                request.HelpText)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductAttributeDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AttributesCreate)
        .WithName("CreateProductAttribute")
        .WithSummary("Create a new product attribute")
        .WithDescription("Creates a new product attribute.")
        .Produces<ProductAttributeDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update attribute
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateProductAttributeRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateProductAttributeCommand(
                id,
                request.Code,
                request.Name,
                request.IsFilterable,
                request.IsSearchable,
                request.IsRequired,
                request.IsVariantAttribute,
                request.ShowInProductCard,
                request.ShowInSpecifications,
                request.IsGlobal,
                request.Unit,
                request.ValidationRegex,
                request.MinValue,
                request.MaxValue,
                request.MaxLength,
                request.DefaultValue,
                request.Placeholder,
                request.HelpText,
                request.SortOrder,
                request.IsActive)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductAttributeDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AttributesUpdate)
        .WithName("UpdateProductAttribute")
        .WithSummary("Update an existing product attribute")
        .WithDescription("Updates product attribute details.")
        .Produces<ProductAttributeDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Delete attribute (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteProductAttributeCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AttributesDelete)
        .WithName("DeleteProductAttribute")
        .WithSummary("Soft-delete a product attribute")
        .WithDescription("Soft-deletes a product attribute.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ===== Attribute Values =====

        // Add value to attribute
        group.MapPost("/{id:guid}/values", async (
            Guid id,
            AddProductAttributeValueRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AddProductAttributeValueCommand(
                id,
                request.Value,
                request.DisplayValue,
                request.ColorCode,
                request.SwatchUrl,
                request.IconUrl,
                request.SortOrder)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductAttributeValueDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AttributesUpdate)
        .WithName("AddProductAttributeValue")
        .WithSummary("Add a value to a product attribute")
        .WithDescription("Adds a new value to a Select/MultiSelect attribute.")
        .Produces<ProductAttributeValueDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update attribute value
        group.MapPut("/{id:guid}/values/{valueId:guid}", async (
            Guid id,
            Guid valueId,
            UpdateProductAttributeValueRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateProductAttributeValueCommand(
                id,
                valueId,
                request.Value,
                request.DisplayValue,
                request.ColorCode,
                request.SwatchUrl,
                request.IconUrl,
                request.SortOrder,
                request.IsActive)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProductAttributeValueDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AttributesUpdate)
        .WithName("UpdateProductAttributeValue")
        .WithSummary("Update an attribute value")
        .WithDescription("Updates an existing attribute value.")
        .Produces<ProductAttributeValueDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Remove attribute value
        group.MapDelete("/{id:guid}/values/{valueId:guid}", async (
            Guid id,
            Guid valueId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RemoveProductAttributeValueCommand(id, valueId) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AttributesUpdate)
        .WithName("RemoveProductAttributeValue")
        .WithSummary("Remove an attribute value")
        .WithDescription("Removes a value from a Select/MultiSelect attribute.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}

/// <summary>
/// Request to create a product attribute.
/// </summary>
public sealed record CreateProductAttributeRequest(
    string Code,
    string Name,
    string Type,
    bool IsFilterable = false,
    bool IsSearchable = false,
    bool IsRequired = false,
    bool IsVariantAttribute = false,
    bool ShowInProductCard = false,
    bool ShowInSpecifications = true,
    bool IsGlobal = false,
    string? Unit = null,
    string? ValidationRegex = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    int? MaxLength = null,
    string? DefaultValue = null,
    string? Placeholder = null,
    string? HelpText = null);

/// <summary>
/// Request to update a product attribute.
/// </summary>
public sealed record UpdateProductAttributeRequest(
    string Code,
    string Name,
    bool IsFilterable,
    bool IsSearchable,
    bool IsRequired,
    bool IsVariantAttribute,
    bool ShowInProductCard,
    bool ShowInSpecifications,
    bool IsGlobal,
    string? Unit,
    string? ValidationRegex,
    decimal? MinValue,
    decimal? MaxValue,
    int? MaxLength,
    string? DefaultValue,
    string? Placeholder,
    string? HelpText,
    int SortOrder,
    bool IsActive);

/// <summary>
/// Request to add a value to a product attribute.
/// </summary>
public sealed record AddProductAttributeValueRequest(
    string Value,
    string DisplayValue,
    string? ColorCode = null,
    string? SwatchUrl = null,
    string? IconUrl = null,
    int SortOrder = 0);

/// <summary>
/// Request to update a product attribute value.
/// </summary>
public sealed record UpdateProductAttributeValueRequest(
    string Value,
    string DisplayValue,
    string? ColorCode,
    string? SwatchUrl,
    string? IconUrl,
    int SortOrder,
    bool IsActive);
