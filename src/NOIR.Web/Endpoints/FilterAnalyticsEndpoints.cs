using NOIR.Application.Features.FilterAnalytics.Commands.CreateFilterEvent;
using NOIR.Application.Features.FilterAnalytics.DTOs;
using NOIR.Application.Features.FilterAnalytics.Queries.GetPopularFilters;
using NOIR.Domain.Enums;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Filter Analytics API endpoints.
/// Provides endpoints for tracking and analyzing filter usage.
/// </summary>
public static class FilterAnalyticsEndpoints
{
    public static void MapFilterAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics/filter-events")
            .WithTags("Filter Analytics");

        // Track a filter event (public - allows anonymous tracking)
        group.MapPost("/", async (
            CreateFilterEventCommand command,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<FilterAnalyticsEventDto>>(command);
            return result.ToHttpResult();
        })
        .AllowAnonymous() // Allow tracking for guest users
        .WithName("TrackFilterEvent")
        .WithSummary("Track a filter analytics event")
        .WithDescription("Records a filter usage event for analytics purposes. Can be called by both authenticated and anonymous users.")
        .Produces<FilterAnalyticsEventDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // Get popular filters (admin only)
        group.MapGet("/popular", async (
            [FromQuery] DateTimeOffset? fromDate,
            [FromQuery] DateTimeOffset? toDate,
            [FromQuery] string? categorySlug,
            [FromQuery] int? top,
            IMessageBus bus) =>
        {
            var query = new GetPopularFiltersQuery(
                FromDate: fromDate,
                ToDate: toDate,
                CategorySlug: categorySlug,
                Top: top ?? 20);
            var result = await bus.InvokeAsync<Result<PopularFiltersResult>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ProductsRead)
        .WithName("GetPopularFilters")
        .WithSummary("Get popular filters")
        .WithDescription("Returns the most frequently used filters within a date range. Requires product read permission.")
        .Produces<PopularFiltersResult>(StatusCodes.Status200OK);
    }
}
