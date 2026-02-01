using NOIR.Application.Features.LegalPages.Commands.RevertLegalPageToDefault;
using NOIR.Application.Features.LegalPages.Commands.UpdateLegalPage;
using NOIR.Application.Features.LegalPages.Queries.GetLegalPage;
using NOIR.Application.Features.LegalPages.Queries.GetLegalPages;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Legal page management API endpoints.
/// Admin endpoints for viewing and editing legal pages (Terms, Privacy, etc.).
/// </summary>
public static class LegalPageEndpoints
{
    public static void MapLegalPageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/legal-pages")
            .WithTags("Legal Pages")
            .RequireAuthorization();

        // Get all legal pages
        group.MapGet("/", async (IMessageBus bus) =>
        {
            var query = new GetLegalPagesQuery();
            var result = await bus.InvokeAsync<Result<List<LegalPageListDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.LegalPagesRead)
        .WithName("GetLegalPages")
        .WithSummary("Get list of legal pages")
        .WithDescription("Returns all legal pages with inheritance status.")
        .Produces<List<LegalPageListDto>>(StatusCodes.Status200OK);

        // Get single legal page by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetLegalPageQuery(id);
            var result = await bus.InvokeAsync<Result<LegalPageDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.LegalPagesRead)
        .WithName("GetLegalPage")
        .WithSummary("Get legal page by ID")
        .WithDescription("Returns full legal page details including HTML content.")
        .Produces<LegalPageDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Update legal page
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateLegalPageRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateLegalPageCommand(
                id,
                request.Title,
                request.HtmlContent,
                request.MetaTitle,
                request.MetaDescription,
                request.CanonicalUrl,
                request.AllowIndexing)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<LegalPageDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.LegalPagesUpdate)
        .WithName("UpdateLegalPage")
        .WithSummary("Update legal page content")
        .WithDescription("Updates the title, HTML content, and meta description. Implements copy-on-write for tenant overrides.")
        .Produces<LegalPageDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Revert to platform default
        group.MapPost("/{id:guid}/revert", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RevertLegalPageToDefaultCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<LegalPageDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.LegalPagesUpdate)
        .WithName("RevertLegalPageToDefault")
        .WithSummary("Revert to platform default page")
        .WithDescription("Deletes the tenant's customized version and reverts to the platform default. Only available for tenant users.")
        .Produces<LegalPageDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
