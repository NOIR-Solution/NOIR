using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace NOIR.Web.OpenApi;

/// <summary>
/// OpenAPI operation transformer that automatically adds standard HTTP error responses
/// based on endpoint metadata (authorization requirements, rate limiting, HTTP method).
/// Eliminates the need to manually add .Produces&lt;ProblemDetails&gt;(401/403/422/429)
/// to every endpoint definition.
/// </summary>
public sealed class EndpointMetadataTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var httpMethod = context.Description.HttpMethod?.ToUpperInvariant();

        var requiresAuth = metadata.OfType<AuthorizeAttribute>().Any()
                           && !metadata.OfType<AllowAnonymousAttribute>().Any();
        var isRateLimited = metadata.OfType<EnableRateLimitingAttribute>().Any();
        var isMutation = httpMethod is "POST" or "PUT" or "PATCH";

        if (requiresAuth)
        {
            operation.Responses?.TryAdd("401", new OpenApiResponse
            {
                Description = "Unauthorized — JWT token or API key is missing, expired, or revoked."
            });
            operation.Responses?.TryAdd("403", new OpenApiResponse
            {
                Description = "Forbidden — you do not have the required permission."
            });
        }

        if (isMutation)
        {
            operation.Responses?.TryAdd("422", new OpenApiResponse
            {
                Description = "Unprocessable Entity — FluentValidation failed. See ProblemDetails for field-level errors."
            });
        }

        if (isRateLimited)
        {
            operation.Responses?.TryAdd("429", new OpenApiResponse
            {
                Description = "Too Many Requests — rate limit exceeded. Retry after the value indicated by the Retry-After header."
            });
        }

        return Task.CompletedTask;
    }
}
