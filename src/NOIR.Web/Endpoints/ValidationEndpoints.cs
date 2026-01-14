using NOIR.Application.Common.Validation;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Validation metadata API endpoints.
/// Used by frontend codegen to generate Zod schemas from FluentValidation rules.
/// </summary>
public static class ValidationEndpoints
{
    public static void MapValidationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/validation")
            .WithTags("Validation")
            .AllowAnonymous(); // Allow anonymous for build-time codegen

        // Get all validation metadata
        group.MapGet("/metadata", (IValidationMetadataService service) =>
        {
            var metadata = service.GetAllValidatorMetadata();
            return Results.Ok(metadata);
        })
        .WithName("GetAllValidationMetadata")
        .WithSummary("Get validation metadata for all commands")
        .WithDescription("""
            Returns validation rules extracted from FluentValidation validators.
            Used by frontend build process to generate Zod schemas for real-time validation.
            """)
        .Produces<IReadOnlyList<ValidatorMetadata>>(StatusCodes.Status200OK);

        // Get validation metadata for a specific command
        group.MapGet("/metadata/{commandName}", (
            string commandName,
            IValidationMetadataService service) =>
        {
            var metadata = service.GetValidatorMetadata(commandName);
            return metadata is not null
                ? Results.Ok(metadata)
                : Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Validator Not Found",
                    Detail = $"No validator found for command '{commandName}'"
                });
        })
        .WithName("GetValidationMetadataByCommand")
        .WithSummary("Get validation metadata for a specific command")
        .Produces<ValidatorMetadata>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get validation metadata filtered by feature/area
        group.MapGet("/metadata/filter", (
            string? feature,
            IValidationMetadataService service) =>
        {
            var metadata = string.IsNullOrEmpty(feature)
                ? service.GetAllValidatorMetadata()
                : service.GetValidatorMetadata(name =>
                    name.Contains(feature, StringComparison.OrdinalIgnoreCase));

            return Results.Ok(metadata);
        })
        .WithName("GetValidationMetadataFiltered")
        .WithSummary("Get validation metadata filtered by feature name")
        .WithDescription("Filter validation metadata by feature name (e.g., 'Tenant', 'Auth', 'Role')")
        .Produces<IReadOnlyList<ValidatorMetadata>>(StatusCodes.Status200OK);
    }
}
