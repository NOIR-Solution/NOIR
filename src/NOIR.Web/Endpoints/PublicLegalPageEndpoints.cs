using NOIR.Application.Features.LegalPages.Queries.GetPublicLegalPage;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Public legal page API endpoints.
/// Unauthenticated endpoints for serving legal page content to website visitors.
/// </summary>
public static class PublicLegalPageEndpoints
{
    public static void MapPublicLegalPageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/public/legal")
            .WithTags("Public Legal Pages")
            .AllowAnonymous();

        // Get legal page by slug (public, resolves tenant override → platform default)
        group.MapGet("/{slug}", async (string slug, IMessageBus bus) =>
        {
            var query = new GetPublicLegalPageQuery(slug);
            var result = await bus.InvokeAsync<Result<PublicLegalPageDto>>(query);
            return result.ToHttpResult();
        })
        .WithName("GetPublicLegalPage")
        .WithSummary("Get public legal page by slug")
        .WithDescription("Returns the legal page content for the given slug. Resolves tenant override if available, otherwise returns platform default.")
        .Produces<PublicLegalPageDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
