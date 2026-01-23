namespace NOIR.Application.Features.Blog.Queries.GetTagById;

/// <summary>
/// Query to get a single blog tag by ID.
/// Used as a before-state resolver for auditing.
/// </summary>
public sealed record GetTagByIdQuery(Guid Id);
